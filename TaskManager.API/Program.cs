using CommonTypes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskManager.API;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7191")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Minimal API endpoints
app.MapGet("/tasks", async (ITaskRepository repo) =>
    await repo.GetAllAsync());

app.MapGet("/tasks/{id}", async (int id, ITaskRepository repo) =>
    await repo.GetByIdAsync(id) is UTask task ? Results.Ok(task) : Results.NotFound());

app.MapPost("/tasks", async (UTask task, ITaskRepository repo) =>
{
    var createdTask = await repo.CreateAsync(task);
    return Results.Created($"/tasks/{createdTask.Id}", createdTask);
});

app.MapPut("/tasks/{id}", async (int id, UTask inputTask, ITaskRepository repo) =>
{
    if (id != inputTask.Id) return Results.BadRequest("ID mismatch");

    return await repo.UpdateAsync(inputTask)
        ? Results.NoContent()
        : Results.NotFound();
});

app.MapDelete("/tasks/{id}", async (int id, ITaskRepository repo) =>
{
    return await repo.DeleteAsync(id)
        ? Results.NoContent()
        : Results.NotFound();
});

app.UseCors();

app.Run("https://localhost:5000");