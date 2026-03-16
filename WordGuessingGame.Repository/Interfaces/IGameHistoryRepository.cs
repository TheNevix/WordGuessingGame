using WordGuessingGame.Core.Models;

namespace WordGuessingGame.Repository.Interfaces;

public interface IGameHistoryRepository
{
    Task AddAsync(GameHistory gameHistory);
    Task SaveChangesAsync();
}
