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
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            Player player = new Player("player", 0);
            board.GetAllPropertySquares().First().Owner = player;

            // Act
            var playerMortgageableSquares = board.GetAllMortgageableSquaresForPlayer(player);

            // Assert
            Assert.Single(playerMortgageableSquares);
        }

        [Fact]
        public void GetPlayerMortgageableSquares_ShouldReturnSquaresWithNoHouses()
        {
            // Arrange
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            Player player = new Player("player", 0);
            var property = board.GetAllPropertySquares().First();
            property.Houses = 0;
            property.Owner = player;

            // Act
            var playerMortgageableSquares = board.GetPlayerMortgageableSquares(player);

            // Assert
            Assert.Single(playerMortgageableSquares);
        }

        [Fact]
        public void GetPlayerMortgagedSquares_ShouldReturnMortgagedSquares()
        {
            // Arrange
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            Player player = new Player("player", 0);
            var property = board.GetAllPropertySquares().First();
            property.Owner = player;
            property.IsMortgage = true;

            // Act
            var mortgagedSquares = board.GetPlayerMortgagedSquares(player);

            // Assert
            Assert.Single(mortgagedSquares);
        }

        [Fact]
        public void GetPlayerUnmortgagedSquares_ShouldReturnUnmortgagedSquares()
        {
            // Arrange
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            Player player = new Player("player", 0);
            var property = board.GetAllPropertySquares().First();
            property.Owner = player;
            property.IsMortgage = false;

            // Act
            var unmortgagedSquares = board.GetPlayerUnmortgagedSquares(player);

            // Assert
            Assert.Single(unmortgagedSquares);
        }

        [Fact]
        public void GetAllPlayerOwnedPropertySquares_ShouldReturnCorrectOwnedProperties()
        {
            // Arrange
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            Player player = new Player("player", 0);
            var property1 = board.GetAllPropertySquares()[0];
            var property2 = board.GetAllPropertySquares()[1];
            property1.Owner = player;
            property2.Owner = player;

            // Act
            var ownedProperties = board.GetAllPlayerOwnedPropertySquares(player);

            // Assert
            Assert.Equal(2, ownedProperties.Count);
        }

        [Fact]
        public void GetAllPropertySquaresPlayerCanBuyHousesIn_ShouldReturnCorrectProperties()
        {
            // Arrange
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            Player player = new Player("player", 0);
            var properties = board.GetAllPropertySquares().Take(2).ToList();
            foreach (var property in properties)
            {
                property.Owner = player;
            }

            // Act
            var propertiesCanBuyHousesIn = board.GetAllPropertySquaresPlayerCanBuyHousesIn(player);

            // Assert
            Assert.Equal(properties.Count, propertiesCanBuyHousesIn.Count);
        }

        [Fact]
        public void GetAllPropertySquaresPlayerCanBuyHousesIn_ShouldReturnPropertiesInFullColorGroup()
        {
            // Arrange
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            Player player = new Player("player", 0);

            var colorGroup = board.GetAllPropertySquares().GroupBy(p => p.Color).First().ToList();
            foreach (var property in colorGroup)
            {
                property.Owner = player;
            }

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
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            Player player = new Player("player", 0);

            var allProperties = board.GetAllPropertySquares();
            var firstColorGroup = allProperties.GroupBy(p => p.Color).First().ToList();
            var secondColorGroup = allProperties.GroupBy(p => p.Color).Skip(1).First().ToList();

            foreach (var property in firstColorGroup.Take(1))
            {
                property.Owner = player;
            }
            foreach (var property in secondColorGroup.Take(1))
            {
                property.Owner = player;
            }

            // Act
            var propertiesCanBuyHousesIn = board.GetAllPropertySquaresPlayerCanBuyHousesIn(player);

            // Assert
            Assert.Empty(propertiesCanBuyHousesIn);
        }

        [Fact]
        public void GetAllPropertySquaresPlayerCanBuyHousesIn_ShouldReturnPropertiesInMultipleFullColorGroups()
        {
            // Arrange
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            Player player = new Player("player", 0);

            var allProperties = board.GetAllPropertySquares();
            var firstColorGroup = allProperties.GroupBy(p => p.Color).First().ToList();
            var secondColorGroup = allProperties.GroupBy(p => p.Color).Skip(1).First().ToList();

            foreach (var property in firstColorGroup)
            {
                property.Owner = player;
            }
            foreach (var property in secondColorGroup)
            {
                property.Owner = player;
            }

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
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            Player player = new Player("player", 0);
            Player otherPlayer = new Player("otherPlayer", 1);

            var colorGroup = board.GetAllPropertySquares().GroupBy(p => p.Color).First().ToList();
            foreach (var property in colorGroup.Take(1))
            {
                property.Owner = player;
            }
            foreach (var property in colorGroup.Skip(1))
            {
                property.Owner = otherPlayer;
            }

            // Act
            var propertiesCanBuyHousesIn = board.GetAllPropertySquaresPlayerCanBuyHousesIn(player);

            // Assert
            Assert.Empty(propertiesCanBuyHousesIn);
        }

        [Fact]
        public void GetAllPropertySquaresPlayerCanSellHousesIn_ShouldReturnCorrectProperties()
        {
            // Arrange
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            Player player = new Player("player", 0);
            var property = board.GetAllPropertySquares().First();
            property.Owner = player;
            property.Houses = 1;

            // Act
            var propertiesCanSellHousesIn = board.GetAllPropertySquaresPlayerCanSellHousesIn(player);

            // Assert
            Assert.Single(propertiesCanSellHousesIn);
        }

        [Fact]
        public void GetPlayerMortgageableSquares_ShouldNotReturnPropertiesWithHouses()
        {
            // Arrange
            var rules = new GameRules(2, 2, 6);
            var board = new GameBoard(rules);
            Player player = new Player("player", 0);
            var propertyOne = board.GetAllPropertySquares()[0];
            var propertyTwo = board.GetAllPropertySquares()[1];
            var propertyThree = board.GetAllPropertySquares()[5];
            propertyOne.Owner = player;
            propertyOne.Houses = 1;
            propertyTwo.Owner = player;
            propertyTwo.Houses = 0;
            propertyThree.Owner = player;
            propertyThree.Houses = 0;

            // Act
            var mortgageableSquares = board.GetPlayerMortgageableSquares(player);

            // Assert
            Assert.DoesNotContain(mortgageableSquares, s => s == propertyOne);
            Assert.DoesNotContain(mortgageableSquares, s => s == propertyTwo);
            Assert.Contains(mortgageableSquares, s => s == propertyThree);
        }
    }
}