namespace WordGuessingGame.API.Models.DTOs;

public class ChallengeProgressResponse
{
    public int ChallengeId { get; set; }
    public string Key { get; set; } = string.Empty;
    public int Progress { get; set; }
    public int Target { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsClaimed { get; set; }
    public string RewardType { get; set; } = string.Empty;  // "Tag" | "BannerColor"
    public string RewardValue { get; set; } = string.Empty; // tag name or hex color
}
