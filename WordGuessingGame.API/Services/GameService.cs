using Microsoft.AspNetCore.SignalR;
using WordGuessingGame.API.Hubs;
using WordGuessingGame.API.Models;

namespace WordGuessingGame.API.Services
{
    public class GameService
    {
        private readonly Lobby _lobby;
        private readonly WordList _wordList;
        private readonly IHubContext<GameHub> _hub;
        private readonly Dictionary<string, User> _pendingPlayers = new();
        private readonly Dictionary<string, User> _namedPlayers = new();

        public GameService(Lobby lobby, WordList wordList, IHubContext<GameHub> hub)
        {
            _lobby = lobby;
            _wordList = wordList;
            _hub = hub;
        }

        public async Task AddPendingConnection(string connectionId)
        {
            _pendingPlayers[connectionId] = new User(connectionId);
            await _hub.Clients.Client(connectionId).SendAsync("Connected", connectionId);
        }

        public async Task RegisterName(string connectionId, string name)
        {
            var player = _pendingPlayers[connectionId];
            player.Username = name;

            // Move to named players
            _pendingPlayers.Remove(connectionId);
            _namedPlayers[connectionId] = player;

            // Try to join a game
            var game = _lobby.JoinPlayer(player);

            if (game == null)
            {
                return;
            }

            await _hub.Groups.AddToGroupAsync(game.Player1.ConnectionId, game.GameId.ToString());
            await _hub.Groups.AddToGroupAsync(game.Player2.ConnectionId, game.GameId.ToString());

            await StartGame(game);

        }

        public User GetPlayer(string connectionId)
        {
            return _namedPlayers.TryGetValue(connectionId, out var p) ? p : null;
        }

        public async Task StartGame(GameState game)
        {
            // Generates a word, resets game state, and notifies clients
            GenerateAndClearState(game);

            await _hub.Clients.Group(game.GameId.ToString()).SendAsync("GameStarted", new
            {
                CurrentWordLength = game.CurrentWord.Length,
                Player1 = game.Player1?.Username,
                Player2 = game.Player2?.Username,
                Turn = game.Player1.Username,
            });
        }

        public async Task RematchAsync(string connectionId)
        {
            var game = _lobby.GetGameByPlayerId(connectionId);

            var rematchCount = game.Rematch.Where(r => r).Count();

            if (rematchCount == 0)
            {
                game.Rematch[0] = true;
                await _hub.Clients.Group(game.GameId.ToString()).SendAsync("Rematch");
            }
            else
            {
                await StartGame(game);
            }
        }

        // 2 options: player disconnects during game, player disconnects while waiting
        public async Task DisconnectAsync(string connectionId)
        {
            // *******Still in lobby
            var game = _lobby.GetGameByPlayerId(connectionId);

            if (game is null)
            {
                _pendingPlayers.Remove(connectionId);
                _namedPlayers.Remove(connectionId);
                _lobby.DisconnectPlayer(connectionId);
                return;
            }

            // *******In game
            var opponent = game.Player1?.ConnectionId == connectionId
                ? game.Player2
                : game.Player1;

            // Send disconnect to other player
            await _hub.Clients.Client(opponent.ConnectionId).SendAsync("Disconnected");

            // Remove the named player
            _namedPlayers.Remove(connectionId);
            _pendingPlayers.Remove(connectionId);
            _lobby.RemovePlayerToGame(connectionId);
            _lobby.RemovePlayerToGame(opponent.ConnectionId);
            _lobby.DisconnectPlayer(connectionId);

            // Remove the game
            _lobby.EndGame(game.GameId);

            // Move the other player back to waiting
            _lobby.JoinPlayer(opponent);
        }

        public async Task GuessAsync(string connectionId, string guess)
        {
            var game = _lobby.GetGameByPlayerId(connectionId);

            var guessLength = guess.Length;

            if (guessLength == 1)
            {
                await HandleCharGuess(game, connectionId, guess[0]);
            }
            else
            {
                await HandleWordGuess(game, connectionId, guess);
            }
        }

        /// <summary>
        /// Handles a single character guess.
        /// </summary>
        /// <param name="guess">Recieved input which is a char</param>
        private async Task HandleCharGuess(GameState game, string connectionId, char guess)
        {
            // Letter guess
            if (!game.GuessedLetters.Contains(guess))
            {
                game.GuessedLetters.Add(guess);

                // Check if it is a correct guess
                var indexes = CheckIfGuessedWasCorrect(game, guess);

                // If it has correct indexes, check if we have all the guessed letters
                if (indexes.Count > 0)
                {
                    // Check if all letters have been guessed
                    var isGuessed = CheckIfGuessedByLetters(game);

                    // If they are all guesed, game is over
                    if (isGuessed)
                    {
                        await HandleWordGuessed(game, connectionId);
                        return;
                    }
                    else // Not all guessed, game is not over yet
                    {
                        // Prepare for next guess
                        await PrepareForNextGuess(game, connectionId, indexes, guess.ToString());
                        return;
                    }
                }
                else // Wrong guess
                {
                    // Prepare for next guess
                    await PrepareForNextGuess(game, connectionId, indexes, guess.ToString());
                    return;
                }
            }

            // Letter already was guessed, let them now and give turn to other player
            await PrepareForNextGuess(game, connectionId, new List<int>(), guess.ToString(), true);
        }

