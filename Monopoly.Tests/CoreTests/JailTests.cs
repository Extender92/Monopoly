using Monopoly.Core.Events;
using Monopoly.Core.Interface;
using Monopoly.Core.Logs;
using Monopoly.Core.Models.Board;
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
            logsMock.VerifyNoOtherCalls();
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
        public void IsPlayerInJail_ShouldReturnFalse()
        {
            // Arrange
            Player player = new Player("player", 0);
            var gameMock = new Mock<IGame>();
            Jail jail = new Jail(gameMock.Object, 0);
            var logsMock = new Mock<ILogHandler>();
            gameMock.Setup(g => g.Logs).Returns(logsMock.Object);

            // Act
            var result = jail.IsPlayerInJail(player);

            // Assert
            Assert.False(result);
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
            Assert.True(player.IsBankrupt);
            logsMock.Verify(l => l.CreateLog($"{player.Name} has been bankrupt, {player.Name} Could not afford to pay Jail Fine of 50£."), Times.Once);
            logsMock.VerifyNoOtherCalls();
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
            logsMock.Verify(l => l.CreateLog($"JailTurn 3: {player.Name} has been released from jail, {player.Name} used a Get Out of Jail For Free card and have 1 left."), Times.Once);
            logsMock.VerifyNoOtherCalls();
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
            logsMock.Verify(l => l.CreateLog($"JailTurn 3: {player.Name} has been released from jail, {player.Name} paid the fine to get out of jail."), Times.Once);
            logsMock.Verify(l => l.CreateLog($"{player.Name} payed fines of 50£."), Times.Once);
            logsMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void BuyOutPlayerFromJail_UseGetOutOfJailFreeCard()
        {
            // Arrange
            Player player = new Player("Player", 0);
            player.Money = 200;
            player.NumberOfGetOutOFJailCards = 1;
            var gameMock = new Mock<IGame>();

            var jail = new Jail(gameMock.Object, 0);
            jail.playersInJail[player] = new JailStatus();

            // Act
            string reason = jail.BuyOutPlayerFromJail(player);

            // Assert
            Assert.Equal(", Player used a Get Out of Jail For Free card and have 0 left", reason);
            Assert.Equal(0, player.NumberOfGetOutOFJailCards);
        }

        [Fact]
        public void BuyOutPlayerFromJail_PayFine()
        {
            // Arrange
            Player player = new Player("Player", 0);
            player.Money = 200;
            player.NumberOfGetOutOFJailCards = 0;
            var gameMock = new Mock<IGame>();
            var logsMock = new Mock<ILogHandler>();
            var rules = new GameRules(2, 2, 6);
            var transactions = new Transaction(gameMock.Object);

            gameMock.Setup(g => g.Logs).Returns(logsMock.Object);
            gameMock.Setup(g => g.Rules).Returns(rules);
            gameMock.Setup(g => g.Transactions).Returns(transactions);

            var jail = new Jail(gameMock.Object, 0);
            jail.playersInJail[player] = new JailStatus();

            // Act
            string reason = jail.BuyOutPlayerFromJail(player);

            // Assert
            Assert.Equal(", Player paid the fine to get out of jail", reason);
            Assert.Equal(150, player.Money);
            logsMock.Verify(l => l.CreateLog($"{player.Name} payed fines of 50£."), Times.Once);
            logsMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void BuyOutPlayerFromJail_InsufficientFunds_ShouldRaiseEventAndPayFine()
        {
            // Arrange
            Player player = new Player("Player", 0);
            player.Money = 0;
            player.NumberOfGetOutOFJailCards = 0;
            var gameMock = new Mock<IGame>();
            var logsMock = new Mock<ILogHandler>();
            var rules = new GameRules(2, 2, 6);
            var transactions = new Transaction(gameMock.Object);

            gameMock.Setup(g => g.Logs).Returns(logsMock.Object);
            gameMock.Setup(g => g.Rules).Returns(rules);
            gameMock.Setup(g => g.Transactions).Returns(transactions);

            var jail = new Jail(gameMock.Object, 0);
            jail.playersInJail[player] = new JailStatus();


            var eventRaised = false;
            GameEvents.PlayerInsufficientFunds += (sender, args) =>
            {
                eventRaised = true;
                player.Money += rules.JailFine;
            };

            // Act
            string reason = jail.BuyOutPlayerFromJail(player);

            // Assert
            Assert.True(eventRaised);
            Assert.Equal(0, player.Money);
            logsMock.Verify(l => l.CreateLog($"{player.Name} payed fines of 50£."), Times.Once);
            logsMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void ReleasePlayerFromJail_PlayerInJail_NoReason()
        {
            // Arrange
            Player player = new Player("Player", 0);
            var gameMock = new Mock<IGame>();
            var logsMock = new Mock<ILogHandler>();

            gameMock.Setup(g => g.Logs).Returns(logsMock.Object);

            var jail = new Jail(gameMock.Object, 0);
            jail.playersInJail[player] = new JailStatus();

            // Act
            jail.ReleasePlayerFromJail(player);

            // Assert
            logsMock.Verify(l => l.CreateLog("JailTurn 0: Player has been released from jail."), Times.Once);
            logsMock.VerifyNoOtherCalls();
            Assert.DoesNotContain(player, jail.playersInJail.Keys);
        }

        [Fact]
        public void ReleasePlayerFromJail_PlayerInJail_WithReason()
        {
            // Arrange
            Player player = new Player("Player", 0);
            var gameMock = new Mock<IGame>();
            var logsMock = new Mock<ILogHandler>();

            gameMock.Setup(g => g.Logs).Returns(logsMock.Object);

            var jail = new Jail(gameMock.Object, 0);
            jail.playersInJail[player] = new JailStatus();
            string reason = ". Some reason for release";

            // Act
            jail.ReleasePlayerFromJail(player, reason);

            // Assert
            logsMock.Verify(l => l.CreateLog("JailTurn 0: Player has been released from jail. Some reason for release."), Times.Once);
            logsMock.VerifyNoOtherCalls();
            Assert.DoesNotContain(player, jail.playersInJail.Keys);
        }
    }
}
