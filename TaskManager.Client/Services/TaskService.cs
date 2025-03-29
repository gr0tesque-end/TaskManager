using System.Net.Http;
using System.Net.Http.Json;
using CommonTypes;

namespace TaskManager.Client.Services;

public class TaskService
{
    private readonly HttpClient _http;

    public TaskService(IHttpClientFactory clientFactory)
    {
        _http = clientFactory.CreateClient("TaskAPI");
    }
    public async Task<List<UTask>> GetTasksAsync()
    {
        return await _http.GetFromJsonAsync<List<UTask>>("/tasks") ?? new();
    }

    public async Task<UTask?> GetTaskAsync(int id)
    {
        return await _http.GetFromJsonAsync<UTask>($"/tasks/{id}");
    }

    public async Task CreateTaskAsync(UTask task)
    {
        await _http.PostAsJsonAsync("/tasks", task);
    }

    public async Task UpdateTaskAsync(UTask task)
    {
        await _http.PutAsJsonAsync($"/tasks/{task.Id}", task);
    }

    public async Task DeleteTaskAsync(int id)
    {
        await _http.DeleteAsync($"/tasks/{id}");
    }
}