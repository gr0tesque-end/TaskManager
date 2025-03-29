using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using TaskManager.Client;
using TaskManager.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<TaskService>();
builder.Services.AddMudServices();
builder.Services.AddHttpClient("TaskAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:5000"); // Your API port
});
await builder.Build().RunAsync();
