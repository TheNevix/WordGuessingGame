namespace WordGuessingGame.Core.Models;

public class UserChallenge
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ChallengeId { get; set; }
    public int Progress { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsClaimed { get; set; }        // user must click Claim to receive reward
    public DateTime? CompletedAt { get; set; }

    public AppUser User { get; set; } = null!;
    public Challenge Challenge { get; set; } = null!;
}
