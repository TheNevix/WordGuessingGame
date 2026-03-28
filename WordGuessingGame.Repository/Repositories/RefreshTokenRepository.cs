using Microsoft.EntityFrameworkCore;
using WordGuessingGame.Core.Data;
using WordGuessingGame.Core.Models;
using WordGuessingGame.Repository.Interfaces;

namespace WordGuessingGame.Repository.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
        => await _context.RefreshTokens
            .Include(r => r.User).ThenInclude(u => u.Tags)
            .FirstOrDefaultAsync(r => r.Token == token);

    public async Task AddAsync(RefreshToken refreshToken)
        => await _context.RefreshTokens.AddAsync(refreshToken);

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
