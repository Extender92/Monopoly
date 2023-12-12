using Monopoly.Console.GUI;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Tests.ConsoleTests
{
    public class InputTests
    {
        [Fact]
        public void GetUserConfirmation_WhenUserSelectsYes_ReturnsTrue()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();
            var menuSelectorMock = new Mock<IMenuOptionSelector>();

            menuSelectorMock.Setup(m =>
                m.GetSelectedOption(
                    It.IsAny<List<string>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<ConsoleColor>()
                )
            ).Returns(0);

            var input = new Input(consoleMock.Object, menuSelectorMock.Object);

            // Act
            bool result = input.GetUserConfirmation();

            // Assert
            Assert.True(result);
            menuSelectorMock.Verify(
                m => m.GetSelectedOption(
                    It.IsAny<List<string>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<ConsoleColor>()
                ),
                Times.Once);
        }

        [Fact]
        public void GetUserConfirmation_WhenUserSelectsNo_ReturnsFalse()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();
            var menuSelectorMock = new Mock<IMenuOptionSelector>();

            menuSelectorMock.Setup(m =>
                m.GetSelectedOption(
                    It.IsAny<List<string>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<ConsoleColor>()
                )
            ).Returns(1);

            var input = new Input(consoleMock.Object, menuSelectorMock.Object);

            // Act
            bool result = input.GetUserConfirmation();

            // Assert
            Assert.False(result);
            menuSelectorMock.Verify(
                m => m.GetSelectedOption(
                    It.IsAny<List<string>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<ConsoleColor>()
                ),
                Times.Once
            );
        }

        [Fact]
        public void GetNumberOfPlayers_WhenUserSelectsOption_ReturnsCorrectNumberOfPlayers()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();
            var menuSelectorMock = new Mock<IMenuOptionSelector>();

            menuSelectorMock.Setup(m =>
                m.GetSelectedOption(
                    It.IsAny<List<string>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<ConsoleColor>()
                )
            ).Returns(2);

            var input = new Input(consoleMock.Object, menuSelectorMock.Object);

            // Act
            int result = input.GetNumberOfPlayers();

            // Assert
            Assert.Equal(4, result);
            menuSelectorMock.Verify(
                m => m.GetSelectedOption(
                    It.IsAny<List<string>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<ConsoleColor>()
                ),
                Times.Once
            );
            consoleMock.Verify(c => c.Clear(), Times.Exactly(2)); // Once in GetNumberOfPlayers and once in GetSelectedOption
        }
    }
}
