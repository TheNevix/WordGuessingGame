using Microsoft.Extensions.Logging.Abstractions;

namespace WordGuessingGame.API.Models
{
    public class GameState
    {
        public Guid GameId { get; set; }
        public string CurrentWord { get; set; } = string.Empty;
        public List<char> GuessedLetters { get; set; } = new List<char>();
        public bool IsGuessed { get; set; } = false;
        public User? Player1 { get; set; } = null;
        public User? Player2 { get; set; } = null;
        public string CurrentTurnUsername { get; set; } = string.Empty;
        public List<bool> Rematch { get; set; } = new List<bool> { false, false };

        public GameState(Guid gameId, User opponent, User user)
        {
            GameId = gameId;
            Player1 = opponent;
            Player2 = user;
        }
    }
}
