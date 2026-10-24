using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Practice6.Contracts;
using Practice6.Data;
using Practice6.Models;
using Practice6.Options;
using Microsoft.Extensions.Options;

namespace Practice6.Services;

public sealed class AuthService(AppDbContext db, IOptions<JwtOptions> settings, ILogger<AuthService> logger)
{
    public async Task<TokenDto?> RegisterAsync(RegisterDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Email == email))
            return null;

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var tokens = CreateTokens(user);
        await db.SaveChangesAsync();
        return tokens;
    }

    public async Task<TokenDto?> LoginAsync(LoginDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(user => user.Email == email);
        var valid = user is not null && BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        logger.LogInformation("Вход {Email}: {Result}", email, valid ? "успех" : "отказ");
        if (!valid)
            return null;

        var tokens = CreateTokens(user!);
        await db.SaveChangesAsync();
        return tokens;
    }

    public async Task<TokenDto?> RefreshAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var token = await db.RefreshTokens.Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == hash);
        if (token is null)
            return null;

        var now = DateTime.UtcNow;
        if (token.RevokedAt is not null)
        {
            var activeTokens = await db.RefreshTokens
                .Where(item => item.UserId == token.UserId && item.RevokedAt == null).ToListAsync();
            foreach (var activeToken in activeTokens)
                activeToken.RevokedAt = now;

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return null;
        }
        if (token.ExpiresAt <= now)
            return null;

        token.RevokedAt = now;
        var tokens = CreateTokens(token.User);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return tokens;
    }

    private TokenDto CreateTokens(User user)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(settings.Value.AccessMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        if (user.Role == "Moderator")
            claims.Add(new Claim("permission", "ads.moderate"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Value.Secret));
        var jwt = new JwtSecurityToken(
            issuer: settings.Value.Issuer,
            audience: settings.Value.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refresh),
            CreatedAt = now,
            ExpiresAt = now.AddDays(7)
        });
        return new TokenDto(new JwtSecurityTokenHandler().WriteToken(jwt), refresh, expiresAt);
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
