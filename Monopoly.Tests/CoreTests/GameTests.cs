using Monopoly.Core;
using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Tests.CoreTests
{
    public class GameTests
    {
        [Fact]
        public void GameSetUpReturnsValidGame()
        {
            // Arrange
            int numberOfPlayers = 4;
            int numberOfDice = 2;
            int dieSides = 6;

            //Act
            Game game = GameSetup.Setup(numberOfPlayers, numberOfDice, dieSides);

            //Assert
            Assert.NotNull(game);
            Assert.Equal(numberOfPlayers, game.Players.Count);
            Assert.Equal(numberOfDice, game.Dice.Count);

        }
    }
}
