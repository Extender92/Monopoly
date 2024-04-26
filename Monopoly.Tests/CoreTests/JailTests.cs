using Monopoly.Core.Events;
using Monopoly.Core.Interface;
using Monopoly.Core.Logs;
using Monopoly.Core.Models.Board;
using Moq.Protected;
using System.Reflection;
using static Monopoly.Core.Jail;

namespace Monopoly.Tests.CoreTests
{
    public class JailTests
    {
        [Fact]
        public void TryGetJailInfo_ShouldReturnCorrectJailInfo()
        {
            // Arrange
            Player player = new Player("player", 0);
            var gameMock = new Mock<IGame>();

            Jail jail = new Jail(gameMock.Object, 0);
            jail.playersInJail[player] = new JailStatus();

            // Act
            var GetJailInfoMethod = jail.GetJailInfo(player);
            jail.playersInJail.TryGetValue(player, out JailStatus JailInfo);

            // Assert
            Assert.NotNull(GetJailInfoMethod);
            Assert.NotNull(JailInfo);
            Assert.Equal(JailInfo, GetJailInfoMethod);
            Assert.Equal(0, GetJailInfoMethod.TurnsInJail);
        }

        [Fact]
        public void CheckIfPlayerGoToJail_ShouldSetPlayerInJail()
        {
            // Arrange
            Player player = new Player("player", 0);
            player.Position = 0;
            var gameMock = new Mock<IGame>();
            var logsMock = new Mock<ILogHandler>();

            gameMock.Setup(g => g.Logs).Returns(logsMock.Object);

            int jailPosition = 9;
            Jail jail = new Jail(gameMock.Object, jailPosition);

            // Act
            jail.PlayerGoToJail(player);
            var isPlayerInJail = jail.IsPlayerInJail(player);

            // Assert
            Assert.True(isPlayerInJail);
            Assert.Equal(jailPosition, player.Position);
        }

