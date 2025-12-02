using Microsoft.AspNetCore.SignalR;
using WordGuessingGame.API.Models;
using WordGuessingGame.API.Services;

namespace WordGuessingGame.API.Hubs
{
    public class GameHub : Hub
    {
        private GameService _gameService;

        public GameHub(GameService gameService)
        {
            _gameService = gameService;
        }

        public override Task OnConnectedAsync()
        {
            _gameService.OnConnected(Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public async Task RegisterName(string name)
        {
            await _gameService.RegisterNameAsync(Context.ConnectionId, name);

            // Start the game if both players have registered
            await _gameService.StartGame();
        }

        // Can be a letter or a word
        public async Task Guess(string guess)
        {
            await _gameService.GuessAsync(Context.ConnectionId, guess);
        }
    }
}
