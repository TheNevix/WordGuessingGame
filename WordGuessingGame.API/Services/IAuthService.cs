using WordGuessingGame.API.Models.DTOs;

namespace WordGuessingGame.API.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
}
