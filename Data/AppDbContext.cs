using Microsoft.EntityFrameworkCore;
using Practice6.Models;

namespace Practice6.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Ad> Ads => Set<Ad>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.Property(f => f.OriginalName).HasMaxLength(255);
            entity.Property(f => f.StoredRelativePath).HasMaxLength(500);
            entity.Property(f => f.ContentType).HasMaxLength(100);
            entity.HasOne(f => f.Ad).WithMany().HasForeignKey(f => f.AdId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>().WithMany().HasForeignKey(f => f.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Email).HasMaxLength(254);
            entity.Property(user => user.Role).HasMaxLength(20);
        });
        modelBuilder.Entity<Ad>(entity =>
        {
            entity.Property(ad => ad.Title).HasMaxLength(150);
            entity.Property(ad => ad.Text).HasMaxLength(5000);
        });
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(token => token.TokenHash).HasMaxLength(64);
            entity.HasIndex(token => token.TokenHash).IsUnique();
        });
    }
}
