using Monopoly.Core;
using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Tests.CoreTests
{
    public class GameTests
    {
        [Fact]
        public void GameSetup_InitializesGameCorrectly()
        {
            // Arrange
            var gameRules = new GameRules(numberOfPlayers: 4, numberOfDice: 2, dieSides: 6);

            // Act
            var game = CoreGameSetup.Setup(gameRules);

            // Assert
            Assert.NotNull(game);
            Assert.NotNull(game.Players);
            Assert.NotNull(game.Dice);
            Assert.NotNull(game.Rules);
            Assert.Equal(4, game.Players.Count); // Assuming numberOfPlayers is 4
            Assert.Equal(2, game.Dice.Count); // Assuming numberOfDice is 2
            Assert.Equal(6, game.Rules.DieSides); // Assuming dieSides is 6
            Assert.NotNull(game.Handler);
            Assert.NotNull(game.Logs);
            Assert.NotNull(game.Board);
            Assert.NotNull(game.Transactions);
            Assert.NotNull(game.TheJail);
            Assert.NotNull(game.FortuneCard);
            Assert.Equal(0, game.Fines);
        }
    }
}
