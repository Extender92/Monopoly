using Monopoly.Core;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models.FortuneCard;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Tests.CoreTests
{
    public class DataTests
    {

        [Fact]
        public void TaxSetsPositionAndInfoCorrectly()    // TaxTest
        {
            // Arrange
            int expectedPosition = 10;
            int expectedPrice = 150;
            string expectedInfo = "Test info";
            string expectedName = "Tax";

            // Act
            var tax = new TaxSquare(expectedPosition, expectedPrice, expectedName, expectedInfo);

            // Assert
            Assert.Equal(expectedPosition, tax.Position);
            Assert.Equal(expectedInfo, tax.Presentation.Description);
            Assert.Equal(expectedPrice, tax.Price);
            Assert.Equal(expectedName, tax.Presentation.DisplayText);
        }



        [Fact]
        public void RailroadSetsPositionAndInfoCorrectly() // RailroadTest
        {
            // Arrange
            int expectedPosition = 5;
            string expectedInfo = "Test info";
            int expectedPrice = 200;
            int expectedRenOneStation = 25;
            int expectedRentTwoStation = 50;
            int expectedRenThreeStation = 100;
            int expectedRentFourStation = 200;
            // Act
            var railroad = new RailroadSquare(expectedPosition, expectedInfo, expectedPrice, expectedRenOneStation, expectedRentTwoStation, expectedRenThreeStation, expectedRentFourStation, 2);

            // Assert
            Assert.Equal(expectedPosition, railroad.Position);
            Assert.Equal(expectedInfo, railroad.Presentation.DisplayText);
            Assert.Equal(expectedRenOneStation, railroad.RentOneStation);
            Assert.Equal(expectedRentTwoStation, railroad.RentTwoStation);
            Assert.Equal(expectedRenThreeStation, railroad.RentThreeStation);
            Assert.Equal(expectedRentFourStation, railroad.RentFourStation);
        }

        [Fact]
        public void ParkingSpaceSetsPositionAndInfoCorrectly() // ParkingSpaceTest
        {
            // Arrange
            int expectedPosition = 20;
            string expectedInfo = "Free Parking";

            // Act
            var parkingSpace = new ParkingSquare(expectedPosition, expectedInfo);

            // Assert
            Assert.Equal(expectedPosition, parkingSpace.Position);
            Assert.Equal(expectedInfo, parkingSpace.Presentation.DisplayText);
        }

        [Fact]
        public void JailSpaceSetsPositionAndInfoCorrectly() // JailSpaceTest
        {
            // Arrange
            int expectedPosition = 30;
            string expectedInfo = "Go to jail! Do not collect salary";
            string expectedName = "Go To Jail";

            // Act
            var jailSpace = new GoToJailSquare(expectedPosition, expectedName, expectedInfo);

            // Assert
            Assert.Equal(expectedPosition, jailSpace.Position);
            Assert.Equal(expectedInfo, jailSpace.Presentation.Description);
            Assert.Equal(expectedName, jailSpace.Presentation.DisplayText);
        }

        [Fact]
        public void JailSetsPositionAndInfoCorrectly() // JailTest
        {
            // Arrange
            int expectedPosition = 10;
            string expectedInfo = "Visiting Jail";
            string expectedInJail = "In Jail";
            string expectedName = "Jail";

            // Act
            var jail = new JailSquare(expectedPosition, expectedName, expectedInfo, expectedInJail);

            // Assert
            Assert.Equal(expectedPosition, jail.Position);
            Assert.Equal($"{expectedInfo} || {expectedInJail}", jail.Presentation.Description);
            Assert.Equal(expectedName, jail.Presentation.DisplayText);
        }

        [Fact]
        public void GoSpaceSetsPositionAndInfoCorrectly()  //  GoSpaceTest
        {
            //Arrange 
            int expectedPosition = 0;
            var expectedInfo = "Collect salary as you pass GO";
            string name = "GO";

            // Act
            var goSpace = new GoSquare(expectedPosition, name , expectedInfo);

            // Assert
            Assert.Equal(expectedPosition, goSpace.Position);
            Assert.Equal(expectedInfo, goSpace.Presentation.Description);
            Assert.Equal(name, goSpace.Presentation.DisplayText);
        }

        [Fact]
        public void UtilitySetsPositionInfoAndPriceCorrectly()
        {
            // Arrange
            int expectedPosition = 12;
            var expectedInfo = "Utility";
            int expectedPrice = 150;
            int expectedRentOneUtility = 4;
            int expectedRentTwoUtility = 10;

            // Act
            var utility = new UtilitySquare(expectedPosition, expectedInfo, expectedPrice, expectedRentOneUtility, expectedRentTwoUtility, 2);

            // Assert
            Assert.Equal(expectedPosition, utility.Position);
            Assert.Equal(expectedInfo, utility.Presentation.DisplayText);
            Assert.Equal(expectedPrice, utility.Price);
            Assert.Equal(expectedRentOneUtility, utility.RentOneUtility);
            Assert.Equal(expectedRentTwoUtility, utility.RentTwoUtility);
        }

        [Fact]
        public void GetRailroadSquareDataReturnsCorrectRailroadSquareProperties()
        {
            // Arrange
            int expectedFirstPositionRailroad = 5;
            int expectedSecondPositionRailroad = 15;
            int expectedThirdPositionRailroad = 25;
            int expectedFourthPositionRailroad = 35;
            GameRules gameRules = new GameRules(2, 1, 6);

            // Act
            List<RailroadSquare> result = Core.Data.Data.GetRailroadSquareData(gameRules);

            // Assert
            Assert.Collection(result,
                item => Assert.Equal(expectedFirstPositionRailroad, item.Position),
                item => Assert.Equal(expectedSecondPositionRailroad, item.Position),
                item => Assert.Equal(expectedThirdPositionRailroad, item.Position),
                item => Assert.Equal(expectedFourthPositionRailroad, item.Position)
            );

            Assert.Collection(result,
                item => Assert.Equal("Kings Cross Station", item.Presentation.DisplayText),
                item => Assert.Equal("Marylebone Station", item.Presentation.DisplayText),
                item => Assert.Equal("Fenchurch Street Station", item.Presentation.DisplayText),
                item => Assert.Equal("Liverpool Street Station", item.Presentation.DisplayText)
            );
        }

        [Fact]
        public void GetTaxSquareDataReturnsCorrectTaxSquareProperties()
        {
            // Arrange
            int expectedFirstPositionTax = 4;
            int expectedSecondPositionTax = 38;
            GameRules gameRules = new GameRules(2, 1, 6);

            // Act
            List<TaxSquare> result = Core.Data.Data.GetTaxSquareData(gameRules);

            // Assert
            Assert.Collection(result,
                item =>
                {
                    Assert.Equal(expectedFirstPositionTax, item.Position);
                    Assert.Equal(200, item.Price);
                    Assert.Equal("Income Tax", item.Presentation.DisplayText);
                    Assert.Equal("Pay tax", item.Presentation.Description);
                },
                item =>
                {
                    Assert.Equal(expectedSecondPositionTax, item.Position);
                    Assert.Equal(100, item.Price);
                    Assert.Equal("Luxury Tax", item.Presentation.DisplayText);
                    Assert.Equal("Pay tax", item.Presentation.Description);
                }
            );
        }

        [Fact]
        public void GetUtilitySquareDataReturnsCorrectUtilitySquareProperties()
        {
            // Arrange
            int expectedFirstPositionUtility = 12;
            int expectedSecondPositionUtility = 28;
            GameRules gameRules = new GameRules(2, 1, 6);

            // Act
            List<UtilitySquare> result = Core.Data.Data.GetUtilitySquareData(gameRules);

            // Assert
            Assert.Collection(result,
                item =>
                {
                    Assert.Equal(expectedFirstPositionUtility, item.Position);
                    Assert.Equal("Electric Company", item.Presentation.DisplayText);
                    Assert.Equal(150, item.Price);
                    Assert.Equal(4, item.RentOneUtility);
                    Assert.Equal(10, item.RentTwoUtility);
                    Assert.Equal(75, item.MortgageValue);
                },
                item =>
                {
                    Assert.Equal(expectedSecondPositionUtility, item.Position);
                    Assert.Equal("Water Works", item.Presentation.DisplayText);
                    Assert.Equal(150, item.Price);
                    Assert.Equal(4, item.RentOneUtility);
                    Assert.Equal(10, item.RentTwoUtility);
                    Assert.Equal(75, item.MortgageValue);
                }
            );
        }
    }
}
