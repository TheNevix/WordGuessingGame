using Microsoft.AspNetCore.SignalR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordGuessingGame.API.Hubs;
using WordGuessingGame.API.Models;
using WordGuessingGame.API.Services;

namespace WordGuessingGame.Tests.Unit
{
    [TestFixture]
    internal class GameServiceTests
    {
        [Test]
        public void OnConnected_FirstConnection_AssignsPlayer1()
        {
            // Arrange
            var state = new GameState();
            var wordList = new WordList(new[] { "APPLE" });
            var hub = Mock.Of<IHubContext<GameHub>>(); // not used here
            var service = new GameService(state, wordList, hub);

            // Act
            service.OnConnected("conn-1");

            // Assert
            Assert.That(state.Player1, Is.Not.Null);
            Assert.That(state.Player1.ConnectionId, Is.EqualTo("conn-1"));
            Assert.That(state.Player2, Is.Null);
        }

        [Test]
        public void OnConnected_SecondConnection_AssignsPlayer2()
        {
            // Arrange
            var state = new GameState();
            var wordList = new WordList(new[] { "APPLE" });
            var hub = Mock.Of<IHubContext<GameHub>>();
            var service = new GameService(state, wordList, hub);

            // Act
            service.OnConnected("conn-1");
            service.OnConnected("conn-2");

            // Assert
            Assert.That(state.Player2, Is.Not.Null);
            Assert.That(state.Player2.ConnectionId, Is.EqualTo("conn-2"));
        }

        [Test]
        public async Task StartGame_ShouldGenerateWord_ResetState_AndBroadcast()
        {
            // Arrange
            var state = new GameState();
            state.Player1 = new User("conn-1", "Alice");
            state.Player2 = new User("conn-2", "Bob");

            var wordList = new WordList(new[] { "APPLE", "HOUSE", "TRAIN" });

            // Mock SignalR hub broadcast chain
            var hubContextMock = new Mock<IHubContext<GameHub>>();
            var clientsMock = new Mock<IHubClients>();
            var allClientsMock = new Mock<IClientProxy>();

            hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);
            clientsMock.Setup(c => c.All).Returns(allClientsMock.Object);

            var service = new GameService(state, wordList, hubContextMock.Object);

            // Act
            await service.StartGame();

            // Assert: word must be set
            Assert.That(state.CurrentWord, Is.Not.Null.And.Not.Empty);

            // Assert: guessed letters cleared
            Assert.That(state.GuessedLetters.Count, Is.EqualTo(0));

            // Assert: guess flag reset
            Assert.That(state.IsGuessed, Is.False);

            // Assert: turn set to Player1
            Assert.That(state.CurrengtTurnConnectionId, Is.EqualTo("conn-1"));

            // Assert: broadcast sent
            allClientsMock.Verify(
                x => x.SendCoreAsync(
                    "GameStarted",
                    It.Is<object[]>(args =>
                        args != null &&
                        args.Length == 1 &&
                        HasExpectedPayload(args[0], state)
                    ),
                    default
                ),
                Times.Once
            );
        }

        // Helper to validate anonymous object payload
        private bool HasExpectedPayload(object payload, GameState state)
        {
            var dict = ToDictionary(payload);

            return
                (int)dict["CurrentWordLength"] == state.CurrentWord.Length &&
                (string)dict["Player1"] == state.Player1.Username &&
                (string)dict["Player2"] == state.Player2.Username &&
                (string)dict["CurrentTurnConnectionId"] == state.CurrengtTurnConnectionId;
        }

        private Dictionary<string, object> ToDictionary(object obj)
        {
            return obj.GetType()
                .GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(obj)!);
        }
    }
}
