namespace WordGuessingGame.API.Models.DTOs;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string Language { get; set; } = "en";
    public string BannerColor { get; set; } = "#5b21b6";
    public string? ActiveTag { get; set; }
    public List<string> Tags { get; set; } = new();
}
