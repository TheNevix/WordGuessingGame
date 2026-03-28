using System.Security.Cryptography;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using WordGuessingGame.API.Hubs;
using WordGuessingGame.API.Models;
using WordGuessingGame.Core.Models;
using WordGuessingGame.Repository.Interfaces;
using RewardType = WordGuessingGame.Core.Models.RewardType;

namespace WordGuessingGame.API.Services
{
    public class GameService
    {
        private readonly Lobby _lobby;
        private readonly WordList _wordList;
        private readonly IHubContext<GameHub> _hub;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Dictionary<string, User> _pendingPlayers = new();
        private readonly Dictionary<string, User> _namedPlayers = new();
        private record PrivateLobbyEntry(User Creator, DateTime ExpiresAt);
        private readonly Dictionary<string, PrivateLobbyEntry> _privateLobbies = new();
        private readonly Dictionary<string, CancellationTokenSource> _botTimers = new();

        private const int BotMatchDelaySecs = 15;
        private static readonly string[] BotNames = { "Bot 🤖", "HAL 🤖", "R2D2 🤖" };

        public GameService(Lobby lobby, WordList wordList, IHubContext<GameHub> hub, IServiceScopeFactory scopeFactory)
        {
            _lobby = lobby;
            _wordList = wordList;
            _hub = hub;
            _scopeFactory = scopeFactory;
        }

        public async Task AddPendingConnection(string connectionId)
        {
            _pendingPlayers[connectionId] = new User(connectionId);
            await _hub.Clients.Client(connectionId).SendAsync("Connected", connectionId);
        }

        public async Task RegisterName(string connectionId, string name, int? appUserId)
        {
            var player = _pendingPlayers[connectionId];
            player.Username = name;
            player.AppUserId = appUserId;

            // Move to named players
            _pendingPlayers.Remove(connectionId);
            _namedPlayers[connectionId] = player;

            // Try to join a game
            var game = _lobby.JoinPlayer(player);

            if (game == null)
            {
                // No opponent yet — schedule a bot match after the delay
                var cts = new CancellationTokenSource();
                _botTimers[connectionId] = cts;
                _ = ScheduleBotMatchAsync(player, cts.Token);
                return;
            }

            // Natural match found — cancel any bot timer for the opponent
            CancelBotTimer(game.Player1!.ConnectionId);
            CancelBotTimer(game.Player2!.ConnectionId);

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
            // Fetch profile pictures for authenticated players
            await FetchProfilePictures(game);

            // Notify clients that a match was found (show countdown)
            await _hub.Clients.Group(game.GameId.ToString()).SendAsync("MatchFound", new
            {
                Player1 = game.Player1?.Username,
                Player2 = game.Player2?.Username,
                Player1Pfp = game.Player1?.ProfilePictureUrl,
                Player2Pfp = game.Player2?.ProfilePictureUrl,
                Player1BannerColor = game.Player1?.BannerColor ?? "#5b21b6",
                Player2BannerColor = game.Player2?.BannerColor ?? "#5b21b6",
                Player1ActiveTag = game.Player1?.ActiveTag,
                Player2ActiveTag = game.Player2?.ActiveTag,
                Player1Tags = game.Player1?.Tags ?? new List<string>(),
                Player2Tags = game.Player2?.Tags ?? new List<string>(),
            });

            // 5-second countdown before starting
            await Task.Delay(5000);

            // Generates a word, resets game state, and notifies clients
            GenerateAndClearState(game);

            await _hub.Clients.Group(game.GameId.ToString()).SendAsync("GameStarted", new
            {
                CurrentWordLength = game.CurrentWord.Length,
                Player1 = game.Player1?.Username,
                Player2 = game.Player2?.Username,
                Turn = game.Player1.Username,
            });

            // If the bot goes first, kick off its turn
            if (game.Player1?.IsBot == true)
                _ = ScheduleBotTurnAsync(game);
        }

