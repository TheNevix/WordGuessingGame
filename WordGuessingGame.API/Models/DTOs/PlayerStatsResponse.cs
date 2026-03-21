namespace WordGuessingGame.API.Models.DTOs;

public class PlayerStatsResponse
{
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public double WinRate { get; set; }
    public int Streak { get; set; }
}
