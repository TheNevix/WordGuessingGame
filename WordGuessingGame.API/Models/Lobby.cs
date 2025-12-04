namespace WordGuessingGame.API.Models
{
    public class Lobby
    {
        // List of ongoing games
        private readonly Dictionary<Guid, GameState> _games = new();

        private readonly Dictionary<string, Guid> _playerToGame = new();


        // List of players waiting for a game
        private readonly Queue<User> _waitingPlayers = new();

        public GameState JoinPlayer(User user)
        {
            // If someone is waiting → pair them with this new player
            if (_waitingPlayers.Count > 0)
            {
                var opponentUser = _waitingPlayers.Dequeue();

                // Create new game
                var gameId = Guid.NewGuid();
                var game = new GameState(gameId, opponentUser, user);

                _playerToGame[opponentUser.ConnectionId] = gameId;
                _playerToGame[user.ConnectionId] = gameId;

                _games.Add(gameId, game);

                return game;
            }

            // Otherwise add player to waiting queue
            _waitingPlayers.Enqueue(user);
            return null; // waiting
        }

        public GameState GetGame(Guid id)
        {
            return _games[id];
        }

        public GameState GetGameByPlayerId(string connectionId)
        {
            if (_playerToGame.TryGetValue(connectionId, out var gameId))
            {
                return _games[gameId];
            }

            return null; // Player is not in a game yet
        }
    }
}
