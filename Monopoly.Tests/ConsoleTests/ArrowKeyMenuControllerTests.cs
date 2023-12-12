using Monopoly.Console.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Tests.ConsoleTests
{
    public class ArrowKeyMenuControllerTests
    {
        [Theory]
        [InlineData(true, 4, 3, 9, ConsoleKey.LeftArrow, 3)]
        [InlineData(true, 0, 3, 9, ConsoleKey.LeftArrow, 0)]
        [InlineData(true, 1, 3, 9, ConsoleKey.LeftArrow, 0)]
        [InlineData(true, 5, 3, 9, ConsoleKey.UpArrow, 2)]
        [InlineData(true, 2, 3, 9, ConsoleKey.RightArrow, 2)]
        [InlineData(true, 7, 3, 9, ConsoleKey.RightArrow, 8)]
        [InlineData(true, 2, 3, 9, ConsoleKey.DownArrow, 5)]
        [InlineData(false, 3, 3, 9, ConsoleKey.Escape, 3)]
        [InlineData(true, 3, 3, 9, ConsoleKey.Escape, -1)]
        public void HandleArrowKeyInput_ShouldHandleInputCorrectly(
            bool canCancel, int initialIndex, int optionsPerLine, int optionCount, ConsoleKey key, int expectedIndex)
        {
            // Act
            int result = ArrowKeyMenuController.HandleArrowKeyInput(canCancel, initialIndex, optionsPerLine, optionCount, key);

            // Assert
            Assert.Equal(expectedIndex, result);
        }
    }
}
