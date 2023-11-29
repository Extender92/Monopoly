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
            string inputString = "   Test   ";
            int totalLength = 10;

            //Act
            var actual = StringHelper.CenterString(inputString, totalLength);

            //Assert
            Assert.Equal(inputString, actual);
        }

        [Fact]
        public void CenterStringReuturnsOriginalStringIfStringIsEmpty()
        {
            //Arrange
            string inputString = string.Empty;
            int totalLength = 10;

            //Act
            var actual = StringHelper.CenterString(inputString, totalLength);

            //Assert
            Assert.Equal(inputString, actual);
        }

        [Fact]
        public void CenterStringReuturnsOriginalStringIfStringIsNull()
        {
            //Arrange
            string inputString = null;
            int totalLength = 10;

            //Act
            var actual = StringHelper.CenterString(inputString, totalLength);

            //Assert
            Assert.Equal(inputString, actual);
        }

        [Fact]
        public void CenterStringInListReuturnsCenterdStrings()
        {
            //Arrange
            List<string> inputList = new List<string> { "Test1", "Test2", "Test3" };
            int totalLength = 10;

            //Act 
            List<string> centerdList = StringHelper.CenterStringInList(inputList, totalLength);

            //Assert
            Assert.Equal(["  Test1   ", "  Test2   ", "  Test3   "], centerdList);
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
