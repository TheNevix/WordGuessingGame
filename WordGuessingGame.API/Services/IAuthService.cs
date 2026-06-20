using WordGuessingGame.API.Models.DTOs;

namespace WordGuessingGame.API.Services;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request);
    Task<string?> RegisterAsync(RegisterRequest request);   // returns error message or null on success
    Task<AuthResponse?> RefreshAsync(string refreshToken);
    Task<bool> RevokeAsync(string refreshToken);
    Task<bool> VerifyEmailAsync(string token);
    Task<bool> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}

public record LoginResult(AuthResponse? Auth, string? ErrorCode = null)
{
    public static LoginResult Ok(AuthResponse auth) => new(auth);
    public static LoginResult Invalid()     => new(null, "invalid_credentials");
    public static LoginResult NotVerified() => new(null, "email_not_verified");
}
