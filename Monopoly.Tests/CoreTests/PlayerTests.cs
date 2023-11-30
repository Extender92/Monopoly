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
            var PlayerId = 1;
            var player = new Player(playerName, PlayerId);
            var players = new List<Player> {player};

            //Act
            var ExpectedPlayerId = 1;
            var ExpectedPlayerName = playerName;
            var ExpectedMoney = 3000;
            var ExpectedPosition = 0;
            var ExpectedNumberOfPlayers = 1;

            //Assert
            Assert.Equal(ExpectedPlayerName, player.Name);
            Assert.Equal(ExpectedMoney, player.Money);
            Assert.Equal(ExpectedPosition, player.Position);
            Assert.Equal(ExpectedNumberOfPlayers, players.Count);
            Assert.Equal(ExpectedPlayerId, PlayerId);
        }

        [Theory]
        [InlineData("Player 1")]
        [InlineData("Player 2")]
        [InlineData("Player 3")]
        [InlineData("Player 4")]
        public void CanCrateFourNewPlayers(string playerName)
        {
            //Arrange
            var player = new Player(playerName, 0);
            var players = new List<Player> { player };

            //Act
            var expectedNumberOfPlayers = 1;
            var expectedPlayerNames = playerName;

            //Assert
            Assert.Equal(expectedNumberOfPlayers, players.Count);
            Assert.Equal(expectedPlayerNames, player.Name);
        }

        [Fact]
        public void CanUpdatePlayerPosition()
        {
            //Arrange
            var player = new Player("Mohammad", 0);

            //Act
             var expectedPosition = player.Position = 5;

            //Assert
            Assert.Equal(expectedPosition, player.Position);
        }
    }
}
