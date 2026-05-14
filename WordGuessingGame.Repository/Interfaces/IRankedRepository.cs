using WordGuessingGame.Core.Models;

namespace WordGuessingGame.Repository.Interfaces;

public interface IRankedRepository
{
    Task<Season?> GetCurrentSeasonAsync();
    Task<UserRankedStats> GetOrCreateStatsAsync(int userId, int seasonId);
    Task<List<UserRankedStats>> GetLeaderboardAsync(int seasonId, int top = 10);
    Task<UserRankedStats?> GetStatsByUserAsync(int userId, int seasonId);
    Task AddMatchHistoryAsync(RankedMatchHistory history);
    Task<List<RankedMatchHistory>> GetMatchHistoryAsync(int userId, int seasonId, int take = 10);
    Task SaveChangesAsync();
}
