using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordGuessingGame.API.Models.DTOs;
using WordGuessingGame.Core.Models;
using WordGuessingGame.Repository.Interfaces;

namespace WordGuessingGame.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IGameHistoryRepository _gameHistoryRepository;
    private readonly IUserChallengeRepository _challengeRepository;

    public UserController(IUserRepository userRepository, IGameHistoryRepository gameHistoryRepository, IUserChallengeRepository challengeRepository)
    {
        _userRepository = userRepository;
        _gameHistoryRepository = gameHistoryRepository;
        _challengeRepository = challengeRepository;
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

    [HttpPut("active-tag")]
    public async Task<IActionResult> UpdateActiveTag([FromBody] UpdateActiveTagRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return NotFound();

        user.ActiveTag = string.IsNullOrEmpty(request.ActiveTag) ? null : request.ActiveTag;
        await _userRepository.SaveChangesAsync();

        return Ok(new { activeTag = user.ActiveTag });
    }

    [HttpPut("banner-color")]
    public async Task<IActionResult> UpdateBannerColor([FromBody] UpdateBannerColorRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return NotFound();

        user.BannerColor = request.BannerColor;
        await _userRepository.SaveChangesAsync();

        return Ok(new { bannerColor = user.BannerColor });
    }

    [HttpGet("challenges")]
    public async Task<IActionResult> GetChallenges()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var allChallenges = await _challengeRepository.GetAllChallengesAsync();
        var userChallenges = await _challengeRepository.GetByUserIdAsync(userId);

        var result = allChallenges.Select(c =>
        {
            var uc = userChallenges.FirstOrDefault(u => u.ChallengeId == c.Id);
            return new ChallengeProgressResponse
            {
                ChallengeId  = c.Id,
                Key          = c.Key,
                Progress     = uc?.Progress ?? 0,
                Target       = c.TargetValue,
                IsCompleted  = uc?.IsCompleted ?? false,
                IsClaimed    = uc?.IsClaimed ?? false,
                RewardType   = c.RewardType.ToString(),
                RewardValue  = c.RewardValue,
            };
        }).ToList();

        return Ok(result);
    }

    [HttpPost("challenges/{challengeId}/claim")]
    public async Task<IActionResult> ClaimChallenge(int challengeId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var uc = await _challengeRepository.GetOrCreateAsync(userId, challengeId);
        if (!uc.IsCompleted)
            return BadRequest(new { message = "Challenge not completed yet." });
        if (uc.IsClaimed)
            return BadRequest(new { message = "Reward already claimed." });

        // Load challenge to know what reward to apply
        var challenges = await _challengeRepository.GetAllChallengesAsync();
        var challenge = challenges.FirstOrDefault(c => c.Id == challengeId);
        if (challenge == null)
            return NotFound();

        uc.IsClaimed = true;

        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            if (challenge.RewardType == RewardType.Tag)
            {
                if (!user.Tags.Any(t => t.Name == challenge.RewardValue))
                    user.Tags.Add(new UserTag { Name = challenge.RewardValue });
            }
            else if (challenge.RewardType == RewardType.BannerColor)
            {
                user.BannerColor = challenge.RewardValue;
            }
        }

        await _challengeRepository.SaveChangesAsync();
        await _userRepository.SaveChangesAsync();

        return Ok(new
        {
            rewardType  = challenge.RewardType.ToString(),
            rewardValue = challenge.RewardValue,
            tags        = user?.Tags.Select(t => t.Name).ToList(),
            bannerColor = user?.BannerColor,
        });
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
