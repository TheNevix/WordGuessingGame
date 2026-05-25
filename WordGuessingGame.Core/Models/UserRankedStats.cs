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
        >= 800 => RankedTier.Kampioen,
        >= 525 => RankedTier.Diamant,
        >= 325 => RankedTier.Platina,
        >= 175 => RankedTier.Goud,
        >= 75  => RankedTier.Zilver,
        _      => RankedTier.Brons
    };
}
