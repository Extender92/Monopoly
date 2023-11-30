using Monopoly.Console.GUI;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Tests.ConsoleTests
{
    public class ConsoleWrapperTests
    {
        [Fact]
        public void CanConsoleWriteLinePrintToSystemConsole()
        {
            // Arrange
            var text = "TestPrint";
            var mockConsole = new Mock<IConsoleWrapper>();
            mockConsole.Setup(c => c.WriteLine(It.IsAny<string>()));

            // Act
            mockConsole.Object.WriteLine(text);

            // Assert
            mockConsole.Verify(c => c.WriteLine(text), Times.Once);
        }

        [Fact]
        public void ReadLineShouldlCallSystemConsole()
        {
            // Arrange
            var input = "TestInput";
            var mockConsole = new Mock<IConsoleWrapper>();
            mockConsole.Setup(c => c.ReadLine()).Returns(input);

            // Arrange
            var actual = mockConsole.Object.ReadLine();

            //Assert
            Assert.Equal(input, actual);
            mockConsole.Verify(c => c.ReadLine(), Times.Once);

        }

        [Fact]
        public void ClearShouldCallSystemConsoleClear()
        {
            // Arrange
            var mockConsole = new Mock<IConsoleWrapper>();

            // Act
            mockConsole.Object.Clear();

            // Assert
            mockConsole.Verify(c => c.Clear(), Times.Once);
        }


        [Fact]
        public void ReadKeyShouldCallSystemConsoleReadKey()
        {
            // Arrange
            var mockConsole = new Mock<IConsoleWrapper>();
            mockConsole.Setup(c => c.ReadKey()).Returns("R");

            // Act
            var actual = mockConsole.Object.ReadKey();

            // Assert
            Assert.Equal("R", actual);
            mockConsole.Verify(c => c.ReadKey(), Times.Once);
        }

        [Fact]
        public void SetTextColorShouldCallSystemConsoleSetForegroundColor()
        {
            // Arrange
            var mockConsole = new Mock<IConsoleWrapper>();

            // Act
            mockConsole.Object.SetTextColor(ConsoleColor.Red);

            // Assert
            mockConsole.Verify(c => c.SetTextColor(ConsoleColor.Red), Times.Once);
        }

        [Fact]
        public void ResetColorShouldCallSystemConsoleResetColor()
        {
            // Arrange
            var mockConsole = new Mock<IConsoleWrapper>();

            // Act
            mockConsole.Object.ResetColor();

            // Assert
            mockConsole.Verify(c => c.ResetColor(), Times.Once);
        }

        [Fact]
        public void SetPositionShouldCallSystemConsoleSetCursorPosition()
        {
            // Arrange
            var mockConsole = new Mock<IConsoleWrapper>();

            // Act
            mockConsole.Object.SetPosition(10, 20);

            // Assert
            mockConsole.Verify(c => c.SetPosition(10, 20), Times.Once);
        }
    }
}