        private async Task FetchProfilePictures(GameState game)
        {
            using var scope = _scopeFactory.CreateScope();
            var userRepo = scope.ServiceProvider.GetRequiredService<WordGuessingGame.Repository.Interfaces.IUserRepository>();

            if (game.Player1?.AppUserId.HasValue == true)
            {
                var dbUser = await userRepo.GetByIdAsync(game.Player1.AppUserId.Value);
                if (dbUser != null)
                {
                    game.Player1.ProfilePictureUrl = dbUser.ProfilePictureUrl;
                    game.Player1.BannerColor = dbUser.BannerColor;
                    game.Player1.ActiveTag = dbUser.ActiveTag;
                    game.Player1.Tags = dbUser.Tags.Select(t => t.Name).ToList();
                }
            }

            if (game.Player2?.AppUserId.HasValue == true)
            {
                var dbUser = await userRepo.GetByIdAsync(game.Player2.AppUserId.Value);
                if (dbUser != null)
                {
                    game.Player2.ProfilePictureUrl = dbUser.ProfilePictureUrl;
                    game.Player2.BannerColor = dbUser.BannerColor;
                    game.Player2.ActiveTag = dbUser.ActiveTag;
                    game.Player2.Tags = dbUser.Tags.Select(t => t.Name).ToList();
                }
            }
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
            // Cancel any pending bot match timer
            CancelBotTimer(connectionId);

            // Clean up any private lobby this player created
            var expiredCodes = _privateLobbies.Where(kv => kv.Value.Creator.ConnectionId == connectionId).Select(kv => kv.Key).ToList();
            foreach (var key in expiredCodes) _privateLobbies.Remove(key);

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

            // Only put opponent back in public queue for public games (never re-queue bots)
            if (!game.IsPrivate && !opponent.IsBot)
                _lobby.JoinPlayer(opponent);
        }

        public async Task GuessAsync(string connectionId, string guess)
        {
            var game = _lobby.GetGameByPlayerId(connectionId);

            game.TotalGuesses++;

            if (guess.Length == 1)
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

            var winner = game.Player1?.ConnectionId == connectionId ? game.Player1 : game.Player2;
            var opponent = game.Player1?.ConnectionId == connectionId ? game.Player2 : game.Player1;

            await _hub.Clients.Group(game.GameId.ToString()).SendAsync("WordGuessed", new
            {
                Winner = winner!.Username,
                Word = game.CurrentWord
            });

            // Save history if at least one player is authenticated
            if (winner.AppUserId.HasValue || opponent!.AppUserId.HasValue)
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IGameHistoryRepository>();

                await repo.AddAsync(new GameHistory
                {
                    Word = game.CurrentWord,
                    WinnerUsername = winner.Username,
                    WinnerUserId = winner.AppUserId,
                    OpponentUsername = opponent!.Username,
                    OpponentUserId = opponent.AppUserId,
                    TotalGuesses = game.TotalGuesses,
                    PlayedAt = DateTime.UtcNow
                });

                await repo.SaveChangesAsync();
            }

            await EvaluateChallengesAsync(winner, opponent!);
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
                await _hub.Clients.Group(game.GameId.ToString()).SendAsync("Guessed", new
                {
                    Username = user.Username,
                    Guess = guess,
                    MessageType = "letter_duplicate",
                    CorrectGuess = false,
                    Turn = opponent.Username,
                });
            }
            else if (guessedLetterAppearsOnIndexes.Count == 0 && guess.Length == 1)
            {
                await _hub.Clients.Group(game.GameId.ToString()).SendAsync("Guessed", new
                {
                    Username = user.Username,
                    Guess = guess,
                    MessageType = "letter_wrong",
                    CorrectGuess = false,
                    Turn = opponent.Username,
                });
            }
            else if (guessedLetterAppearsOnIndexes.Count > 0 && guess.Length == 1)
            {
                await _hub.Clients.Group(game.GameId.ToString()).SendAsync("Guessed", new
                {
                    Username = user.Username,
                    Guess = guess,
                    MessageType = "letter_correct",
                    CorrectGuess = true,
                    Indexes = guessedLetterAppearsOnIndexes,
                    Turn = opponent.Username,
                });
            }
            else
            {
                await _hub.Clients.Group(game.GameId.ToString()).SendAsync("Guessed", new
                {
                    Username = user.Username,
                    Guess = guess,
                    MessageType = "word_wrong",
                    CorrectGuess = false,
                    Turn = opponent.Username,
                });
            }

            // If it's now the bot's turn, schedule an automatic guess
            var botPlayer = game.Player1?.IsBot == true ? game.Player1 : (game.Player2?.IsBot == true ? game.Player2 : null);
            if (botPlayer != null && game.CurrentTurnUsername == botPlayer.Username)
                _ = ScheduleBotTurnAsync(game);
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

