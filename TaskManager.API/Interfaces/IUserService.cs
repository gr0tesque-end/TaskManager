using CommonTypes;
using TaskManager.API.Services;

namespace TaskManager.API;

public interface IUserService
{
    Task<User?> GetByUsername(string username);
    Task<User> CreateUser(UserRegistrationModel model);
    Task<bool> VerifyPassword(User user, string password);
    Task AddRefreshToken(User user, RefreshToken refreshToken);
    Task RevokeRefreshToken(string token, string ipAddress);
    Task<User> GetByRefreshToken(string refreshToken);
}