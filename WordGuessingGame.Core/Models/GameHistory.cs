namespace WordGuessingGame.Core.Models;

public class GameHistory
{
    public int Id { get; set; }
    public string Word { get; set; } = string.Empty;
    public string WinnerUsername { get; set; } = string.Empty;
    public int? WinnerUserId { get; set; }
    public string OpponentUsername { get; set; } = string.Empty;
    public int? OpponentUserId { get; set; }
    public int TotalGuesses { get; set; }
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public AppUser? Winner { get; set; }
    public AppUser? Opponent { get; set; }
}