        private async Task EvaluateChallengesAsync(User winner, User loser)
        {
            if (!winner.AppUserId.HasValue && !loser.AppUserId.HasValue) return;

            using var scope = _scopeFactory.CreateScope();
            var challengeRepo = scope.ServiceProvider.GetRequiredService<IUserChallengeRepository>();

            var challenges = await challengeRepo.GetAllChallengesAsync();

            // Winner: increment WinCount + WinStreak progress
            if (winner.AppUserId.HasValue)
            {
                foreach (var challenge in challenges)
                {
                    var uc = await challengeRepo.GetOrCreateAsync(winner.AppUserId.Value, challenge.Id);
                    if (uc.IsCompleted) continue;

                    uc.Progress++;

                    if (uc.Progress >= challenge.TargetValue)
                    {
                        uc.IsCompleted = true;
                        uc.CompletedAt = DateTime.UtcNow;
                        // Reward is NOT applied here — user must click Claim
                    }
                }
            }

            // Loser: reset WinStreak progress
            if (loser.AppUserId.HasValue)
            {
                foreach (var challenge in challenges.Where(c => c.Type == ChallengeType.WinStreak))
                {
                    var uc = await challengeRepo.GetOrCreateAsync(loser.AppUserId.Value, challenge.Id);
                    if (!uc.IsCompleted)
                        uc.Progress = 0;
                }
            }

            await challengeRepo.SaveChangesAsync();
        }

        private void CancelBotTimer(string connectionId)
        {
            if (_botTimers.TryGetValue(connectionId, out var cts))
            {
                cts.Cancel();
                _botTimers.Remove(connectionId);
            }
        }

        private async Task ScheduleBotMatchAsync(User human, CancellationToken ct)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(BotMatchDelaySecs), ct);
            }
            catch (TaskCanceledException) { return; }

            _botTimers.Remove(human.ConnectionId);

            try
            {
                var botName = BotNames[new Random().Next(BotNames.Length)];
                var bot = new User($"BOT_{Guid.NewGuid()}", botName) { IsBot = true };

                var game = _lobby.StartBotGame(human, bot);
                if (game == null) return; // human was already matched naturally

                await _hub.Groups.AddToGroupAsync(human.ConnectionId, game.GameId.ToString());
                await StartGame(game);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BOT] Error starting bot match: {ex.Message}");
            }
        }

        private async Task ScheduleBotTurnAsync(GameState game)
        {
            var bot = game.Player1?.IsBot == true ? game.Player1 : game.Player2;
            if (bot == null || game.CurrentTurnUsername != bot.Username) return;

            // Random delay between 1.5 – 3.5 seconds to feel natural
            var delay = new Random().Next(1500, 3500);
            await Task.Delay(delay);

            if (game.IsGuessed) return;

            // Pick a random unguessed letter from the word
            var unguessed = game.CurrentWord
                .Where(c => !game.GuessedLetters.Contains(c))
                .Distinct()
                .ToList();

            if (unguessed.Count == 0) return;

            var guess = unguessed[new Random().Next(unguessed.Count)].ToString();
            await GuessAsync(bot.ConnectionId, guess);
        }

        public string CreatePrivateLobby(string connectionId, string name, int? appUserId)
        {
            var player = _pendingPlayers[connectionId];
            player.Username = name;
            player.AppUserId = appUserId;

            _pendingPlayers.Remove(connectionId);
            _namedPlayers[connectionId] = player;

            var code = GeneratePrivateCode();
            _privateLobbies[code] = new PrivateLobbyEntry(player, DateTime.UtcNow.AddMinutes(10));
            return code;
        }

        public async Task JoinPrivateLobbyAsync(string connectionId, string name, int? appUserId, string code)
        {
            var upperCode = code.ToUpper();

            if (!_privateLobbies.TryGetValue(upperCode, out var entry) || DateTime.UtcNow > entry.ExpiresAt)
            {
                _privateLobbies.Remove(upperCode);
                await _hub.Clients.Client(connectionId).SendAsync("PrivateLobbyError", "Invalid or expired invite code.");
                return;
            }

            _privateLobbies.Remove(upperCode);

            var joiner = _pendingPlayers[connectionId];
            joiner.Username = name;
            joiner.AppUserId = appUserId;
            _pendingPlayers.Remove(connectionId);
            _namedPlayers[connectionId] = joiner;

            var gameId = Guid.NewGuid();
            var game = _lobby.StartPrivateGame(gameId, entry.Creator, joiner);

            await _hub.Groups.AddToGroupAsync(entry.Creator.ConnectionId, gameId.ToString());
            await _hub.Groups.AddToGroupAsync(joiner.ConnectionId, gameId.ToString());

            await StartGame(game);
        }

        private static string GeneratePrivateCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var bytes = new byte[6];
            RandomNumberGenerator.Fill(bytes);
            return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        }

        private void GenerateAndClearState(GameState game)
        {
            game.Rematch = new List<bool> { false, false };
            game.CurrentWord = _wordList.Words[new Random().Next(_wordList.Words.Count)];
            Console.WriteLine($"[DEBUG] Word selected: {game.CurrentWord}");
            game.GuessedLetters.Clear();
            game.IsGuessed = false;
            game.TotalGuesses = 0;
            game.CurrentTurnUsername = game.Player1?.Username ?? string.Empty;
        }

    }
}
