using System.Security.Cryptography;
using System.Text;
using CommonTypes;
using Microsoft.EntityFrameworkCore;

namespace TaskManager.API.Services;

public class UserService : IUserService
{
    private readonly TaskDbContext _context;

    public UserService(TaskDbContext context) => _context = context;

    public async Task<User?> GetByUsername(string username)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User> CreateUser(UserRegistrationModel model)
    {
        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            throw new Exception("Username already exists");

        // Create password hash
        using var hmac = new HMACSHA512();
        var user = new User
        {
            Username = model.Username,
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(model.Password)),
            PasswordSalt = hmac.Key,
            RefreshTokens = new List<RefreshToken>()
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> VerifyPassword(User user, string password)
    {
        using var hmac = new HMACSHA512(user.PasswordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return computedHash.SequenceEqual(user.PasswordHash);
    }

    public async Task AddRefreshToken(User user, RefreshToken refreshToken)
    {
        user.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeRefreshToken(string token, string ipAddress)
    {
        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

        if (user == null) return;

        var refreshToken = user.RefreshTokens.First(t => t.Token == token);
        if (!refreshToken.IsActive) return;

        refreshToken.Revoked = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        await _context.SaveChangesAsync();
    }

    public async Task<User> GetByRefreshToken(string refreshToken)
    {
        return await _context.Users
            .SingleAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken));
    }
}

public record UserRegistrationModel(string Username, string Password);