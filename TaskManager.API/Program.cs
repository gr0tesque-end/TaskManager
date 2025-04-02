using System.Text;
using CommonTypes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using TaskManager.API;
using TaskManager.API.Interfaces;
using TaskManager.API.Repositories;
using TaskManager.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ILocalStorageService, ServerStorageService>();
builder.Services.AddSession();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TeamOwner", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c =>
                c.Type == "TeamOwner" && c.Value == "true")));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Minimal API endpoints
app.MapGet("/tasks", async (ITaskRepository repo) =>
    await repo.GetAllAsync()).RequireAuthorization();

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

app.Run("https://localhost:5000");