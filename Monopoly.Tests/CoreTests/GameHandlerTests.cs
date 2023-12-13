using Monopoly.Core.Models;
using Monopoly.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monopoly.Core.Logs;
using Monopoly.Core.Models.Board;

namespace Monopoly.Tests.CoreTests
{
    public class GameHandlerTests
    {
        [Fact]
        public void RoleDiceAndMovePlayer_ShouldAdjustPlayerPosition()
        {
            // Arrange
            var dieMock = new Mock<IDie>();
            dieMock.Setup(d => d.GetDieResult()).Returns(3);

            GameRules gameRules = new GameRules(2, 2, 6);
            Game game = CoreGameSetup.Setup(gameRules);
            var gameHandler = new GameHandler(game);
            game.Dice = new List<IDie> {dieMock.Object};

            Player player = game.Players[0];

            // Act
            gameHandler.RoleDiceAndMovePlayer(player);

            // Assert
            Assert.Equal(3, player.Position);
        }

        [Fact]
        public void CheckIfPlayerGoPastGo_ShouldAdjustPlayerPositionAndGrantSalary()
        {
            // Arrange
            GameRules gameRules = new GameRules(2, 2, 6);
            Game game = CoreGameSetup.Setup(gameRules);
            var gameHandler = new GameHandler(game);

            game.Players[0].Position = 41;
            int startingMoney = 3000;
            game.Players[0].Money = startingMoney;

            int expectedMoney = game.Rules.Salary + startingMoney;

            // Act
            gameHandler.CheckIfPlayerGoPastGo(game.Players[0]);

            // Assert
            Assert.Equal(1, game.Players[0].Position); // Player position should be adjusted to 1 after passing Go
            Assert.True(startingMoney < game.Players[0].Money); // Player should have received salary
            Assert.Equal(expectedMoney, game.Players[0].Money); // Player should have received correct amount of money
        }

        [Fact]
        public void CheckIfPlayerGoPastGoAfterRollingDice_ShouldAdjustPlayerPositionAndGrantSalary()
        {
            // Arrange
            var dieMock = new Mock<IDie>();
            dieMock.Setup(d => d.GetDieResult()).Returns(4);

            GameRules gameRules = new GameRules(2, 2, 6);
            Game game = CoreGameSetup.Setup(gameRules);
            var gameHandler = new GameHandler(game);
            game.Dice = new List<IDie> { dieMock.Object };

            Player player = game.Players[0];
            player.Position = 37;
            int startingMoney = 3000;
            player.Money = startingMoney;

            int expectedMoney = game.Rules.Salary + startingMoney;

            // Act
            gameHandler.RoleDiceAndMovePlayer(player);

            // Assert
            Assert.Equal(1, player.Position); // Player position should be adjusted to 1 after passing Go
            Assert.True(startingMoney < player.Money); // Player should have received salary
            Assert.Equal(expectedMoney, player.Money); // Player should have received correct amount of money
        }

        [Fact]
        public void RoleDiceAndMovePlayer_ShouldAdjustPlayerPositionWithTwoDice()
        {
            // Arrange
            var die1Mock = new Mock<IDie>();
            die1Mock.Setup(d => d.GetDieResult()).Returns(2);

            var die2Mock = new Mock<IDie>();
            die2Mock.Setup(d => d.GetDieResult()).Returns(4);

            GameRules gameRules = new GameRules(2, 2, 6);
            Game game = CoreGameSetup.Setup(gameRules);
            var gameHandler = new GameHandler(game);
            game.Dice = new List<IDie> { die1Mock.Object, die2Mock.Object };

            Player player = game.Players[0];
            player.Position = 10;

            int diceOneExpectedReturn = 2;
            int diceTwoExpectedReturn = 4;

            // Act
            gameHandler.RoleDiceAndMovePlayer(player);

            // Assert
            Assert.Equal(16, player.Position);
            Assert.Equal(2, game.Dice.Count);
            Assert.Equal(diceOneExpectedReturn, game.Dice[0].GetDieResult());
            Assert.Equal(diceTwoExpectedReturn, game.Dice[1].GetDieResult());
        }

