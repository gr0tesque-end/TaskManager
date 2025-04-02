using CommonTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.API.Services;
using Xunit;

namespace TaskManager.API.Tests.Services;

public class UserServiceTests
{
    private readonly DbContextOptions<TaskDbContext> _options;
    private readonly TaskDbContext _context;
    private readonly UserService _service;

    public UserServiceTests()
    {
        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        _options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        _context = new TaskDbContext(_options);
        _service = new UserService(_context);
    }

    [Fact]
    public async Task CanAddRefreshToken()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_RefreshTokens")
            .Options;

        using (var context = new TaskDbContext(options))
        {
            var user = new User
            {
                Username = "tokenuser",
                PasswordHash = new byte[0],
                PasswordSalt = new byte[0]
            };
            context.Users.Add(user);

            var refreshToken = new RefreshToken
            {
                Token = "test-token",
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = "127.0.0.1",
                User = user
            };
            context.RefreshTokens.Add(refreshToken);

            await context.SaveChangesAsync();
        }

        using (var context = new TaskDbContext(options))
        {
            var token = await context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == "test-token");

            Assert.NotNull(token);
            Assert.Equal("tokenuser", token.User.Username);
        }
    }

    [Fact]
    public async Task CreateUser_ValidUser_CreatesUser()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;

        // Act & Assert
        using (var context = new TaskDbContext(options))
        {
            var user = new User
            {
                Username = "testuser",
                PasswordHash = new byte[0],
                PasswordSalt = new byte[0]
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        using (var context = new TaskDbContext(options))
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == "testuser");
            Assert.NotNull(user);
            Assert.Equal("testuser", user.Username);
        }
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_ThrowsException()
    {
        // Arrange
        var model1 = new UserRegistrationModel("duplicate", "Test@123");
        await _service.CreateUser(model1);

        var model2 = new UserRegistrationModel("duplicate", "Test@123");

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.CreateUser(model2));
    }

    [Fact]
    public async Task VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var model = new UserRegistrationModel("testuser", "correct");
        var user = await _service.CreateUser(model);

        // Act
        var result = await _service.VerifyPassword(user, "correct");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task VerifyPassword_WrongPassword_ReturnsFalse()
    {
        // Arrange
        var model = new UserRegistrationModel("testuser", "correct");
        var user = await _service.CreateUser(model);

        // Act
        var result = await _service.VerifyPassword(user, "wrong");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AddRefreshToken_ValidToken_AddsToken()
    {
        // Arrange
        var model = new UserRegistrationModel("testuser", "password");
        var user = await _service.CreateUser(model);
        var refreshToken = new RefreshToken
        {
            Token = "test-token",
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            CreatedByIp = "127.0.0.1"
        };

        // Act
        await _service.AddRefreshToken(user, refreshToken);
        var dbUser = await _service.GetByUsername("testuser");

        // Assert
        Assert.Single(dbUser!.RefreshTokens);
        Assert.Equal("test-token", dbUser.RefreshTokens[0].Token);
    }

    [Fact]
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}