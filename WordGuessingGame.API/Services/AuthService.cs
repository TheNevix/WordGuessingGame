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
    private readonly IConfiguration _config;

    public AuthService(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository, IConfiguration config)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _config = config;
    }

    private static string MapLanguage(Core.Models.Language lang) =>
        lang == Core.Models.Language.English ? "en" : "nl";

    private static List<string> MapTags(AppUser user) =>
        user.Tags.Select(t => t.Name).ToList();

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user is null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var response = new AuthResponse
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
            response.RefreshToken = await CreateRefreshTokenAsync(user.Id);

        return response;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.UsernameExistsAsync(request.Username))
            return null;

        if (await _userRepository.EmailExistsAsync(request.Email))
            return null;

        var user = new AppUser
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Tags = new List<UserTag> { new UserTag { Name = "OG" } }
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return new AuthResponse
        {
            Token = GenerateAccessToken(user, rememberMe: false),
            Username = user.Username,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Language = MapLanguage(user.Language),
            BannerColor = user.BannerColor,
            ActiveTag = user.ActiveTag,
            Tags = MapTags(user)
        };
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (token is null || !token.IsActive)
            return null;

        // Rotate: revoke old, issue new
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

        // Short-lived when rememberMe (refresh token handles renewal), longer otherwise
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
