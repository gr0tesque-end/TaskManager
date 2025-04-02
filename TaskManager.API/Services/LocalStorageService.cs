using System.Text;
using Microsoft.AspNetCore.Http;
using TaskManager.API.Interfaces;

namespace TaskManager.API.Services;

public class ServerStorageService : ILocalStorageService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServerStorageService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<T?> GetItemAsync<T>(string key)
    {
        var value = _httpContextAccessor.HttpContext?.Session.GetString(key);
        return Task.FromResult(value == null ? default : System.Text.Json.JsonSerializer.Deserialize<T>(value));
    }

    public Task SetItemAsync<T>(string key, T value)
    {
        var serialized = System.Text.Json.JsonSerializer.Serialize(value);
        _httpContextAccessor.HttpContext?.Session.SetString(key, serialized);
        return Task.CompletedTask;
    }

    public Task RemoveItemAsync(string key)
    {
        _httpContextAccessor.HttpContext?.Session.Remove(key);
        return Task.CompletedTask;
    }
}