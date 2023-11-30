using Monopoly.Core.Models;

namespace Monopoly.Tests.CoreTests
{
    public class DiceTests
    {
        [Fact]
        public void RoleSetResultBetweenZeroAndDieSides()
        {
            //Arrange
            var expectedDieSides = 6;
            var dice = new Die(expectedDieSides);

            //Act
            dice.Roll();
            var actual = dice.GetDieResult();

            //Assert
            Assert.InRange(actual, 1, expectedDieSides);

        }

        [Fact]
        public void GetDieTypeReturnDieSides()
        {
            //Arrange
            var expectedDieSides = 6;
            var die = new Die(expectedDieSides);

            //Act
            var actual = die.GetDieType();

            //Assert
            Assert.Equal(expectedDieSides, actual);
        }
        [Fact]
        public void RollReturnRandomResult()
        {
            //Arrange
            var expectedDieSides = 6;
            var die = new Die(expectedDieSides);

            //Act
            die.Roll();
            var actual1 = die.GetDieResult();
            var actual2 = die.GetDieResult();

            //Assert
            Assert.Equal(actual1, actual2);

        }
    }
}