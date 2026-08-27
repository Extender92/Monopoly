using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Tests.CoreTests
{
    public class GameBoardTests
    {
        [Fact]
        public void GetSquareAtPosition_ShouldReturnCorrectSquare()
        {
            // Arrange
            var gameMock = new Mock<IGame>();
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);

            // Act
            var square = board.GetSquareAtPosition(5);

            // Assert
            Assert.IsType<RailroadSquare>(square);
            Assert.Equal("Kings Cross Station", square.Name);
        }

        [Fact]
        public void GetAllSquaresOfType_ShouldReturnAllPropertySquares()
        {
            // Arrange
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);

            // Act
            var propertySquares = board.GetAllSquaresOfType<PropertySquare>();

            // Assert
            Assert.Equal(22, propertySquares.Count);
        }

        [Fact]
        public void GetAllMortgageableSquares_ShouldReturnCorrectSquares()
        {
            // Arrange
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);

            // Act
            var mortgageableSquares = board.GetAllMortgageableSquares();

            // Assert
            Assert.Equal(28, mortgageableSquares.Count);
        }

        [Fact]
        public void GetAllMortgageableSquaresForPlayer_ShouldReturnPlayerOwnedSquares()
        {
            // Arrange
            Game game = new GameTestBuilder().WithSquare(1, ownerId: 0).Build();
            GameBoard board = game.Board;
            Player player = game.Players[0];

            // Act
            var playerMortgageableSquares = board.GetAllMortgageableSquaresForPlayer(player);

            // Assert
            Assert.Single(playerMortgageableSquares);
        }

        [Fact]
        public void GetPlayerMortgageableSquares_ShouldReturnSquaresWithNoHouses()
        {
            // Arrange
            Game game = new GameTestBuilder().WithSquare(1, ownerId: 0).Build();
            GameBoard board = game.Board;
            Player player = game.Players[0];

            // Act
            var playerMortgageableSquares = board.GetPlayerMortgageableSquares(player);

            // Assert
            Assert.Single(playerMortgageableSquares);
        }

        [Fact]
        public void GetPlayerMortgagedSquares_ShouldReturnMortgagedSquares()
        {
            // Arrange
            Game game = new GameTestBuilder().WithSquare(1, ownerId: 0, isMortgage: true).Build();
            GameBoard board = game.Board;
            Player player = game.Players[0];

            // Act
            var mortgagedSquares = board.GetPlayerMortgagedSquares(player);

            // Assert
            Assert.Single(mortgagedSquares);
        }

        [Fact]
        public void GetPlayerUnmortgagedSquares_ShouldReturnUnmortgagedSquares()
        {
            // Arrange
            Game game = new GameTestBuilder().WithSquare(1, ownerId: 0).Build();
            GameBoard board = game.Board;
            Player player = game.Players[0];

            // Act
            var unmortgagedSquares = board.GetPlayerUnmortgagedSquares(player);

            // Assert
            Assert.Single(unmortgagedSquares);
        }

        [Fact]
        public void GetAllPlayerOwnedPropertySquares_ShouldReturnCorrectOwnedProperties()
        {
            // Arrange
            Game game = new GameTestBuilder()
                .WithSquare(1, ownerId: 0)
                .WithSquare(3, ownerId: 0)
                .Build();
            GameBoard board = game.Board;
            Player player = game.Players[0];

            // Act
            var ownedProperties = board.GetAllPlayerOwnedPropertySquares(player);

            // Assert
            Assert.Equal(2, ownedProperties.Count);
        }

        [Fact]
        public void GetAllPropertySquaresPlayerCanBuyHousesIn_ShouldReturnCorrectProperties()
        {
            // Arrange
            Game game = new GameTestBuilder()
                .WithSquare(1, ownerId: 0)
                .WithSquare(3, ownerId: 0)
                .Build();
            GameBoard board = game.Board;
            Player player = game.Players[0];
            IReadOnlyList<PropertySquare> properties = board.GetAllPropertySquares().Take(2).ToList();

            // Act
            var propertiesCanBuyHousesIn = board.GetAllPropertySquaresPlayerCanBuyHousesIn(player);

            // Assert
            Assert.Equal(properties.Count, propertiesCanBuyHousesIn.Count);
        }

        [Fact]
        public void GetAllPropertySquaresPlayerCanBuyHousesIn_ShouldReturnPropertiesInFullColorGroup()
        {
            // Arrange
            Game game = new GameTestBuilder()
                .WithSquare(1, ownerId: 0)
                .WithSquare(3, ownerId: 0)
                .Build();
            GameBoard board = game.Board;
            Player player = game.Players[0];
            List<PropertySquare> colorGroup = board.GetAllPropertySquares().GroupBy(p => p.Color).First().ToList();

            // Act
            var propertiesCanBuyHousesIn = board.GetAllPropertySquaresPlayerCanBuyHousesIn(player);

            // Assert
            Assert.Equal(colorGroup.Count, propertiesCanBuyHousesIn.Count);
            foreach (var property in propertiesCanBuyHousesIn)
            {
                Assert.Contains(property, colorGroup);
            }
        }

        [Fact]
        public void GetAllPropertySquaresPlayerCanBuyHousesIn_ShouldNotReturnPropertiesNotInFullColorGroup()
        {
            // Arrange
            Game game = new GameTestBuilder()
                .WithSquare(1, ownerId: 0)
                .WithSquare(6, ownerId: 0)
                .Build();
            GameBoard board = game.Board;
            Player player = game.Players[0];

            // Act
            var propertiesCanBuyHousesIn = board.GetAllPropertySquaresPlayerCanBuyHousesIn(player);

            // Assert
            Assert.Empty(propertiesCanBuyHousesIn);
        }

        [Fact]
        public void GetAllPropertySquaresPlayerCanBuyHousesIn_ShouldReturnPropertiesInMultipleFullColorGroups()
        {
            // Arrange
            Game game = new GameTestBuilder()
                .WithSquare(1, ownerId: 0)
                .WithSquare(3, ownerId: 0)
                .WithSquare(6, ownerId: 0)
                .WithSquare(8, ownerId: 0)
                .WithSquare(9, ownerId: 0)
                .Build();
            GameBoard board = game.Board;
            Player player = game.Players[0];
            IReadOnlyList<PropertySquare> allProperties = board.GetAllPropertySquares();
            List<PropertySquare> firstColorGroup = allProperties.GroupBy(p => p.Color).First().ToList();
            List<PropertySquare> secondColorGroup = allProperties.GroupBy(p => p.Color).Skip(1).First().ToList();

            // Act
            var propertiesCanBuyHousesIn = board.GetAllPropertySquaresPlayerCanBuyHousesIn(player);

            // Assert
            Assert.Equal(firstColorGroup.Count + secondColorGroup.Count, propertiesCanBuyHousesIn.Count);
            foreach (var property in firstColorGroup)
            {
                Assert.Contains(property, propertiesCanBuyHousesIn);
            }
            foreach (var property in secondColorGroup)
            {
                Assert.Contains(property, propertiesCanBuyHousesIn);
            }
        }

        [Fact]
        public void GetAllPropertySquaresPlayerCanBuyHousesIn_ShouldNotReturnPropertiesOwnedByOthers()
        {
            // Arrange
            Game game = new GameTestBuilder()
                .WithSquare(1, ownerId: 0)
                .WithSquare(3, ownerId: 1)
                .Build();
            GameBoard board = game.Board;
            Player player = game.Players[0];

            // Act
            var propertiesCanBuyHousesIn = board.GetAllPropertySquaresPlayerCanBuyHousesIn(player);

            // Assert
            Assert.Empty(propertiesCanBuyHousesIn);
        }

        [Fact]
        public void GetAllPropertySquaresPlayerCanSellHousesIn_ShouldReturnCorrectProperties()
        {
            // Arrange
            Game game = new GameTestBuilder().WithSquare(1, ownerId: 0, houses: 1).Build();
            GameBoard board = game.Board;
            Player player = game.Players[0];

            // Act
            var propertiesCanSellHousesIn = board.GetAllPropertySquaresPlayerCanSellHousesIn(player);

            // Assert
            Assert.Single(propertiesCanSellHousesIn);
        }

        [Fact]
        public void GetPlayerMortgageableSquares_ShouldNotReturnPropertiesWithHouses()
        {
            // Arrange
            Game game = new GameTestBuilder()
                .WithSquare(1, ownerId: 0, houses: 1)
                .WithSquare(3, ownerId: 0)
                .WithSquare(11, ownerId: 0)
                .Build();
            GameBoard board = game.Board;
            Player player = game.Players[0];
            PropertySquare propertyOne = (PropertySquare)board.GetSquareAtPosition(1);
            PropertySquare propertyTwo = (PropertySquare)board.GetSquareAtPosition(3);
            PropertySquare propertyThree = (PropertySquare)board.GetSquareAtPosition(11);

            // Act
            var mortgageableSquares = board.GetPlayerMortgageableSquares(player);

            // Assert
            Assert.DoesNotContain(mortgageableSquares, s => s == propertyOne);
            Assert.DoesNotContain(mortgageableSquares, s => s == propertyTwo);
            Assert.Contains(mortgageableSquares, s => s == propertyThree);
        }
    }
}
