using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordGuessingGame.API.Models.DTOs;
using WordGuessingGame.Core.Models;
using WordGuessingGame.Repository.Interfaces;

namespace WordGuessingGame.API.Controllers;

[ApiController]
[Route("api/ranked")]
public class RankedController : ControllerBase
{
    private readonly IRankedRepository _rankedRepo;

    public RankedController(IRankedRepository rankedRepo)
    {
        _rankedRepo = rankedRepo;
    }

    [Authorize]
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var season = await _rankedRepo.GetCurrentSeasonAsync();
        if (season == null) return Ok(new RankedStatsResponse { SeasonName = "Off-season" });

        var stats = await _rankedRepo.GetStatsByUserAsync(userId, season.Id);
        if (stats == null)
            return Ok(new RankedStatsResponse { RP = 0, Tier = RankedTier.Scribbler.ToString(), SeasonName = season.Name });

        return Ok(new RankedStatsResponse
        {
            RP = stats.RP,
            PeakRP = stats.PeakRP,
            Tier = stats.Tier.ToString(),
            Wins = stats.Wins,
            Losses = stats.Losses,
            SeasonName = season.Name
        });
    }

    [Authorize]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var season = await _rankedRepo.GetCurrentSeasonAsync();
        if (season == null) return Ok(new List<object>());

        var history = await _rankedRepo.GetMatchHistoryAsync(userId, season.Id, 10);
        return Ok(history.Select(h => new
        {
            h.OpponentName,
            h.Won,
            h.RPChange,
            h.NewRP,
            h.MySeriesWins,
            h.OpponentSeriesWins,
            h.WasForfeit,
            h.PlayedAt
        }));
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard()
    {
        var season = await _rankedRepo.GetCurrentSeasonAsync();
        if (season == null) return Ok(new List<LeaderboardEntryResponse>());

        var entries = await _rankedRepo.GetLeaderboardAsync(season.Id, 10);
        var result = entries.Select((e, i) => new LeaderboardEntryResponse
        {
            Rank = i + 1,
            Username = e.User.Username,
            ProfilePictureUrl = e.User.ProfilePictureUrl,
            RP = e.RP,
            Tier = e.Tier.ToString()
        }).ToList();

        return Ok(result);
    }
}
