using Microsoft.EntityFrameworkCore;
using WordGuessingGame.Core.Models;

namespace WordGuessingGame.Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users { get; set; }
    public DbSet<GameHistory> GameHistories { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("wgg");

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Username)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Token).HasMaxLength(256).IsRequired();
            entity.HasOne(r => r.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GameHistory>(entity =>
        {
            entity.HasKey(g => g.Id);

            entity.Property(g => g.Word).HasMaxLength(100).IsRequired();
            entity.Property(g => g.WinnerUsername).HasMaxLength(50).IsRequired();
            entity.Property(g => g.OpponentUsername).HasMaxLength(50).IsRequired();

            entity.HasOne(g => g.Winner)
                .WithMany()
                .HasForeignKey(g => g.WinnerUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(g => g.Opponent)
                .WithMany()
                .HasForeignKey(g => g.OpponentUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
