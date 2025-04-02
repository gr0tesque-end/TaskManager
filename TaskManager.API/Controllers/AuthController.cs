using CommonTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaskManager.API.Services;

namespace TaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IUserService _userService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthController(
        IConfiguration config,
        IUserService userService,
        IHttpContextAccessor httpContextAccessor)
    {
        _config = config;
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationModel model)
    {
        try
        {
            var user = await _userService.CreateUser(model);
            return Ok(new { Message = "Registration successful" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _userService.GetByUsername(model.Username);
        if (user == null || !await _userService.VerifyPassword(user, model.Password))
            return Unauthorized(new { Message = "Invalid username or password" });

        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        await _userService.AddRefreshToken(user, refreshToken);

        SetRefreshTokenCookie(refreshToken.Token);

        return Ok(new TokenResult
        (
            token,
            refreshToken.Token,
            refreshToken.Expires
        ));
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { Message = "Refresh token is required" });

        var user = await _userService.GetByRefreshToken(refreshToken);
        if (user == null)
            return Unauthorized(new { Message = "Invalid refresh token" });

        var existingRefreshToken = user.RefreshTokens.Single(t => t.Token == refreshToken);
        if (!existingRefreshToken.IsActive)
            return Unauthorized(new { Message = "Token expired or revoked" });

        // Replace old refresh token
        var newRefreshToken = GenerateRefreshToken();
        await _userService.RevokeRefreshToken(refreshToken, GetIpAddress());
        await _userService.AddRefreshToken(user, newRefreshToken);

        SetRefreshTokenCookie(newRefreshToken.Token);

        var token = GenerateJwtToken(user);
        return Ok(new TokenResult
        (
            token,
            newRefreshToken.Token,
            newRefreshToken.Expires
        ));
    }

    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest model)
    {
        var token = model.Token ?? Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(token))
            return BadRequest(new { Message = "Token is required" });

        await _userService.RevokeRefreshToken(token, GetIpAddress());
        return Ok(new { Message = "Token revoked" });
    }

    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(_config["Jwt:ExpireMinutes"])),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken()
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            CreatedByIp = GetIpAddress()
        };
    }

    private void SetRefreshTokenCookie(string token)
    {
        if (Response == null)
        {
            throw new InvalidOperationException("Response is not available");
        }
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(7),
            SameSite = SameSiteMode.None,
            Secure = true
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }

    private string GetIpAddress()
    {
        return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

public record TokenResult(string Token, string RefreshToken, DateTime Expires);
 
public record LoginModel(string Username, string Password);
public record RevokeTokenRequest(string? Token);