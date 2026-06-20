using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using WordGuessingGame.API.Models;
using WordGuessingGame.API.Services;

namespace WordGuessingGame.API.Hubs
{
    public class GameHub : Hub
    {
        private readonly GameService _gameService;
        private readonly ILogger<GameHub> _logger;

        public GameHub(GameService gameService, ILogger<GameHub> logger)
        {
            _gameService = gameService;
            _logger = logger;
        }

        // Returns the authenticated user's DB id, or null for guests
        private int? GetAuthUserId()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        public override async Task OnConnectedAsync()
        {
            var appUserId = GetAuthUserId();
            _logger.LogInformation("[CONNECTED] connId={ConnId} appUserId={AppUserId}", Context.ConnectionId, appUserId);
            if (appUserId.HasValue && await _gameService.TryAutoReconnectAsync(Context.ConnectionId, appUserId.Value))
            {
                await base.OnConnectedAsync();
                return;
            }
            await _gameService.AddPendingConnection(Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public async Task RegisterName(string name)
        {
            await _gameService.RegisterName(Context.ConnectionId, name, GetAuthUserId());
        }

        public async Task CreatePrivateLobby(string name)
        {
            var code = _gameService.CreatePrivateLobby(Context.ConnectionId, name, GetAuthUserId());
            await Clients.Caller.SendAsync("PrivateLobbyCreated", code);
        }

        public async Task JoinPrivateLobby(string code, string name)
        {
            await _gameService.JoinPrivateLobbyAsync(Context.ConnectionId, name, GetAuthUserId(), code);
        }

        public async Task RegisterRanked(string name)
        {
            await _gameService.RegisterRanked(Context.ConnectionId, name, GetAuthUserId());
        }

        public Task CancelRanked()
        {
            _gameService.CancelRanked(Context.ConnectionId);
            return Task.CompletedTask;
        }

        // Can be a letter or a word
        public async Task Guess(string guess)
        {
            if (string.IsNullOrWhiteSpace(guess))
                return; // or handle error however you want

            await _gameService.GuessAsync(Context.ConnectionId, guess.ToUpper());
        }

        public async Task RematchVote()
        {
            await _gameService.RematchAsync(Context.ConnectionId);
        }

        // Fallback for guest players without a JWT
        public async Task Reconnect(string name)
        {
            await _gameService.ReconnectAsync(Context.ConnectionId, name);
        }

        public async Task LeaveGame()
        {
            await _gameService.DisconnectAsync(Context.ConnectionId);
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _gameService.HandleConnectionDroppedAsync(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
