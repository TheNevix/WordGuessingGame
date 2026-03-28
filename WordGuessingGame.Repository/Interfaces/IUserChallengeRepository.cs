using WordGuessingGame.Core.Models;

namespace WordGuessingGame.Repository.Interfaces;

public interface IUserChallengeRepository
{
    Task<List<Challenge>> GetAllChallengesAsync();
    Task<List<UserChallenge>> GetByUserIdAsync(int userId);
    Task<UserChallenge> GetOrCreateAsync(int userId, int challengeId);
    Task SaveChangesAsync();
}