        [Fact]
        public void CheckIfPlayerGoToJail_ShouldThrowArgumentNullException()
        {
            // Arrange
            Player player = null;
            var gameMock = new Mock<IGame>();
            var logsMock = new Mock<ILogHandler>();
            gameMock.Setup(g => g.Logs).Returns(logsMock.Object);
            Jail jail = new Jail(gameMock.Object, 0);

            var exeptionType = typeof(ArgumentNullException);
            var expectedMessage = $"Player cannot be null. (Parameter '{nameof(player)}')";

            // Act
            var ex = Record.Exception(() => jail.PlayerGoToJail(player));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType(exeptionType, ex);
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void CheckIfPlayerGoToJail_ShouldSetCorrectLog()
        {
            // Arrange
            Player player = new Player("player", 0);
            var gameMock = new Mock<IGame>();
            var logsMock = new Mock<ILogHandler>();

            gameMock.Setup(g => g.Logs).Returns(logsMock.Object);

            var testReason = "testReason";
            Jail jail = new Jail(gameMock.Object, 0);

            // Act
            jail.PlayerGoToJail(player, testReason);

            // Assert
            logsMock.Verify(l => l.CreateLog($"{player.Name} has been sent to jail {testReason}."), Times.Once);
        }

        [Fact]
        public void IsPlayerInJail_ShouldThrowArgumentNullException()
        {
            // Arrange
            Player player = null;
            var gameMock = new Mock<IGame>();
            Jail jail = new Jail(gameMock.Object, 0);

            var exeptionType = typeof(ArgumentNullException);
            var expectedMessage = $"Player cannot be null. (Parameter '{nameof(player)}')";

            // Act
            var ex = Record.Exception(() => jail.IsPlayerInJail(player));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType(exeptionType, ex);
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void IsPlayerInJail_ShouldThrowInvalidOperationException()
        {
            // Arrange
            Player player = new Player("player", 0);
            var gameMock = new Mock<IGame>();
            Jail jail = new Jail(gameMock.Object, 0);

            var exeptionType = typeof(InvalidOperationException);
            var expectedMessage = $"{player.Name} is not in jail!";

            // Act
            var ex = Record.Exception(() => jail.IsPlayerInJail(player));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType(exeptionType, ex);
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void IsPlayerInJail_ShouldReturnTrue()
        {
            // Arrange
            Player player = new Player("player", 0);
            var gameMock = new Mock<IGame>();
            Jail jail = new Jail(gameMock.Object, 0);
            var logsMock = new Mock<ILogHandler>();
            gameMock.Setup(g => g.Logs).Returns(logsMock.Object);

            jail.PlayerGoToJail(player);

            // Act
            var result = jail.IsPlayerInJail(player);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TryPlayerBuyOut_ShouldReturnTrueWithGetOutOfJailCard()
        {
            // Arrange
            Player player = new Player("player", 0);
            var gameMock = new Mock<IGame>();
            var logsMock = new Mock<ILogHandler>();

            // Set up necessary mocks
            gameMock.Setup(g => g.Logs).Returns(logsMock.Object);

            Jail jail = new Jail(gameMock.Object, 0);
            jail.PlayerGoToJail(player);

            player.NumberOfGetOutOFJailCards = 1;

            bool eventResult = true;
            GameEvents.AskPlayerToBuyOutOfJail += (sender, args) => eventResult;

            // Act
            var result = jail.TryPlayerBuyOut(player);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TryPlayerBuyOut_ShouldReturnTrueWithMoney()
        {
            // Arrange
            Player player = new Player("player", 0);
            var gameMock = new Mock<IGame>();
            var logsMock = new Mock<ILogHandler>();
            var handler = new GameHandler(gameMock.Object);
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);

            // Set up necessary mocks
            gameMock.Setup(g => g.Logs).Returns(logsMock.Object);
            gameMock.Setup(g => g.Handler).Returns(handler);
            gameMock.Setup(g => g.Rules).Returns(rules);
            gameMock.Setup(g => g.Board).Returns(board);

            Jail jail = new Jail(gameMock.Object, 0);
            jail.PlayerGoToJail(player);

            player.NumberOfGetOutOFJailCards = 0;
            player.Money = rules.JailFine;

            bool eventResult = true;
            GameEvents.AskPlayerToBuyOutOfJail += (sender, args) => eventResult;

            // Act
            var result = jail.TryPlayerBuyOut(player);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TryPlayerBuyOut_ShouldReturnTrueWithAssets()
        {
            // Arrange
            Player player = new Player("player", 0);
            var gameMock = new Mock<IGame>();
            var logsMock = new Mock<ILogHandler>();
            var handler = new GameHandler(gameMock.Object);
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            board.Squares.ForEach(s => { s.Owner = player; });

            // Set up necessary mocks
            gameMock.Setup(g => g.Logs).Returns(logsMock.Object);
            gameMock.Setup(g => g.Handler).Returns(handler);
            gameMock.Setup(g => g.Rules).Returns(rules);
            gameMock.Setup(g => g.Board).Returns(board);

            Jail jail = new Jail(gameMock.Object, 0);
            jail.PlayerGoToJail(player);

            player.NumberOfGetOutOFJailCards = 0;
            player.Money = 0;

            bool eventResult = true;
            GameEvents.AskPlayerToBuyOutOfJail += (sender, args) => eventResult;

            // Act
            var result = jail.TryPlayerBuyOut(player);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TryPlayerBuyOut_ShouldReturnFalse()
        {
            // Arrange
            Player player = new Player("player", 0);
            var gameMock = new Mock<IGame>();
            var logsMock = new Mock<ILogHandler>();
            var handler = new GameHandler(gameMock.Object);
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);

            // Set up necessary mocks
            gameMock.Setup(g => g.Logs).Returns(logsMock.Object);
            gameMock.Setup(g => g.Handler).Returns(handler);
            gameMock.Setup(g => g.Rules).Returns(rules);
            gameMock.Setup(g => g.Board).Returns(board);

            Jail jail = new Jail(gameMock.Object, 0);
            jail.PlayerGoToJail(player);

            player.NumberOfGetOutOFJailCards = 0;
            player.Money = 0;

            bool eventResult = true;
            GameEvents.AskPlayerToBuyOutOfJail += (sender, args) => eventResult;

            // Act
            var result = jail.TryPlayerBuyOut(player);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryIncrementTurnsInJail_ShouldIncrementTurnsInJailForPlayer()
        {
            // Arrange
            Player player = new Player("player", 0);
            var gameMock = new Mock<IGame>();
            int startingIncrement = 1;
            int expectedIncrement = 2;

            Jail jail = new Jail(gameMock.Object, 0);
            jail.playersInJail[player] = new JailStatus();
            var playerJail = jail.GetJailInfo(player);
            playerJail.TurnsInJail = startingIncrement;

            // Act
            jail.IncrementTurnsInJail(player);
            int actualIncrement = playerJail.TurnsInJail;

            // Assert
            Assert.Equal(expectedIncrement, actualIncrement);
        }

        [Fact]
        public void PlayerReachedMaxTurnsInJail_WhenTurnsEqualMax_ReturnsTrue()
        {
            // Arrange
            Player player = new Player("player", 0);
            var rules = new GameRules(2, 2, 6);
            rules.MaxTurnsInJail = 3;

            var gameMock = new Mock<IGame>();
            gameMock.Setup(g => g.Rules).Returns(rules);

            Jail jail = new Jail(gameMock.Object, 0);

            jail.playersInJail[player] = new JailStatus();
            jail.GetJailInfo(player).TurnsInJail = 3;

            // Act
            var result = jail.PlayerReachedMaxTurnsInJail(player);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PlayerReachedMaxTurnsInJail_WhenTurnsLessThanMax_ReturnsFalse()
        {
            // Arrange
            Player player = new Player("player", 0);
            var rules = new GameRules(2, 2, 6);
            rules.MaxTurnsInJail = 3;

            var gameMock = new Mock<IGame>();
            gameMock.Setup(g => g.Rules).Returns(rules);

            Jail jail = new Jail(gameMock.Object, 0);

            jail.playersInJail[player] = new JailStatus();
            jail.GetJailInfo(player).TurnsInJail = 2;

            // Act
            var result = jail.PlayerReachedMaxTurnsInJail(player);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PlayerReachedMaxTurnsInJail_WhenTurnsGreaterThanMax_ReturnsTrue()
        {
            // Arrange
            Player player = new Player("player", 0);
            var rules = new GameRules(2, 2, 6);
            rules.MaxTurnsInJail = 3;

            var gameMock = new Mock<IGame>();
            gameMock.Setup(g => g.Rules).Returns(rules);

            Jail jail = new Jail(gameMock.Object, 0);

            jail.playersInJail[player] = new JailStatus();
            jail.GetJailInfo(player).TurnsInJail = 4;

            // Act
            var result = jail.PlayerReachedMaxTurnsInJail(player);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HandleMaxTurnsInJail_PlayerHasNoMoney_PlayerShouldBecomeBankrupt()
        {
            // Arrange
            Player player = new Player("player", 0);
            player.Money = 0;
            var gameMock = new Mock<IGame>();
            var handler = new GameHandler(gameMock.Object);
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            rules.MaxTurnsInJail = 3;
            rules.JailFine = 50;

            gameMock.Setup(g => g.Handler).Returns(handler);
            gameMock.Setup(g => g.Rules).Returns(rules);
            gameMock.Setup(g => g.Board).Returns(board);

            Jail jail = new Jail(gameMock.Object, 0);
            jail.playersInJail[player] = new JailStatus();
            jail.GetJailInfo(player).TurnsInJail = 3;

            // Act
            jail.HandleMaxTurnsInJail(player);

            // Assert
            Assert.True(player.IsBankrupt);
        }

        [Fact]
        public void HandleMaxTurnsInJail_PlayerHasGetOutOfJailCard_PlayerShouldUseGetOutOfJailCard()
        {
            // Arrange
            Player player = new Player("player", 0);
            player.Money = 200;
            player.NumberOfGetOutOFJailCards = 2;
            var gameMock = new Mock<IGame>();
            var logsMock = new Mock<ILogHandler>();
            var handler = new GameHandler(gameMock.Object);
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            rules.MaxTurnsInJail = 3;
            rules.JailFine = 50;

            gameMock.Setup(g => g.Logs).Returns(logsMock.Object);
            gameMock.Setup(g => g.Handler).Returns(handler);
            gameMock.Setup(g => g.Rules).Returns(rules);
            gameMock.Setup(g => g.Board).Returns(board);

            Jail jail = new Jail(gameMock.Object, 0);
            jail.playersInJail[player] = new JailStatus();
            jail.GetJailInfo(player).TurnsInJail = 3;

            // Act
            jail.HandleMaxTurnsInJail(player);

            // Assert
            Assert.Equal(200, player.Money);
            Assert.Equal(1, player.NumberOfGetOutOFJailCards);
        }

        [Fact]
        public void HandleMaxTurnsInJail_PlayerHasMoney_PlayerMoneyShouldBeDeducted()
        {
            // Arrange
            Player player = new Player("player", 0);
            player.Money = 200;
            player.NumberOfGetOutOFJailCards = 0;
            var gameMock = new Mock<IGame>();
            var logsMock = new Mock<ILogHandler>();
            var handler = new GameHandler(gameMock.Object);
            var transactions = new Transaction(gameMock.Object);
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            rules.MaxTurnsInJail = 3;
            rules.JailFine = 50;

            gameMock.Setup(g => g.Logs).Returns(logsMock.Object);
            gameMock.Setup(g => g.Handler).Returns(handler);
            gameMock.Setup(g => g.Transactions).Returns(transactions);
            gameMock.Setup(g => g.Rules).Returns(rules);
            gameMock.Setup(g => g.Board).Returns(board);

            Jail jail = new Jail(gameMock.Object, 0);
            jail.playersInJail[player] = new JailStatus();
            jail.GetJailInfo(player).TurnsInJail = 3;

            // Act
            jail.HandleMaxTurnsInJail(player);

            // Assert
            Assert.Equal(150, player.Money);
        }
    }
}
