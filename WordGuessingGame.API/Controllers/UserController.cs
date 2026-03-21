using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordGuessingGame.API.Models.DTOs;
using WordGuessingGame.Repository.Interfaces;

namespace WordGuessingGame.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IGameHistoryRepository _gameHistoryRepository;

    public UserController(IUserRepository userRepository, IGameHistoryRepository gameHistoryRepository)
    {
        _userRepository = userRepository;
        _gameHistoryRepository = gameHistoryRepository;
    }

    [HttpPut("avatar")]
    public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return NotFound();

        user.ProfilePictureUrl = request.ProfilePictureUrl;
        await _userRepository.SaveChangesAsync();

        return Ok(new { profilePictureUrl = user.ProfilePictureUrl });
    }

    [HttpPut("language")]
    public async Task<IActionResult> UpdateLanguage([FromBody] UpdateLanguageRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return NotFound();

        user.Language = request.Language;
        await _userRepository.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var games = await _gameHistoryRepository.GetByUserIdAsync(userId);

        var gamesPlayed = games.Count;
        var wins = games.Count(g => g.WinnerUserId == userId);
        var winRate = gamesPlayed > 0 ? Math.Round((double)wins / gamesPlayed * 100, 1) : 0;

        // Streak: consecutive wins from most recent game
        var streak = 0;
        foreach (var game in games) // already ordered by PlayedAt desc
        {
            if (game.WinnerUserId == userId)
                streak++;
            else
                break;
        }

        return Ok(new PlayerStatsResponse
        {
            GamesPlayed = gamesPlayed,
            Wins = wins,
            WinRate = winRate,
            Streak = streak
        });
    }
}