        [Fact]
        public void RollDice_ShouldLRollDiceAndLogRollAndTotal()
        {
            // Arrange
            var player = new Player("TestPlayer", 0);

            var die1Mock = new Mock<IDie>();
            die1Mock.Setup(d => d.GetDieResult()).Returns(3);

            var die2Mock = new Mock<IDie>();
            die2Mock.Setup(d => d.GetDieResult()).Returns(4);

            var dice = new List<IDie> { die1Mock.Object, die2Mock.Object };

            var logHandlerMock = new Mock<ILogHandler>();

            var logs = logHandlerMock.Object;

            var game = new Game(new List<Player>(), dice, new GameRules(1, 2, 6), logs);

            var gameHandler = new GameHandler(game);

            // Act
            gameHandler.RollDice(player);

            // Assert
            // Verify that CreateLog was called with the expected dice roll and total
            logHandlerMock.Verify(l => l.CreateLog("TestPlayer rolled: 3 4 Total: 7"), Times.Once);
            Assert.Equal(3, game.Dice[0].GetDieResult());
            Assert.Equal(4, game.Dice[1].GetDieResult());
            Assert.Equal(7, gameHandler.CalculateDiceSum());
        }

        [Fact]
        public void IsDiceDouble_ShouldReturnTrueForDouble()
        {
            // Arrange
            var die1Mock = new Mock<IDie>();
            die1Mock.Setup(d => d.GetDieResult()).Returns(3);

            var die2Mock = new Mock<IDie>();
            die2Mock.Setup(d => d.GetDieResult()).Returns(3);

            var dice = new List<IDie> { die1Mock.Object, die2Mock.Object };

            var game = new Game(new List<Player>(), dice, new GameRules(1, 2, 6), new LogHandler());
            var gameHandler = new GameHandler(game);

            // Act
            var result = gameHandler.IsDiceDouble();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsDiceDouble_ShouldReturnTrueForAllDice()
        {
            // Arrange
            var die1Mock = new Mock<IDie>();
            die1Mock.Setup(d => d.GetDieResult()).Returns(3);

            var die2Mock = new Mock<IDie>();
            die2Mock.Setup(d => d.GetDieResult()).Returns(3);

            var die3Mock = new Mock<IDie>();
            die3Mock.Setup(d => d.GetDieResult()).Returns(3);

            var die4Mock = new Mock<IDie>();
            die4Mock.Setup(d => d.GetDieResult()).Returns(3);

            var dice = new List<IDie> { die1Mock.Object, die2Mock.Object, die3Mock.Object, die4Mock.Object };

            var game = new Game(new List<Player>(), dice, new GameRules(1, 2, 6), new LogHandler());
            var gameHandler = new GameHandler(game);

            // Act
            var result = gameHandler.IsDiceDouble();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsDiceDouble_ShouldReturnFalseForNonDouble()
        {
            // Arrange
            var die1Mock = new Mock<IDie>();
            die1Mock.Setup(d => d.GetDieResult()).Returns(3);

            var die2Mock = new Mock<IDie>();
            die2Mock.Setup(d => d.GetDieResult()).Returns(4);

            var dice = new List<IDie> { die1Mock.Object, die2Mock.Object };

            var game = new Game(new List<Player>(), dice, new GameRules(1, 2, 6), new LogHandler());
            var gameHandler = new GameHandler(game);

            // Act
            var result = gameHandler.IsDiceDouble();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsDiceDouble_ShouldReturnFalseForSingleDie()
        {
            // Arrange
            var dieMock = new Mock<IDie>();
            dieMock.Setup(d => d.GetDieResult()).Returns(3);

            var dice = new List<IDie> { dieMock.Object };

            var game = new Game(new List<Player>(), dice, new GameRules(1, 1, 6), new LogHandler());
            var gameHandler = new GameHandler(game);

            // Act
            var result = gameHandler.IsDiceDouble();

            // Assert
            Assert.False(result);
        }

        
    }
}
