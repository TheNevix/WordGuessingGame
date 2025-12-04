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

        public override Task OnConnectedAsync()
        {
            _gameService.AddPendingConnection(Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public async Task RegisterName(string name)
        {
            // Finalize the player
            var player = _gameService.RegisterName(Context.ConnectionId, name);
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
            return base.OnDisconnectedAsync(exception);
        }
    }
}
