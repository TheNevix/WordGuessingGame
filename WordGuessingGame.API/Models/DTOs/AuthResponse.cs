namespace WordGuessingGame.API.Models.DTOs;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string? ProfilePictureUrl { get; set; }
}
