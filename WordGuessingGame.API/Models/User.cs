namespace WordGuessingGame.API.Models
{
    public class User
    {
        public string ConnectionId { get; set; }
        public string Username { get; set; } = string.Empty;

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
