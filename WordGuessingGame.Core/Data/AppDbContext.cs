using Microsoft.EntityFrameworkCore;
using WordGuessingGame.Core.Models;

namespace WordGuessingGame.Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users { get; set; }
    public DbSet<GameHistory> GameHistories { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<UserTag> UserTags { get; set; }
    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<UserChallenge> UserChallenges { get; set; }

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

        modelBuilder.Entity<UserTag>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).HasMaxLength(50).IsRequired();
            entity.HasOne(t => t.User)
                .WithMany(u => u.Tags)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Challenge>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Key).HasMaxLength(100).IsRequired();
            entity.Property(c => c.RewardValue).HasMaxLength(100).IsRequired();
            entity.HasData(
                new Challenge { Id = 1, Key = "win_5_games",    Type = ChallengeType.WinCount,  TargetValue = 5, RewardType = RewardType.Tag,         RewardValue = "Veteran" },
                new Challenge { Id = 2, Key = "win_streak_5",   Type = ChallengeType.WinStreak, TargetValue = 5, RewardType = RewardType.BannerColor,  RewardValue = "#d97706" }
            );
        });

        modelBuilder.Entity<UserChallenge>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(u => u.Challenge)
                .WithMany(c => c.UserChallenges)
                .HasForeignKey(u => u.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(u => new { u.UserId, u.ChallengeId }).IsUnique();
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
