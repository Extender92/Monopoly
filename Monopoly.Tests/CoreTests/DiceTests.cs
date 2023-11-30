using Monopoly.Core.Models;
using Moq;

namespace Monopoly.Tests.CoreTests
{
    public class DiceTests
    {
        [Fact]
        public void RollShouldSetResultToDieType()
        {
            // Arrange
            var expectedDieSides = 6;
            var mockDie = new Mock<IDie>();
            mockDie.Setup(d => d.GetDieType()).Returns(expectedDieSides);

            // Act
            mockDie.Object.Roll();
            var actual = mockDie.Object.GetDieResult();

            // Assert
            mockDie.Verify(x => x.GetDieResult(), Times.Once);

        }

        [Fact]
        public void GetDieTypeReturnDieSides()
        {
            //Arrange
            var expectedDieSides = 6;
            var mockDie = new Mock<IDie>();
            mockDie.Setup(d => d.GetDieType()).Returns(expectedDieSides);

            //Act
            var actual = mockDie.Object.GetDieType();

            //Assert
            Assert.Equal(expectedDieSides, actual);
        }
        [Fact]
        public void RollReturnRandomResult()
        {
            //Arrange
            var expectedDieSides = 6;
            var mockDie = new Mock<IDie>();
            mockDie.Setup(die => die.GetDieResult()).Returns(expectedDieSides);
            var die = new Die(expectedDieSides);

            //Act
            die.Roll();
            var actual = mockDie.Object.GetDieResult();

            //Assert
            Assert.Equal(expectedDieSides, actual);

        }
    }
}