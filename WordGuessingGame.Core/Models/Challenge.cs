namespace WordGuessingGame.Core.Models;

public class Challenge
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;   // i18n key e.g. "win_5_games"
    public ChallengeType Type { get; set; }
    public int TargetValue { get; set; }
    public RewardType RewardType { get; set; }
    public string RewardValue { get; set; } = string.Empty; // tag name OR hex color

    public ICollection<UserChallenge> UserChallenges { get; set; } = new List<UserChallenge>();
}
