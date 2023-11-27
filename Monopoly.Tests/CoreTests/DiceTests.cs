using Monopoly.Core.Models;

namespace Monopoly.Tests.CoreTests
{
    public class DiceTests
    {
        [Fact]
        public void RoleDiceValidDecieValues()
        {
            //Arrange
            var dice = new Die();

            //Act
            dice.Roll();

            //Assert
            Assert.InRange(dice.DiceOne, 1, 6);
            Assert.InRange(dice.DiceTwo, 1, 6);

        }
    }
}