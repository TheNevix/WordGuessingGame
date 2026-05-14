namespace WordGuessingGame.Core.Models;

public class RankedMatchHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SeasonId { get; set; }
    public string OpponentName { get; set; } = string.Empty;
    public bool Won { get; set; }
    public int RPChange { get; set; }
    public int NewRP { get; set; }
    public int MySeriesWins { get; set; }
    public int OpponentSeriesWins { get; set; }
    public bool WasForfeit { get; set; }
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public Season Season { get; set; } = null!;
}
