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
        public int TotalGuesses { get; set; } = 0;
        public bool IsPrivate { get; set; } = false;
        public bool IsRanked { get; set; } = false;
        public int Player1SeriesWins { get; set; } = 0;
        public int Player2SeriesWins { get; set; } = 0;
        public bool SeriesComplete { get; set; } = false;
        public CancellationTokenSource? GuessCts { get; set; }
        // UTC time at which the current ranked turn timer expires — used to restore the
        // countdown with the correct remaining seconds when a player reconnects.
        public DateTime? GuessTurnEndsAt { get; set; }

        public GameState(Guid gameId, User opponent, User user)
        {
            GameId = gameId;
            Player1 = opponent;
            Player2 = user;
        }
    }
}
