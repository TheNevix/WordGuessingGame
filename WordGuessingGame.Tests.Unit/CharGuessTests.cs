using Microsoft.AspNetCore.SignalR;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using WordGuessingGame.API.Hubs;
using WordGuessingGame.API.Models;
using WordGuessingGame.API.Services;

namespace WordGuessingGame.Tests.Unit
{
    [TestFixture]
    internal class CharGuessTests
    {
        [Test]
        public async Task GuessAsync_CorrectLetter_ShouldBroadcastCorrectGuess()
        {
            // Arrange
            var state = new GameState
            {
                CurrentWord = "APPLE",
                Player1 = new User("conn-1", "Alice"),
                Player2 = new User("conn-2", "Bob"),
                CurrengtTurnConnectionId = "conn-1"
            };

            var wordList = new WordList(new[] { "APPLE" });

            var hub = new Mock<IHubContext<GameHub>>();
            var clients = new Mock<IHubClients>();
            var all = new Mock<IClientProxy>();

            hub.Setup(h => h.Clients).Returns(clients.Object);
            clients.Setup(c => c.All).Returns(all.Object);

            object[]? capturedArgs = null;

            all.Setup(x => x.SendCoreAsync("Guessed", It.IsAny<object[]>(), default))
               .Callback<string, object[], System.Threading.CancellationToken>((method, args, token) =>
               {
                   capturedArgs = args;
               })
               .Returns(Task.CompletedTask);

            var service = new GameService(state, wordList, hub.Object);

            // Act
            await service.GuessAsync("conn-1", "A");

            // Assert: letter is added
            Assert.That(state.GuessedLetters.Contains('A'));

            // Assert broadcast fired
            Assert.That(capturedArgs, Is.Not.Null);
            Assert.That(capturedArgs!.Length, Is.EqualTo(1));

            var payload = capturedArgs[0];

            // Now extract properties safely
            var guessProp = payload.GetType().GetProperty("Guess");
            var correctProp = payload.GetType().GetProperty("CorrectGuess");

            Assert.That(guessProp!.GetValue(payload)!.ToString(), Is.EqualTo("A"));
            Assert.That(correctProp!.GetValue(payload), Is.EqualTo(true));
        }


        [Test]
        public async Task GuessAsync_WrongLetter_ShouldBroadcastWrongGuess()
        {
            // Arrange
            var state = new GameState
            {
                CurrentWord = "APPLE",
                Player1 = new User("conn-1", "Alice"),
                Player2 = new User("conn-2", "Bob"),
                CurrengtTurnConnectionId = "conn-1"
            };

            var wordList = new WordList(new[] { "APPLE" });
            var hub = new Mock<IHubContext<GameHub>>();
            var clients = new Mock<IHubClients>();
            var all = new Mock<IClientProxy>();

            hub.Setup(h => h.Clients).Returns(clients.Object);
            clients.Setup(c => c.All).Returns(all.Object);

            object[]? capturedArgs = null;

            all.Setup(x => x.SendCoreAsync("Guessed", It.IsAny<object[]>(), default))
               .Callback<string, object[], System.Threading.CancellationToken>((method, args, token) =>
               {
                   capturedArgs = args;
               })
               .Returns(Task.CompletedTask);

            var service = new GameService(state, wordList, hub.Object);

            // Act
            await service.GuessAsync("conn-1", "Z");

            // Assert state changed
            Assert.That(state.GuessedLetters.Contains('Z'));

            // Assert broadcast fired
            Assert.That(capturedArgs, Is.Not.Null);

            var payload = capturedArgs![0];

            var guessProp = payload.GetType().GetProperty("Guess");
            var correctProp = payload.GetType().GetProperty("CorrectGuess");

            Assert.That(guessProp!.GetValue(payload)!.ToString(), Is.EqualTo("Z"));
            Assert.That(correctProp!.GetValue(payload), Is.EqualTo(false));
        }


        [Test]
        public async Task GuessAsync_LetterAlreadyGuessed_ShouldBroadcastAlreadyGuessed()
        {
            // Arrange
            var state = new GameState
            {
                CurrentWord = "APPLE",
                Player1 = new User("conn-1", "Alice"),
                Player2 = new User("conn-2", "Bob"),
                CurrengtTurnConnectionId = "conn-1",
                GuessedLetters = new List<char> { 'A' }
            };

            var wordList = new WordList(new[] { "APPLE" });
            var hub = new Mock<IHubContext<GameHub>>();
            var clients = new Mock<IHubClients>();
            var all = new Mock<IClientProxy>();

            hub.Setup(h => h.Clients).Returns(clients.Object);
            clients.Setup(c => c.All).Returns(all.Object);

            object[]? capturedArgs = null;

            all.Setup(x => x.SendCoreAsync("Guessed", It.IsAny<object[]>(), default))
               .Callback<string, object[], System.Threading.CancellationToken>((method, args, token) =>
               {
                   capturedArgs = args;
               })
               .Returns(Task.CompletedTask);

            var service = new GameService(state, wordList, hub.Object);

            // Act
            await service.GuessAsync("conn-1", "A");

            // Assert: A was not added twice
            Assert.That(state.GuessedLetters.Count, Is.EqualTo(1));

            Assert.That(capturedArgs, Is.Not.Null);

            var payload = capturedArgs![0];
            var messageProp = payload.GetType().GetProperty("Message");

            var messageValue = messageProp!.GetValue(payload)!.ToString();

            StringAssert.Contains("already guessed", messageValue!.ToLower());
        }
    }
}
