using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monopoly.Console.Helpers;

namespace Monopoly.Tests.ConsoleTests
{
    public class StringHelperTests
    {
        [Fact]
        public void CenterStringReturnsCenterString()
        {
            //Arrange
            string expectedInputString = "   Test   ";
            int expectedTotalLength = 10;

            //Act
            var actual = StringHelper.CenterString(expectedInputString, expectedTotalLength);

            //Assert
            Assert.Equal(expectedInputString, actual);
        }

        [Fact]
        public void CenterStringReturnsOriginalStringIfStringIsEmpty()
        {
            //Arrange
            string expectedInputString = string.Empty;
            int expectedTotalLength = 10;

            //Act
            var actual = StringHelper.CenterString(expectedInputString, expectedTotalLength);

            //Assert
            Assert.Equal(expectedInputString, actual);
        }

        [Fact]
        public void CenterStringReturnsOriginalStringIfStringIsNull()
        {
            //Arrange
            string? expectedInputString = null;
            int expectedTotalLength = 10;

            //Act
            var actual = StringHelper.CenterString(expectedInputString, expectedTotalLength);

            //Assert
            Assert.Equal(expectedInputString, actual);
        }

        [Fact]
        public void CenterStringInListReturnsCenteredStrings()
        {
            //Arrange
            List<string> inputList = new List<string> { "Test1", "Test2", "Test3" };
            int expectedTotalLength = 10;

            //Act 
            List<string> centeredList = StringHelper.CenterStringInList(inputList, expectedTotalLength);

            //Assert
            Assert.Equal(["  Test1   ", "  Test2   ", "  Test3   "], centeredList);
        }

        [Fact]
        public void CenterStringInListReturnsEmptyListIfInputListEmpty()
        {
            // Arrange
            List<string> inputList = new List<string>();
            int totalLength = 10;

            // Act
            List<string> centeredList = StringHelper.CenterStringInList(inputList, totalLength);

            // Assert
            Assert.Empty(centeredList);
        }

        [Fact]
        public void GetListOfStringsFromStringReturnsCorrectList()
        {
            // Arrange
            string inputText = "This is a long text that needs to be split into several strings.";
            int maxLength = 10;

            // Act
            List<string> listOfStrings = StringHelper.GetListOfStringsFromString(inputText, maxLength);

            // Assert
            Assert.Equal(new List<string>
            {
            "This is a",
            "long text",
            "that needs",
            "to be",
            "split into",
            "several",
            "strings."
            }, listOfStrings);
        }



        [Fact]
        public void GetListOfStringsFromStringReturnsEmptyListIfInputEmpty()
        {
            // Arrange
            string inputText = "";
            int maxLength = 10;

            // Act
            List<string> listStrings = StringHelper.GetListOfStringsFromString(inputText, maxLength);

            // Assert
            Assert.Empty(listStrings);
        }
    }
}
