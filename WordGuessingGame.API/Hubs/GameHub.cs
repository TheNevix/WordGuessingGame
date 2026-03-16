using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using WordGuessingGame.API.Models;
using WordGuessingGame.API.Services;

namespace WordGuessingGame.API.Hubs
{
    public class GameHub : Hub
    {
        private readonly GameService _gameService;

        public GameHub(GameService gameService)
        {
            _gameService = gameService;
        }

        // Returns the authenticated user's DB id, or null for guests
        private int? GetAuthUserId()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        public override Task OnConnectedAsync()
        {
            _gameService.AddPendingConnection(Context.ConnectionId);
            return base.OnConnectedAsync();
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

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _gameService.DisconnectAsync(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
