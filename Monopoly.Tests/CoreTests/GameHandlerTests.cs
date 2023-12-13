using Monopoly.Core.Models;
using Monopoly.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            game.Dice = new List<IDie> {dieMock.Object};

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
            var dieMock = new Mock<IDie>();
            dieMock.Setup(d => d.GetDieResult()).Returns(4);

            GameRules gameRules = new GameRules(2, 2, 6);
            Game game = CoreGameSetup.Setup(gameRules);
            var gameHandler = new GameHandler(game);

            Player player = game.Players[0];
            player.Position = 37;
            int salary = game.Rules.Salary;
            int startingMoney = 3000;
            player.Money = startingMoney;


            // Act
            gameHandler.RoleDiceAndMovePlayer(player);

            // Assert
            Assert.Equal(1, player.Position); // Player position should be adjusted to 1 after passing Go
            Assert.True(player.Money > startingMoney); // Player should have received salary
            Assert.Equal(player.Money, startingMoney + salary); // Player should have received correct amount of money
        }
    }
}
