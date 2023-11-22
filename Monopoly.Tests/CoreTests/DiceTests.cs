using Monopoly.Core.Models;

namespace Monopoly.Tests.CoreTests
{
    public class DiceTests
    {
        [Fact]
        public void RoleDiceValidDecieValues()
        {
            //Arrange
            var dice = new Dice();

            //Act
            dice.RoleDice();

            //Assert
            Assert.InRange(dice.DiceOne, 1, 6);
            Assert.InRange(dice.DiceTwo, 1, 6);

        }
    }
}