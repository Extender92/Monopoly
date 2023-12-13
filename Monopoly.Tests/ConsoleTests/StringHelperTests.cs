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




        [Theory]
        [InlineData("Test", 10, "   Test   ")]
        [InlineData("This is a longer string", 30, "   This is a longer string    ")]
        [InlineData("", 5, "")]
        [InlineData(null, 5, null)]
        public void CenterString_ShouldCenterStringCorrectly(string input, int totalLength, string expected)
        {
            // Act
            var result = StringHelper.CenterString(input, totalLength);

            // Assert
            Assert.Equal(expected, result);
        }


        public static IEnumerable<object[]> CenterStringInListTestData()
        {
            yield return new object[]
            {
            new List<string> { "Test1", "Test2", "Test3" },
            10,
            new List<string> { "  Test1   ", "  Test2   ", "  Test3   " }
            };

            yield return new object[]
            {
            new List<string> { "Short", "Longer String", "Another" },
            15,
            new List<string> { "     Short     ", " Longer String ", "    Another    " }
            };

            yield return new object[]
            {
            new List<string>(),
            5,
            new List<string>()
            };
        }

        [Theory]
        [MemberData(nameof(CenterStringInListTestData))]
        public void CenterStringInList_ShouldCenterStringsInListCorrectly(List<string> inputList, int totalLength, List<string> expectedList)
        {
            // Act
            var result = StringHelper.CenterStringInList(inputList, totalLength);

            // Assert
            Assert.Equal(expectedList, result);
        }

        [Theory]
        [InlineData("This is a long string that needs to be split into multiple lines.", 10, new[] { "This is a", "long", "string", "that needs", "to be", "split into", "multiple", "lines." })]
        [InlineData("This is a long string that needs to be split into multiple lines.", 15, new[] { "This is a long", "string that", "needs to be", "split into", "multiple lines." })]
        [InlineData("This. . is.. a string. that. needs. to be. split. into multiple. lines.", 20, new[] { "This", "", "is.", "a string", "that", "needs", "to be", "split", "into multiple", "lines." })]
        [InlineData("Short text", 20, new[] { "Short text" })]
        [InlineData("", 5, new string[] { })]
        [InlineData(null, 5, new string[] { })]
        public void GetListOfStringsFromString_ShouldSplitStringIntoList(string input, int maxLength, string[] expectedArray)
        {
            // Act
            var result = StringHelper.GetListOfStringsFromString(input, maxLength);

            // Assert
            Assert.Equal(expectedArray, result);
        }

        [Theory]
        [InlineData("Test1", "Test2", "Test3", new[] { "Test1", "Test2", "Test3" })]
        [InlineData("", "", "", new string[] { "", "", "" })]
        [InlineData(null, null, null, new string[] { null, null, null })]
        public void CreateStringList_ShouldCreateListWithGivenStrings(string one, string two, string three ,string[] expected)
        {
            // Act
            var result = StringHelper.CreateStringList(one, two, three);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("Test1", "Test2", new[] { "Test1", "Test2" })]
        [InlineData("", "", new string[] { "", "" })]
        [InlineData(null, null, new string[] { null, null })]
        public void CreateStringList_ShouldCreateListWithLessStrings(string one, string two, string[] expected)
        {
            // Act
            var result = StringHelper.CreateStringList(one, two);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
