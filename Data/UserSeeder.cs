using Microsoft.EntityFrameworkCore;
using Practice6.Models;

namespace Practice6.Data;

public static class UserSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration configuration)
    {
        foreach (var role in new[] { "Moderator", "Admin" })
        {
            var email = configuration[$"Seed:{role}:Email"]?.Trim().ToLowerInvariant();
            var password = configuration[$"Seed:{role}:Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                continue;
            if (await db.Users.AnyAsync(user => user.Email == email))
                continue;

            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role
            });
        }
        await db.SaveChangesAsync();
    }
}
