namespace CommonTypes;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
    public List<RefreshToken> RefreshTokens { get; set; } = new();
    public List<UTask> Tasks { get; set; } = new();

    public bool HasValidRefreshToken(string refreshToken)
    {
        return RefreshTokens.Any(rt => rt.Token == refreshToken && rt.IsActive);
    }
}