using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using TaskManager.API.Controllers;
using TaskManager.API.Interfaces;

namespace TaskManager.API.Services;
public class AuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;
    public AuthService(HttpClient http, ILocalStorageService storage)
    {
        _http = http;
        _storage = storage;
    }

    public async Task<bool> LoginAsync(LoginModel model)
    {
        var response = await _http.PostAsJsonAsync("auth/login", model);
        if (!response.IsSuccessStatusCode) return false;

        var result = await response.Content.ReadFromJsonAsync<AuthResult>();
        await _storage.SetItemAsync("authToken", result!.Token);
        return true;
    }

    public async Task<AuthenticationState> GetAuthState()
    {
        var token = await _storage.GetItemAsync<string>("authToken");
        var identity = string.IsNullOrEmpty(token)
            ? new ClaimsIdentity()
            : new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
        return token.Claims;
    }
}

public record AuthResult(string Token);