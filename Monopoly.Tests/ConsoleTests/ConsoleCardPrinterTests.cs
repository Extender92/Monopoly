using Monopoly.Console.GUI;
using Monopoly.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Tests.ConsoleTests
{
    public class ConsoleCardPrinterTests
    {
        [Fact]
        public void PrintCard_PrintsCardCorrectly()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();

            var output = new StringWriter();

            consoleMock.Setup(c => c.Write(It.IsAny<string>())).Callback<string>(s => output.Write(s));

            var squares = new List<Core.Models.Board.Square>();
            var rules = new GameRules(2, 2, 6);

            var consoleCardPrinter = new ConsoleCardPrinter(consoleMock.Object, squares, rules);

            // Act
            consoleCardPrinter.PrintCard("Test Card", 14, 3, new List<string> { "Info Line 1", "Info Line 2", "Info Line 3" }, ConsoleColor.Red, ConsoleColor.Green);

            // Assert
            consoleMock.Verify(c => c.SetPosition(It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
            consoleMock.Verify(c => c.SetTextColor(It.IsAny<ConsoleColor>()), Times.AtLeastOnce);
            consoleMock.Verify(c => c.ResetColor(), Times.AtLeastOnce);
            consoleMock.Verify(c => c.Write(It.IsAny<string>()), Times.AtLeastOnce);

            string expectedOutput =
                "Test Card" +
                "┌──────────────┐" +
                "││" +
                "├──────────────┤" +
                "│ Info Line 1 │" +
                "│ Info Line 2 │" +
                "│ Info Line 3 │" +
                "│              │" +
                "└──────────────┘";

            Assert.Equal(expectedOutput, output.ToString().Replace("\r", "").Trim());

            // Clean up: Restore console output
            System.Console.SetOut(System.Console.Out);
        }
    }
}
