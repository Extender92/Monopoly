using Monopoly.Core.Models;
using Moq;

namespace Monopoly.Tests.CoreTests
{
    public class DiceTests
    {
        [Fact]
        public void GetDieResultReturnsValidResult()
        {
            // Arrange
            var mockDie = new Mock<IDie>();
            var expectedDieResult = 4;
            mockDie.Setup(d => d.GetDieResult()).Returns(expectedDieResult);
            var sut = mockDie.Object;

            // Act
            int result = sut.GetDieResult();

            // Assert
            Assert.Equal(expectedDieResult, result);
        }

        [Fact]
        public void GetDieTypeReturnDieSides()
        {
            //Arrange
            var expectedDieSides = 6;
            var mockDie = new Mock<IDie>();
            mockDie.Setup(d => d.GetDieType()).Returns(expectedDieSides);
            var sut = mockDie.Object;

            //Act
            int result = sut.GetDieType();

            //Assert
            Assert.Equal(expectedDieSides, result);
        }
        [Fact]
        public void ScrambleDieCanRollAndGetResult()
        {
            // Arrange
            var mockDie = new Mock<IDie>();
            mockDie.Setup(d => d.ScrambleDie()).Callback(() => mockDie.Setup(d => d.GetDieResult()).Returns(3));
            var sut = mockDie.Object;

            // Act
            sut.ScrambleDie();
            int result = sut.GetDieResult();

            // Assert
            Assert.Equal(3, result);
        }
    }
}