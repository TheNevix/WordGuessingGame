using Microsoft.EntityFrameworkCore;
using WordGuessingGame.Core.Data;
using WordGuessingGame.Core.Models;
using WordGuessingGame.Repository.Interfaces;

namespace WordGuessingGame.Repository.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AppUser?> GetByIdAsync(int id) =>
        await _context.Users.FindAsync(id);

    public async Task<AppUser?> GetByUsernameAsync(string username) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<AppUser?> GetByEmailAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<bool> UsernameExistsAsync(string username) =>
        await _context.Users.AnyAsync(u => u.Username == username);

    public async Task<bool> EmailExistsAsync(string email) =>
        await _context.Users.AnyAsync(u => u.Email == email);

    public async Task AddAsync(AppUser user) =>
        await _context.Users.AddAsync(user);

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
