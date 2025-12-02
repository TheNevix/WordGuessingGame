using Microsoft.Extensions.Logging.Abstractions;

namespace WordGuessingGame.API.Models
{
    public class GameState
    {
        public string CurrentWord { get; set; } = string.Empty;
        public List<char> GuessedLetters { get; set; } = new List<char>();
        public bool IsGuessed { get; set; } = false;
        public User? Player1 { get; set; } = null;
        public User? Player2 { get; set; } = null;
        public string CurrengtTurnConnectionId { get; set; } = string.Empty;
    }
}
