using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WordGuessingGame.API.Models.DTOs;
using WordGuessingGame.Core.Models;
using WordGuessingGame.Repository.Interfaces;

namespace WordGuessingGame.API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IEmailService emailService,
        IConfiguration config)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _emailService = emailService;
        _config = config;
    }

    private static string MapLanguage(Core.Models.Language lang) =>
        lang == Core.Models.Language.English ? "en" : "nl";

    private static List<TagDto> MapTags(AppUser user) =>
        user.Tags.Select(t => new TagDto { Name = t.Name, Color = t.Color }).ToList();

    private static string GenerateSecureToken()
    {
        var bytes = new byte[48];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').Replace("=", "");
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return LoginResult.Invalid();

        if (!user.EmailVerified)
        {
            // Existing accounts (created before email verification existed) have no token and
            // never received a mail. Generate one and send it on login so they have a way to
            // verify. Only (re)send when there is no still-valid token, to avoid spamming.
            var hasValidToken = !string.IsNullOrEmpty(user.VerificationToken)
                                && user.VerificationTokenExpiry.HasValue
                                && user.VerificationTokenExpiry.Value > DateTime.UtcNow;

            if (!hasValidToken)
            {
                user.VerificationToken = GenerateSecureToken();
                user.VerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
                await _userRepository.SaveChangesAsync();

                try
                {
                    await _emailService.SendConfirmationEmailAsync(user.Email, user.Username, user.VerificationToken);
                }
                catch
                {
                    // Sending failed (e.g. network error). Clear the token so the next login
                    // attempt retries instead of assuming a mail is sitting in their inbox.
                    user.VerificationToken = null;
                    user.VerificationTokenExpiry = null;
                    await _userRepository.SaveChangesAsync();
                }
            }

            return LoginResult.NotVerified();
        }

        var auth = new AuthResponse
        {
            Token = GenerateAccessToken(user, request.RememberMe),
            Username = user.Username,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Language = MapLanguage(user.Language),
            BannerColor = user.BannerColor,
            ActiveTag = user.ActiveTag,
            Tags = MapTags(user)
        };

        if (request.RememberMe)
            auth.RefreshToken = await CreateRefreshTokenAsync(user.Id);

        return LoginResult.Ok(auth);
    }

    public async Task<string?> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.UsernameExistsAsync(request.Username))
            return "Gebruikersnaam is al in gebruik.";

        if (await _userRepository.EmailExistsAsync(request.Email))
            return "E-mailadres is al in gebruik.";

        var token = GenerateSecureToken();

        var user = new AppUser
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            EmailVerified = false,
            VerificationToken = token,
            VerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            Tags = new List<UserTag> { new UserTag { Name = "OG" } }
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        await _emailService.SendConfirmationEmailAsync(request.Email, request.Username, token);

        return null;
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var user = await _userRepository.GetByVerificationTokenAsync(token);
        if (user is null || user.VerificationTokenExpiry < DateTime.UtcNow)
            return false;

        user.EmailVerified = true;
        user.VerificationToken = null;
        user.VerificationTokenExpiry = null;
        await _userRepository.SaveChangesAsync();

        await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);

        return true;
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null || !user.EmailVerified)
            return true; // Don't reveal whether email exists

        var token = GenerateSecureToken();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _userRepository.SaveChangesAsync();

        await _emailService.SendPasswordResetEmailAsync(user.Email, user.Username, token);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var user = await _userRepository.GetByPasswordResetTokenAsync(token);
        if (user is null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        await _userRepository.SaveChangesAsync();

        return true;
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (token is null || !token.IsActive)
            return null;

        token.RevokedAt = DateTime.UtcNow;
        var newRefreshToken = await CreateRefreshTokenAsync(token.UserId);
        await _refreshTokenRepository.SaveChangesAsync();

        return new AuthResponse
        {
            Token = GenerateAccessToken(token.User, rememberMe: true),
            Username = token.User.Username,
            RefreshToken = newRefreshToken,
            ProfilePictureUrl = token.User.ProfilePictureUrl,
            Language = MapLanguage(token.User.Language),
            BannerColor = token.User.BannerColor,
            Tags = MapTags(token.User)
        };
    }

    public async Task<bool> RevokeAsync(string refreshToken)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (token is null || !token.IsActive)
            return false;

        token.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.SaveChangesAsync();
        return true;
    }

    private string GenerateAccessToken(AppUser user, bool rememberMe)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var expiry = rememberMe
            ? DateTime.UtcNow.AddHours(1)
            : DateTime.UtcNow.AddDays(7);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateRefreshTokenAsync(int userId)
    {
        var tokenBytes = new byte[64];
        RandomNumberGenerator.Fill(tokenBytes);
        var tokenString = Convert.ToBase64String(tokenBytes);

        var refreshToken = new RefreshToken
        {
            Token = tokenString,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await _refreshTokenRepository.AddAsync(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync();

        return tokenString;
    }
}
