using System.Net;
using CommonTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using TaskManager.API.Controllers;
using TaskManager.API.Services;

namespace TaskManager.API.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly AuthController _controller;
    private readonly DefaultHttpContext _httpContext;

    public AuthControllerTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(x => x["Jwt:Key"]).Returns("VerySecretKey1234567890VerySecretKey1234567890");
        _mockConfig.Setup(x => x["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfig.Setup(x => x["Jwt:Audience"]).Returns("TestAudience");
        _mockConfig.Setup(x => x["Jwt:ExpireMinutes"]).Returns("30");

        _mockUserService = new Mock<IUserService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        _httpContext = new DefaultHttpContext();
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContext);

        _controller = new AuthController(
            _mockConfig.Object,
            _mockUserService.Object,
            _mockHttpContextAccessor.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            }
        };
    }


    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var model = new LoginModel("testuser", "Test@123");
        var user = new User { Username = "testuser" };

        _mockUserService.Setup(x => x.GetByUsername(model.Username))
            .ReturnsAsync(user);
        _mockUserService.Setup(x => x.VerifyPassword(user, model.Password))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Login(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = (TokenResult)okResult.Value!;
        Assert.NotNull(response.Token);
        Assert.NotNull(response.RefreshToken);
    }

    [Fact]
    public async Task Register_ValidUser_ReturnsOk()
    {
        // Arrange
        var model = new UserRegistrationModel("testuser", "Test@123");

        // Act
        var result = await _controller.Register(model);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Login_ValidCredentials_SetsRefreshTokenCookie()
    {
        // Arrange
        var model = new LoginModel("testuser", "Test@123");
        var user = new User { Username = "testuser" };

        _mockUserService.Setup(x => x.GetByUsername(model.Username))
            .ReturnsAsync(user);
        _mockUserService.Setup(x => x.VerifyPassword(user, model.Password))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Login(model);

        // Assert
        var setCookieHeader = _httpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("refreshToken=", setCookieHeader);
        Assert.Contains("httponly", setCookieHeader);
        Assert.Contains("secure", setCookieHeader);
        Assert.Contains("samesite=none", setCookieHeader);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsBadRequest()
    {
        // Arrange
        var model = new UserRegistrationModel("existinguser", "Test@123");

        _mockUserService.Setup(x => x.CreateUser(model))
            .ThrowsAsync(new Exception("Username already exists"));

        // Act
        var result = await _controller.Register(model);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Username already exists", badRequestResult.Value.ToString());
    }


    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var model = new LoginModel("testuser", "wrongpassword");
        _mockUserService.Setup(x => x.GetByUsername(model.Username))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.Login(model);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var refreshToken = "valid-refresh-token";
        var user = new User
        {
            Username = "testuser",
            RefreshTokens = new List<RefreshToken>
        {
            new RefreshToken
            {
                Token = refreshToken,
                Expires = DateTime.UtcNow.AddDays(1),
                Created = DateTime.UtcNow,
                CreatedByIp = "127.0.0.1"
            }
        }
        };

        _httpContext.Request.Cookies = new MockRequestCookieCollection(new Dictionary<string, string>
        {
            { "refreshToken", refreshToken }
        });

        _mockUserService.Setup(x => x.GetByRefreshToken(refreshToken))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.RefreshToken();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = (TokenResult)okResult.Value!;
        Assert.NotNull(response.Token);
        Assert.NotNull(response.RefreshToken);

        // Verify the new cookie was set
        var setCookieHeader = _httpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("refreshToken=", setCookieHeader);
    }

    [Fact]
    public async Task RevokeToken_ValidToken_ReturnsOk()
    {
        // Arrange
        var request = new RevokeTokenRequest("valid-token");

        // Act
        var result = await _controller.RevokeToken(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}

// Helper class for mocking cookies
public class MockRequestCookieCollection : IRequestCookieCollection
{
    private readonly Dictionary<string, string> _cookies;

    public MockRequestCookieCollection(Dictionary<string, string> cookies)
    {
        _cookies = cookies;
    }

    public string? this[string key] => _cookies.TryGetValue(key, out var value) ? value : null;

    public int Count => _cookies.Count;

    public ICollection<string> Keys => _cookies.Keys;

    public bool ContainsKey(string key) => _cookies.ContainsKey(key);

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();

    public bool TryGetValue(string key, out string? value) => _cookies.TryGetValue(key, out value);

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}