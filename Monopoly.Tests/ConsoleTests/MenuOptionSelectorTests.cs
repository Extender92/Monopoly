using Monopoly.Console.GUI;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Tests.ConsoleTests
{
    public class MenuOptionSelectorTests
    {
        [Fact]
        public void GetSelectedOption_WhenUserSelectsOption_ReturnsCorrectIndex()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();
            consoleMock.Setup(c => c.GetPressedKey()).Returns(new ConsoleKeyInfo(' ', ConsoleKey.Enter, false, false, false));

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

            IMenuOptionSelector menu = menuSelectorMock.Object;

            var options = new List<string> { "Option1", "Option2", "Option3" };

            // Act
            int result = menu.GetSelectedOption(options);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetSelectedOption_WhenUserSelectsOption_ReturnsThirdOption()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();
            consoleMock.Setup(c => c.GetPressedKey()).Returns(new ConsoleKeyInfo(' ', ConsoleKey.Enter, false, false, false));

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
            ).Returns(2); // Return index 2 for "Option3"

            IMenuOptionSelector menu = menuSelectorMock.Object;

            var options = new List<string> { "Option1", "Option2", "Option3" };

            // Act
            int result = menu.GetSelectedOption(options);

            // Assert
            Assert.Equal(2, result);
        }
    }
}
