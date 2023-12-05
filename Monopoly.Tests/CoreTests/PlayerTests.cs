using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Tests.CoreTests
{
    public class PlayerTests
    {
        [Fact]
        public void CanCrateOneNewPlayer()
        {
            //Arrange
            string playerName = "TestPlayer";
            var playerId = 1;
            var expectedPlayerName = playerName;
            var expectedPlayerId = 1;
            var expectedMoney = 3000;
            var expectedPosition = 0;
            var expectedNumberOfPlayers = 1;

            //Act
            var player = new Player(playerName, playerId);
            var players = new List<Player> { player };

            //Assert
            Assert.Equal(expectedPlayerName, player.Name);
            Assert.Equal(expectedMoney, player.Money);
            Assert.Equal(expectedPosition, player.Position);
            Assert.Equal(expectedNumberOfPlayers, players.Count);
            Assert.Equal(expectedPlayerId, playerId);
        }

        [Theory]
        [InlineData("Player 1")]
        [InlineData("Player 2")]
        [InlineData("Player 3")]
        [InlineData("Player 4")]
        public void CanCrateFourNewPlayers(string playerName)
        {
            //Arrange
            var expectedNumberOfPlayers = 1;
            var expectedPlayerNames = playerName;

            //Act
            var player = new Player(playerName, 0);
            var players = new List<Player> { player };

            //Assert
            Assert.Equal(expectedNumberOfPlayers, players.Count);
            Assert.Equal(expectedPlayerNames, player.Name);
        }

        [Fact]
        public void CanUpdatePlayerPosition()
        {
            //Arrange
            var player = new Player("Mohammad", 0);
            var expectedPosition = 5;

            //Act
            player.Position = expectedPosition;

            //Assert
            Assert.Equal(expectedPosition, player.Position);
        }



        [Fact]
        public void BuyShouldDeductMoneyAndSetOwner()
        {
            // Arrange
            Street street = new(ConsoleColor.Magenta, "Old Kent Road", 2, 4, 10, 30, 90, 160, 250, 50, 50, 60, 30);
            Player player = new("TestPlayer", 1);

            // Act
            player.Buy(street);

            // Assert
            Assert.Equal(3000 - street.Price, player.Money);
            Assert.Equal(player, street.Owner);
        }

        [Fact]
        public void PayRentShouldDeductMoneyFromPlayerAndAddToOwner()
        {
            // Arrange
            Street street = new(ConsoleColor.Magenta, "Old Kent Road", 2, 4, 10, 30, 90, 160, 250, 50, 50, 60, 30);
            Player player1 = new("Player1", 1);
            Player player2 = new("Player2", 2);
            street.Owner = player2;

            // Act
            player1.PayRent(street);

            // Assert
            Assert.Equal(3000 - street.Price, player1.Money);
            Assert.Equal(3000 + street.Price, player2.Money);
        }


        [Fact]
        public void SellShouldRefundMoneyWhenPlayerOwnsStreet()
        {
            // Arrange
            Player player = new("Player1", 1);
            Street street = new(ConsoleColor.Magenta, "Old Kent Road", 2, 4, 10, 30, 90, 160, 250, 50, 50, 60, 30);
            player.Buy(street);

            // Act
            player.Sell(street);

            // Assert
            Assert.Null(street.Owner);
            Assert.Equal(3000, player.Money); 
        }

        [Fact]
        public void SellShouldNotRefundMoneyWhenPlayerDoesNotOwnStreet()
        {
            // Arrange
            Player player = new("Player2", 2);
            Street street = new(ConsoleColor.Magenta, "Old Kent Road", 2, 4, 10, 30, 90, 160, 250, 50, 50, 60, 30);

            // Act
            player.Sell(street);

            // Assert
            Assert.Null(street.Owner); 
            Assert.Equal(3000, player.Money); 
        }

    }
}
