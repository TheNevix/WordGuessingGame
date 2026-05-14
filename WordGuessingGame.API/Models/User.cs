namespace WordGuessingGame.API.Models
{
    public class User
    {
        public string ConnectionId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int? AppUserId { get; set; } // null for guests
        public string? ProfilePictureUrl { get; set; }
        public bool IsBot { get; set; } = false;
        public string BannerColor { get; set; } = "#5b21b6";
        public string? ActiveTag { get; set; }
        public List<string> Tags { get; set; } = new();
        public int RankedRP { get; set; } = 0;
        public string BotDifficulty { get; set; } = "easy";

        public User(string connectionId)
        {
            ConnectionId = connectionId;
        }

        public User(string connectionId, string username)
        {
            ConnectionId = connectionId;
            Username = username;
        }
    }
}
