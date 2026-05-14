using Microsoft.EntityFrameworkCore;
using WordGuessingGame.Core.Data;
using WordGuessingGame.Core.Models;
using WordGuessingGame.Repository.Interfaces;

namespace WordGuessingGame.Repository.Repositories;

public class RankedRepository : IRankedRepository
{
    private readonly AppDbContext _context;

    public RankedRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Season?> GetCurrentSeasonAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Seasons
            .Where(s => s.StartDate <= now)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();
    }

    public async Task<UserRankedStats> GetOrCreateStatsAsync(int userId, int seasonId)
    {
        var stats = await _context.UserRankedStats
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SeasonId == seasonId);

        if (stats == null)
        {
            stats = new UserRankedStats { UserId = userId, SeasonId = seasonId };
            _context.UserRankedStats.Add(stats);
        }

        return stats;
    }

    public async Task<List<UserRankedStats>> GetLeaderboardAsync(int seasonId, int top = 10)
    {
        return await _context.UserRankedStats
            .Where(s => s.SeasonId == seasonId)
            .Include(s => s.User)
            .OrderByDescending(s => s.RP)
            .Take(top)
            .ToListAsync();
    }

    public async Task<UserRankedStats?> GetStatsByUserAsync(int userId, int seasonId)
    {
        return await _context.UserRankedStats
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SeasonId == seasonId);
    }

    public async Task AddMatchHistoryAsync(RankedMatchHistory history)
    {
        _context.RankedMatchHistories.Add(history);
    }

    public async Task<List<RankedMatchHistory>> GetMatchHistoryAsync(int userId, int seasonId, int take = 10)
    {
        return await _context.RankedMatchHistories
            .Where(h => h.UserId == userId && h.SeasonId == seasonId)
            .OrderByDescending(h => h.PlayedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
