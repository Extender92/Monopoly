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
        public void CanConsoleWriteLineToConsole()
        {
            //Arrange
            var mockConsoleWrapper = new Mock<IConsoleWrapper>();
            var text = "Hello is this test";

            //Act
            mockConsoleWrapper.Object.WriteLine(text);

            //Assert
            mockConsoleWrapper.Verify(x => x.WriteLine(text), Times.Once);
        }


    }
}
