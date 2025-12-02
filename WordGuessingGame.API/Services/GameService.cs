using Microsoft.AspNetCore.SignalR;
using WordGuessingGame.API.Hubs;
using WordGuessingGame.API.Models;

namespace WordGuessingGame.API.Services
{
    public class GameService
    {
        private readonly GameState _gameState;
        private readonly WordList _wordList;
        private readonly IHubContext<GameHub> _hub;

        public GameService(GameState gameState, WordList wordList, IHubContext<GameHub> hub)
        {
            _gameState = gameState;
            _wordList = wordList;
            _hub = hub;
        }

        public void OnConnected(string connectionId)
        {
            var user = new User(connectionId);

            if (_gameState.Player1 == null)
            {
                _gameState.Player1 = user;
            }
            else if (_gameState.Player2 == null)
            {
                _gameState.Player2 = user;
            }
        }

        public async Task RegisterNameAsync(string connectionId, string name)
        {
            var user = _gameState.Player1?.ConnectionId == connectionId
                ? _gameState.Player1
                : _gameState.Player2;

            if (user == null)
            {
                return;
            }

            user.Username = name;

            // Game should only start when both players have registered
            await _hub.Clients.All.SendAsync("UserRegistered", user.Username);
        }

        public async Task StartGame()
        {
            // Generates a word, resets game state, and notifies clients
            GenerateAndClearState();

            await _hub.Clients.All.SendAsync("GameStarted", new
            {
                CurrentWordLength = _gameState.CurrentWord.Length,
                Player1 = _gameState.Player1?.Username,
                Player2 = _gameState.Player2?.Username,
                CurrentTurnConnectionId = _gameState.CurrengtTurnConnectionId
            });
        }

        public async Task GuessAsync(string connectionId, string guess)
        {
            var guessLength = guess.Length;

            if (guessLength == 1)
            {
                await HandleCharGuess(connectionId, guess[0]);
            }
            else
            {
                await HandleWordGuess(connectionId, guess);
            }
        }

        /// <summary>
        /// Handles a single character guess.
        /// </summary>
        /// <param name="guess">Recieved input which is a char</param>
        private async Task HandleCharGuess(string connectionId, char guess)
        {
            // Letter guess
            if (!_gameState.GuessedLetters.Contains(guess))
            {
                _gameState.GuessedLetters.Add(guess);

                // Check if it is a correct guess
                var indexes = CheckIfGuessedWasCorrect(guess);

                // If it has correct indexes, check if we have all the guessed letters
                if (indexes.Count > 0)
                {
                    // Check if all letters have been guessed
                    var isGuessed = CheckIfGuessedByLetters();

                    // If they are all guesed, game is over
                    if (isGuessed)
                    {
                        await HandleWordGuessed(connectionId);
                        return;
                    }
                    else // Not all guessed, game is not over yet
                    {
                        // Prepare for next guess
                        await PrepareForNextGuess(connectionId, indexes, guess.ToString());
                        return;
                    }
                }
                else // Wrong guess
                {
                    // Prepare for next guess
                    await PrepareForNextGuess(connectionId, indexes, guess.ToString());
                }
            }

            // Letter already was guessed, let them now and give turn to other player
            await PrepareForNextGuess(connectionId, new List<int>(), guess.ToString(), true);
        }

        private async Task HandleWordGuess(string connectionId, string guess)
        {
            // Word guess
            if (string.Equals(guess, _gameState.CurrentWord, StringComparison.OrdinalIgnoreCase))
            {
                await HandleWordGuessed(connectionId);
                return;
            }

            // Incorrect word guess, prepare for next turn
            await PrepareForNextGuess(connectionId, new List<int>(), guess);
        }

        private async Task HandleWordGuessed(string connectionId)
        {
            _gameState.IsGuessed = true;

            var user = _gameState.Player1?.ConnectionId == connectionId
                ? _gameState.Player1
                : _gameState.Player2;

            // Send message to clients
            await _hub.Clients.All.SendAsync("WordGuessed", new
            {
                Message = $"{user.Username} has guessed the word '{_gameState.CurrentWord}' correctly! Congratulations!",
                Winner = user.Username,
                Word = _gameState.CurrentWord
            });
        }

        private async Task PrepareForNextGuess(string connectionId, List<int> guessedLetterAppearsOnIndexes, string guess, bool didAlreadyExist = false)
        {
            // Current user
            var user = _gameState.Player1?.ConnectionId == connectionId
                ? _gameState.Player1
                : _gameState.Player2;

            // Change turns
            _gameState.CurrengtTurnConnectionId = _gameState.Player1?.ConnectionId == _gameState.CurrengtTurnConnectionId
                ? _gameState.Player2?.ConnectionId ?? string.Empty
                : _gameState.Player1?.ConnectionId ?? string.Empty;

            // Notify clients about turn change
            if (didAlreadyExist && guess.Length == 1)
            {
                // Notify about already guessed letter
                await _hub.Clients.All.SendAsync("Guessed", new
                {
                    Guess = guess,
                    Message = $"{user.Username} has guessed '{guess}'. The guessed letter was already guessed before.",
                    CorrectGuess = false,
                });
            }
            else if (guessedLetterAppearsOnIndexes.Count == 0 && guess.Length == 1)
            {
                // Notify about incorrect guess
                await _hub.Clients.All.SendAsync("Guessed", new
                {
                    Guess = guess,
                    Message = $"{user.Username} has guessed '{guess}'. The word does not contain the letter '{guess}'.",
                    CorrectGuess = false,
                });
            }
            else if (guessedLetterAppearsOnIndexes.Count > 0 && guess.Length == 1) // Notify about correct guess
            {
                // Notify about incorrect guess
                await _hub.Clients.All.SendAsync("Guessed", new
                {
                    Guess = guess,
                    Message = $"{user.Username} has guessed '{guess}'!",
                    CorrectGuess = true,
                    Indexes = guessedLetterAppearsOnIndexes
                });
            }
            else // Notify about incorrect word guess
            {
                await _hub.Clients.All.SendAsync("Guessed", new
                {
                    Guess = guess,
                    Message = $"{user.Username} has guessed the word '{guess}'! That was not the word that we are searching!",
                    CorrectGuess = false,
                });
            }
        }

        /// <summary>
        /// Checks if the current word has been completely guessed by letters.
        /// </summary>
        /// <returns></returns>
        private bool CheckIfGuessedByLetters()
        {
            var word = _gameState.CurrentWord;

            foreach (var letter in word)
            {
                if (!_gameState.GuessedLetters.Contains(letter))
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
        private List<int> CheckIfGuessedWasCorrect(char guess)
        {
            var word = _gameState.CurrentWord;
            var indexes = new List<int>();

            foreach (var letter in word)
            {
                if (letter == guess)
                {
                    indexes.Add(word.IndexOf(letter));
                }
            }

            return indexes;
        }

        private void GenerateAndClearState()
        {
            _gameState.CurrentWord = _wordList.Words[new Random().Next(_wordList.Words.Count)];
            _gameState.GuessedLetters.Clear();
            _gameState.IsGuessed = false;
            _gameState.CurrengtTurnConnectionId = _gameState.Player1?.ConnectionId ?? string.Empty;
        }

    }
}
