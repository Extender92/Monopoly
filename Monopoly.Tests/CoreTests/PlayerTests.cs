using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Moq;
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
        [InlineData("Player 1", 0)]
        [InlineData("Player 2", 1)]
        [InlineData("Player 3", 2)]
        [InlineData("Player 4", 3)]
        public void CanCreateFourNewPlayers(string playerName, int playerId)
        {
            // Arrange
            var expectedNumberOfPlayers = 4;

            // Act
            var players = new List<Player>();
            for (int i = 0; i < expectedNumberOfPlayers; i++)
            {
                players.Add(new Player(playerName, playerId + i));
            }

            // Assert
            Assert.Equal(expectedNumberOfPlayers, players.Count);
            foreach (var player in players)
            {
                Assert.Equal(playerName, player.Name);
            }
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
            Assert.True(player.Position >= 0, "Position should not be negative.");
            Assert.Equal(expectedPosition, player.Position);
        }  

    }
}
