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
    }
}
