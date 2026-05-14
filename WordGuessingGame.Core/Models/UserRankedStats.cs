namespace WordGuessingGame.Core.Models;

public class UserRankedStats
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SeasonId { get; set; }
    public int RP { get; set; } = 0;
    public int PeakRP { get; set; } = 0;
    public int Wins { get; set; } = 0;
    public int Losses { get; set; } = 0;

    public AppUser User { get; set; } = null!;
    public Season Season { get; set; } = null!;

    public RankedTier Tier => RP switch
    {
        >= 800 => RankedTier.Oracle,
        >= 525 => RankedTier.Sage,
        >= 325 => RankedTier.Scholar,
        >= 175 => RankedTier.Wordsmith,
        >= 75  => RankedTier.Reader,
        _      => RankedTier.Scribbler
    };
}
