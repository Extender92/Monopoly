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
using System.Numerics;

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
            game.Dice = new List<IDie> { dieMock.Object };

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
        public void RollDice_ShouldRollDiceAndLogRollAndTotal()
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

            var game = new Game(new List<Player>(), player, dice, new GameRules(1, 2, 6), logs);

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

            var game = new Game(new List<Player>(), new Player("Player", 0), dice, new GameRules(1, 2, 6), new LogHandler());
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

            var game = new Game(new List<Player>(), new Player("Player", 0), dice, new GameRules(1, 2, 6), new LogHandler());
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

            var game = new Game(new List<Player>(), new Player("Player", 0), dice, new GameRules(1, 2, 6), new LogHandler());
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

            var game = new Game(new List<Player>(), new Player("Player", 0), dice, new GameRules(1, 1, 6), new LogHandler());
            var gameHandler = new GameHandler(game);

            // Act
            var result = gameHandler.IsDiceDouble();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetMoneyFromBankruptPlayerAndBankruptPlayer_ShouldReturnRemainingMoneyAndHandleBankruptcyOnOnlyOnePlayer()
        {
            // Arrange
            var gameRules = new GameRules(2, 2, 6);
            var playerOne = new Player("TestPlayer", 0) { Money = 200 };
            var playerTwo = new Player("TestPlayer", 0) { Money = 200 };
            var squareList = new List<Square>
            {
                new PropertySquare(ConsoleColor.Blue, "Square1", 100, 100, 50, 75, 80, 85, 90, 50, 50, 100, 50 ,1) { Owner = playerOne },
                new RailroadSquare(2, "Square2", 150, 75, 80, 85, 90, 75){ Owner = playerOne },
                new UtilitySquare(3, "Square3", 200, 100, 110, 100){ Owner = playerTwo },
                new PropertySquare(ConsoleColor.Blue, "Square4", 100, 100, 50, 75, 80, 85, 90, 50, 50, 100, 50 ,4){ Owner = playerTwo },
                new RailroadSquare(5, "Square5", 150, 75, 80, 85, 90, 75),
                new UtilitySquare(6, "Square6", 200, 100, 110, 100) { Owner = playerOne }
            };
            var game = new Game(new List<Player>(), new Player("Player", 0), new List<IDie>(), gameRules, new LogHandler());
            game.Board = new GameBoard(gameRules);
            game.Board.Squares = squareList;

            var gameHandler = new GameHandler(game);

            // Act
            int remainingMoney = gameHandler.GetMoneyFromBankruptPlayerAndBankruptPlayer(playerOne);

            // Assert
            Assert.Equal(425, remainingMoney); // Initial money + assets should be money: 200 + mortgage: 225 = 425
            Assert.Equal(0, playerOne.Money); // Player money should be set to 0
            Assert.True(playerOne.IsBankrupt); // Player should be bankrupt
        }

        [Fact]
        public void HandlePlayerBankruptcy_ShouldClearOwnershipAndSetPlayerBankruptOnOnlyOnePlayer()
        {
            // Arrange
            var gameRules = new GameRules(2, 2, 6);
            var playerOne = new Player("TestPlayer", 0);
            var playerTwo = new Player("TestPlayer", 0);
            var squareList = new List<Square>
            {
                new PropertySquare(ConsoleColor.Blue, "Square1", 100, 100, 50, 75, 80, 85, 90, 50, 50, 100, 50 ,1) { Owner = playerOne },
                new RailroadSquare(2, "Square2", 150, 75, 80, 85, 90, 75){ Owner = playerOne },
                new UtilitySquare(3, "Square3", 200, 100, 110, 100){ Owner = playerTwo },
                new PropertySquare(ConsoleColor.Blue, "Square4", 100, 100, 50, 75, 80, 85, 90, 50, 50, 100, 50 ,4){ Owner = playerTwo },
                new RailroadSquare(5, "Square5", 150, 75, 80, 85, 90, 75),
                new UtilitySquare(6, "Square6", 200, 100, 110, 100) { Owner = playerOne }
            };
            var game = new Game(new List<Player>(), new Player("Player", 0), new List<IDie>(), gameRules, new LogHandler());
            game.Board = new GameBoard(gameRules);
            game.Board.Squares = squareList;
            var gameHandler = new GameHandler(game);

            // Act
            gameHandler.HandlePlayerBankruptcy(playerOne);

            // Assert
            Assert.Null(squareList[0].Owner); // Ownership should be cleared
            Assert.Null(squareList[1].Owner);
            Assert.Null(squareList[5].Owner);

            Assert.True(playerOne.IsBankrupt); // Player should be bankrupt

            Assert.True(squareList[2].Owner == playerTwo); // Should not clear ownership of other players
            Assert.True(squareList[3].Owner == playerTwo);
        }

        [Fact]
        public void ClearOwnershipForPlayer_ShouldClearOwnershipAndHousesOnOnlyOnePlayer()
        {
            // Arrange
            var gameRules = new GameRules(2, 2, 6);
            var playerOne = new Player("TestPlayer", 0);
            var playerTwo = new Player("TestPlayer", 0);
            var squareList = new List<Square>
            {
                new PropertySquare(ConsoleColor.Blue, "Square1", 100, 100, 50, 75, 80, 85, 90, 50, 50, 100, 50 ,1) { Owner = playerOne, Houses = 3 },
                new RailroadSquare(2, "Square2", 150, 75, 80, 85, 90, 75){ Owner = playerOne },
                new UtilitySquare(3, "Square3", 200, 100, 110, 100){ Owner = playerTwo },
                new PropertySquare(ConsoleColor.Blue, "Square4", 100, 100, 50, 75, 80, 85, 90, 50, 50, 100, 50 ,4){ Owner = playerTwo, Houses = 2 },
                new RailroadSquare(5, "Square5", 150, 75, 80, 85, 90, 75),
                new UtilitySquare(6, "Square6", 200, 100, 110, 100) { Owner = playerOne }
            };
            var game = new Game(new List<Player>(), new Player("Player", 0), new List<IDie>(), gameRules, new LogHandler());
            game.Board = new GameBoard(gameRules);
            game.Board.Squares = squareList;
            var gameHandler = new GameHandler(game);

            // Act
            gameHandler.ClearOwnershipForPlayer(playerOne);

            // Assert
            Assert.Null(squareList[0].Owner); // Ownership should be cleared
            Assert.Null(squareList[1].Owner);
            Assert.Null(squareList[5].Owner);

            Assert.IsType<PropertySquare>(squareList[0]);
            Assert.Equal(0, ((PropertySquare)squareList[0]).Houses);// Houses should be set to 0

            Assert.True(squareList[2].Owner == playerTwo); // Should not clear ownership of other players
            Assert.True(squareList[3].Owner == playerTwo);

            Assert.Equal(2, ((PropertySquare)squareList[3]).Houses);// Houses should be set to 2 and not removed
        }

        [Fact]
        public void IsPlayerBankrupt_ShouldReturnTrueWhenPlayerCannotAfford()
        {
            // Arrange
            var gameRules = new GameRules(2, 2, 6);
            var player = new Player("TestPlayer", 0) { Money = 100 };
            var game = new Game(new List<Player>(), new Player("Player", 0), new List<IDie>(), gameRules, new LogHandler());
            var gameHandler = new GameHandler(game);

            // Act
            bool isBankrupt = gameHandler.IsPlayerBankrupt(player, 150);

            // Assert
            Assert.True(isBankrupt);
        }

        [Fact]
        public void IsPlayerBankrupt_ShouldReturnFalseWhenPlayerCanAfford()
        {
            // Arrange
            var gameRules = new GameRules(2, 2, 6);
            var player = new Player("TestPlayer", 0) { Money = 200 };
            var game = new Game(new List<Player>(), new Player("Player", 0), new List<IDie>(), gameRules, new LogHandler());
            var gameHandler = new GameHandler(game);

            // Act
            bool isBankrupt = gameHandler.IsPlayerBankrupt(player, 150);

            // Assert
            Assert.False(isBankrupt);
        }

        [Fact]
        public void CanAffordWithAssets_ShouldReturnTrueWhenPlayerCanAfford()
        {
            // Arrange
            var gameRules = new GameRules(2, 2, 6);
            var player = new Player("TestPlayer", 0) { Money = 200 };
            var game = new Game(new List<Player>(), new Player("Player", 0), new List<IDie>(), gameRules, new LogHandler());
            var gameHandler = new GameHandler(game);

            // Act
            bool canAfford = gameHandler.CanAffordWithAssets(player, 150);

            // Assert
            Assert.True(canAfford);
        }

        [Fact]
        public void CanAffordWithAssets_ShouldReturnFalseWhenPlayerCannotAfford()
        {
            // Arrange
            var gameRules = new GameRules(2, 2, 6);
            var player = new Player("TestPlayer", 0) { Money = 100 };
            var game = new Game(new List<Player>(), new Player("Player", 0), new List<IDie>(), gameRules, new LogHandler());
            var gameHandler = new GameHandler(game);

            // Act
            bool canAfford = gameHandler.CanAffordWithAssets(player, 150);

            // Assert
            Assert.False(canAfford);
        }

        [Fact]
        public void CalculatePlayerAssets_ShouldCalculateCorrectTotalAssets()
        {
            // Arrange
            var gameRules = new GameRules(2, 2, 6);
            var player = new Player("TestPlayer", 0) { Money = 100 };
            var property = new PropertySquare(ConsoleColor.Blue, "Square1", 100, 100, 50, 75, 80, 85, 90, 50, 50, 100, 50, 1) { Owner = player, IsMortgage = false };
            var game = new Game(new List<Player>(), new Player("Player", 0), new List<IDie>(), gameRules, new LogHandler());
            game.Board = new GameBoard(gameRules);
            game.Board.Squares = new List<Square> { property };
            var gameHandler = new GameHandler(game);

            // Act
            int totalAssets = gameHandler.CalculatePlayerAssets(player);

            // Assert
            Assert.Equal(150, totalAssets); // Money: 100 + Mortgage Value 50 = 150
        }

        [Fact]
        public void CalculateMortgageValue_ShouldReturnCorrectMortgageValue()
        {
            // Arrange
            var gameRules = new GameRules(2, 2, 6);
            var property = new PropertySquare(ConsoleColor.Blue, "Square1", 100, 100, 50, 75, 80, 85, 90, 50, 50, 100, 50, 1);
            var game = new Game(new List<Player>(), new Player("Player", 0), new List<IDie>(), gameRules, new LogHandler());
            game.Board = new GameBoard(gameRules);
            game.Board.Squares = new List<Square> { property };
            var gameHandler = new GameHandler(game);

            // Act
            int mortgageValue = gameHandler.CalculateMortgageValue(property);

            // Assert
            Assert.Equal(50, mortgageValue);
        }

        [Fact]
        public void CalculateHouseAndHotelValue_ShouldReturnCorrectValueForHousesAndHotels()
        {
            // Arrange
            var gameRules = new GameRules(2, 2, 6);
            var property = new PropertySquare(ConsoleColor.Blue, "Square1", 100, 100, 50, 75, 80, 85, 90, 50, 50, 100, 50, 1);
            property.Houses = 3;
            var game = new Game(new List<Player>(), new Player("Player", 0), new List<IDie>(), gameRules, new LogHandler());
            game.Board = new GameBoard(gameRules);
            game.Board.Squares = new List<Square> { property };
            var gameHandler = new GameHandler(game);

            // Act
            int value = gameHandler.CalculateHouseAndHotelValue(property);

            // Assert
            Assert.Equal(75, value); // 3 houses * (50 / 2) = 75
        }
    }
}
