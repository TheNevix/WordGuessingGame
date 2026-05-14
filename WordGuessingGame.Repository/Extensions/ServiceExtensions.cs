using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordGuessingGame.Core.Data;
using WordGuessingGame.Repository.Interfaces;
using WordGuessingGame.Repository.Repositories;


namespace WordGuessingGame.Repository.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddRepository(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IGameHistoryRepository, GameHistoryRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserChallengeRepository, UserChallengeRepository>();
        services.AddScoped<IRankedRepository, RankedRepository>();

        return services;
    }
}
