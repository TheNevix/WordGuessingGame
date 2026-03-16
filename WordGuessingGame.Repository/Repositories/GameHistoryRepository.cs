using WordGuessingGame.Core.Data;
using WordGuessingGame.Core.Models;
using WordGuessingGame.Repository.Interfaces;

namespace WordGuessingGame.Repository.Repositories;

public class GameHistoryRepository : IGameHistoryRepository
{
    private readonly AppDbContext _context;

    public GameHistoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(GameHistory gameHistory) =>
        await _context.GameHistories.AddAsync(gameHistory);

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
