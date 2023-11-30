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
            var player = new Player(playerName);
            var playres = new List<Player> {player};

            //Act
            var actualPlayerId = 1;
            var actualPlayerName = playerName;
            var actualMoney = 3000;
            var actualPosition = 0;
            var actualNumberOfPlayers = 1;

            //Assert
            Assert.Equal(actualPlayerName, player.Name);
            Assert.Equal(actualMoney, player.Money);
            Assert.Equal(actualPosition, player.Position);
            Assert.Equal(actualNumberOfPlayers, playres.Count);
            Assert.Equal(1, actualPlayerId);
            Assert.NotEqual(0, actualPlayerId);


        }
        [Theory]
        [InlineData("Player 1")]
        [InlineData("Player 2")]
        [InlineData("Player 3")]
        [InlineData("Player 4")]
        public void CanCrateFourNewPlayers(string playerName)
        {
            //Arrange
            var player = new Player(playerName);
            var players = new List<Player> { player };

            //Act
            var actualNumberOfPlayers = 1;
            var actualPlayerNames = new List<string> { playerName };

            //Assert
            Assert.Equal(actualNumberOfPlayers, players.Count);
            Assert.Equal(actualPlayerNames[0], player.Name);
        }

        [Fact]
        public void CanUpdatePlayerPosition()
        {
            //Arrange
            var player = new Player("Mohammad");

            //Act
             var actual = player.Position = 5;

            //Assert
            Assert.Equal(5, actual);
        }
    }
}
