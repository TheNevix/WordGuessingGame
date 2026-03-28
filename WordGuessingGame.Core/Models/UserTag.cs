namespace WordGuessingGame.Core.Models;

public class UserTag
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;

    public AppUser User { get; set; } = null!;
}