        private async Task HandleWordGuess(GameState game, string connectionId, string guess)
        {
            // Word guess
            if (string.Equals(guess, game.CurrentWord, StringComparison.OrdinalIgnoreCase))
            {
                await HandleWordGuessed(game, connectionId);
                return;
            }

            // Incorrect word guess, prepare for next turn
            await PrepareForNextGuess(game, connectionId, new List<int>(), guess);
        }

        private async Task HandleWordGuessed(GameState game, string connectionId)
        {
            game.IsGuessed = true;

            var user = game.Player1?.ConnectionId == connectionId
                ? game.Player1
                : game.Player2;

            // Send message to clients
            await _hub.Clients.Group(game.GameId.ToString()).SendAsync("WordGuessed", new
            {
                Message = $"{user.Username} has guessed the word '{game.CurrentWord}' correctly! Congratulations!",
                Winner = user.Username,
                Word = game.CurrentWord
            });
        }

        private async Task PrepareForNextGuess(GameState game, string connectionId, List<int> guessedLetterAppearsOnIndexes, string guess, bool didAlreadyExist = false)
        {
            // Current user
            var user = game.Player1?.ConnectionId == connectionId
                ? game.Player1
                : game.Player2;

            var opponent = game.Player1?.ConnectionId == connectionId
                ? game.Player2
                : game.Player1;

            // Change turns
            game.CurrentTurnUsername = game.Player1?.Username == game.CurrentTurnUsername
                ? game.Player2?.Username ?? string.Empty
                : game.Player1?.Username ?? string.Empty;

            // Notify clients about turn change
            if (didAlreadyExist && guess.Length == 1)
            {
                // Notify about already guessed letter
                await _hub.Clients.Group(game.GameId.ToString()).SendAsync("Guessed", new
                {
                    Guess = guess,
                    Message = $"{user.Username} has guessed '{guess}'. The guessed letter was already guessed before.",
                    CorrectGuess = false,
                    Turn = opponent.Username,
                });
            }
            else if (guessedLetterAppearsOnIndexes.Count == 0 && guess.Length == 1)
            {
                // Notify about incorrect guess
                await _hub.Clients.Group(game.GameId.ToString()).SendAsync("Guessed", new
                {
                    Guess = guess,
                    Message = $"{user.Username} has guessed '{guess}'. The word does not contain the letter '{guess}'.",
                    CorrectGuess = false,
                    Turn = opponent.Username,
                });
            }
            else if (guessedLetterAppearsOnIndexes.Count > 0 && guess.Length == 1) // Notify about correct guess
            {
                // Notify about incorrect guess
                await _hub.Clients.Group(game.GameId.ToString()).SendAsync("Guessed", new
                {
                    Guess = guess,
                    Message = $"{user.Username} has guessed '{guess}'!",
                    CorrectGuess = true,
                    Indexes = guessedLetterAppearsOnIndexes,
                    Turn = opponent.Username,
                });
            }
            else // Notify about incorrect word guess
            {
                await _hub.Clients.Group(game.GameId.ToString()).SendAsync("Guessed", new
                {
                    Guess = guess,
                    Message = $"{user.Username} has guessed the word '{guess}'! That was not the word that we are searching!",
                    CorrectGuess = false,
                    Turn = opponent.Username,
                });
            }
        }

        /// <summary>
        /// Checks if the current word has been completely guessed by letters.
        /// </summary>
        /// <returns></returns>
        private bool CheckIfGuessedByLetters(GameState game)
        {
            var word = game.CurrentWord;

            foreach (var letter in word)
            {
                if (!game.GuessedLetters.Contains(letter))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the current word has been completely guessed by letters.
        /// </summary>
        /// <returns>Returns a list of ints, representing the indexes where the letter was found</returns>
        private List<int> CheckIfGuessedWasCorrect(GameState game, char guess)
        {
            var word = game.CurrentWord;
            var indexes = new List<int>();

            for (int i = 0; i < word.Length; i++)
            {
                if (word[i] == guess)
                {
                    indexes.Add(i);
                }
            }

            return indexes;
        }

        private void GenerateAndClearState(GameState game)
        {
            game.Rematch = new List<bool> { false, false };
            game.CurrentWord = _wordList.Words[new Random().Next(_wordList.Words.Count)];
            game.GuessedLetters.Clear();
            game.IsGuessed = false;
            game.CurrentTurnUsername = game.Player1?.ConnectionId ?? string.Empty;
        }

    }
}
