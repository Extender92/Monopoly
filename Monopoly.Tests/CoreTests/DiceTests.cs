using Monopoly.Core.Models;
using Moq;

namespace Monopoly.Tests.CoreTests
{
    public class DiceTests
    {
        [Fact]
        public void GetDieResult_ReturnsValidResult()
        {
            // Arrange
            var mockDie = new Mock<IDie>();
            mockDie.Setup(d => d.GetDieResult()).Returns(3); 
            var sut = mockDie.Object;

            // Act
            int result = sut.GetDieResult();

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public void GetDieTypeReturnsValidType()
        {
            // Arrange
            var mockDie = new Mock<IDie>();
            mockDie.Setup(d => d.GetDieType()).Returns(6); 
            var sut = mockDie.Object;

            // Act
            int result = sut.GetDieType();

            // Assert
            Assert.Equal(6, result); 
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