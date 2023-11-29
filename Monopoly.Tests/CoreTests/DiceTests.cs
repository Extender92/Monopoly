using Monopoly.Core.Models;

namespace Monopoly.Tests.CoreTests
{
    public class DiceTests
    {
        [Fact]
        public void RoleSetResultBetweenZeroAndDieSides()
        {
            //Arrange
            var dieSides = 6;
            var dice = new Die(dieSides);

            //Act
            dice.Roll();
            var actual = dice.GetDieResult();

            //Assert
            Assert.InRange(actual,0 ,dieSides);

        }

        [Fact]
        public void GetDieTypeReturnDieSides()
        {
            //Arange
            var dieSides = 6;
            var die = new Die(dieSides);

            //Act
            var actual = die.GetDieType();

            //Assert
            Assert.Equal(dieSides, actual);
        }
        [Fact]
        public void RollReturnRandomResult()
        {
            //Arange
            var dieSides = 6;
            var die = new Die(dieSides);

            //Act
            die.Roll();
            var actual1 = die.GetDieResult();
            var actual = die.GetDieResult();

            //Assert
            Assert.Equal(actual1, actual);

        }
    }
}