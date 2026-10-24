namespace Practice6.Models;

public sealed class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public List<Ad> Ads { get; set; } = [];
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
