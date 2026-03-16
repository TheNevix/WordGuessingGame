using WordGuessingGame.Core.Models;

namespace WordGuessingGame.Repository.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(int id);
    Task<AppUser?> GetByUsernameAsync(string username);
    Task<AppUser?> GetByEmailAsync(string email);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task AddAsync(AppUser user);
    Task SaveChangesAsync();
}
