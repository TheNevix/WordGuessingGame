using Microsoft.EntityFrameworkCore;
using WordGuessingGame.Core.Data;
using WordGuessingGame.Core.Models;
using WordGuessingGame.Repository.Interfaces;

namespace WordGuessingGame.Repository.Repositories;

public class UserChallengeRepository : IUserChallengeRepository
{
    private readonly AppDbContext _context;

    public UserChallengeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Challenge>> GetAllChallengesAsync() =>
        await _context.Challenges.ToListAsync();

    public async Task<List<UserChallenge>> GetByUserIdAsync(int userId) =>
        await _context.UserChallenges
            .Include(u => u.Challenge)
            .Where(u => u.UserId == userId)
            .ToListAsync();

    public async Task<UserChallenge> GetOrCreateAsync(int userId, int challengeId)
    {
        var uc = await _context.UserChallenges
            .FirstOrDefaultAsync(u => u.UserId == userId && u.ChallengeId == challengeId);

        if (uc == null)
        {
            uc = new UserChallenge { UserId = userId, ChallengeId = challengeId };
            await _context.UserChallenges.AddAsync(uc);
        }

        return uc;
    }

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
