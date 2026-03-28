namespace WordGuessingGame.Core.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ProfilePictureUrl { get; set; }
    public Language Language { get; set; } = Language.Dutch;
    public string BannerColor { get; set; } = "#5b21b6";
    public string? ActiveTag { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserTag> Tags { get; set; } = new List<UserTag>();
}
