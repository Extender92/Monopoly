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
        public void TaxSetsPositionAndInfoCorrectly()    //TaxTest
        {
            // Arrange
            int expectedPosition = 10;
            int expectedPrice = 150;
            string expectedInfo = "Test info";

            // Act
            string info = "Test info";
            int Position = 10;
            int Price = 150;
            var tax = new TaxSquare(Position, Price, "", info);

            // Assert
            Assert.Equal(expectedPosition, tax.Position);
            Assert.Equal(expectedInfo, tax.Info);
            Assert.Equal(expectedPrice, tax.Price);
        }



        [Fact]
        public void RailroadSetsPositionAndInfoCorrectly() // RailroadTest
        {
            // Arrange
            int expectedPosition = 5;
            string expectedInfo = "Test info";
            int expectedPrice = 200;

            // Act
            int position = 5;
            int price = 200;
            int rentOneStation = 25;
            int rentTwoStation = 50;
            int rentThreeStation = 100;
            int rentFourStation = 200;
            string info = "Test info";
            var railroad = new RailroadSquare(position, info, price, rentOneStation, rentTwoStation, rentThreeStation, rentFourStation, 2);

            // Assert
            Assert.Equal(expectedPosition, railroad.Position);
            Assert.Equal(expectedInfo, railroad.Name);
            Assert.Equal(expectedPrice, railroad.Price);
        }

        [Fact]
        public void ParkingSpaceSetsPositionAndInfoCorrectly() // ParkingSpaceTest
        {
            // Arrange
            int expectedPosition = 20;
            string expectedInfo = "Free Parking";

            // Act
            int position = 20;
            string info = "Free Parking";
            var parkingSpace = new ParkingSquare(position, info);

            // Assert
            Assert.Equal(expectedPosition, parkingSpace.Position);
            Assert.Equal(expectedInfo, parkingSpace.Name);
        }

        [Fact]
        public void JailSpaceSetsPositionAndInfoCorrectly() // JailSpaceTest
        {
            // Arrange
            int expectedPosition = 30;
            string expectedInfo = "Go To Jail";

            // Act
            int position = 30;
            string info = "Go To Jail";
            var jailSpace = new GoToJailSquare(position, "", info);

            // Assert
            Assert.Equal(expectedPosition, jailSpace.Position);
            Assert.Equal(expectedInfo, jailSpace.Info);
        }

        [Fact]
        public void JailSetsPositionAndInfoCorrectly() // JailTest
        {
            // Arrange
            int expectedPosition = 10;
            string expectedInfo = "Visiting Jail";
            string expectedInJail = "In Jail";

            // Act
            int position = 10;
            string info = "Visiting Jail";
            string inJail = "In Jail";
            var jail = new JailSquare(position, "", info, inJail);

            // Assert
            Assert.Equal(expectedPosition, jail.Position);
            Assert.Equal(expectedInfo, jail.Info);
            Assert.Equal(expectedInJail, jail.InJailInfo);
        }

        [Fact]
        public void GoSpaceSetsPositionAndInfoCorrectly()  //  GoSpaceTest
        {
            //Arrange 
            int ExpectedPosition = 0;
            var expectedInfo = "Go";

            // Act
            int position = 0;
            var info = "Go";
            var goSpace = new GoSquare(position, "", info);

            // Assert
            Assert.Equal(ExpectedPosition, goSpace.Position);
            Assert.Equal(expectedInfo, goSpace.Info);
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
            int position = 12;
            var info = "Utility";
            int price = 150;
            int rentOneUtility = 4;
            int rentTwoUtility = 10;
            var utility = new UtilitySquare(position, info, price, rentOneUtility, rentTwoUtility, 2);

            // Assert
            Assert.Equal(expectedPosition, utility.Position);
            Assert.Equal(expectedInfo, utility.Name);
            Assert.Equal(expectedPrice, utility.Price);
            Assert.Equal(expectedRentOneUtility, utility.RentOneUtility);
            Assert.Equal(expectedRentTwoUtility, utility.RentTwoUtility);
        }

        [Fact]
        public void GetGoToJailSquareDataReturnsCorrectNumberOfItems() 
        {
            // Arrange
            GameRules gameRules = new GameRules(2, 2, 6);
            int expectedCount = 1;

            // Act
            List<GoToJailSquare> result = Core.Data.Data.GetGoToJailSquareData(gameRules);

            // Assert
            Assert.Equal(expectedCount, result.Count);
        }

        [Fact]
        public void GetGoToJailSquareDataReturnsGoToJailSquareObjects()
        {
            // Arrange
            GameRules gameRules = new GameRules(2, 1, 6);

            // Act
            List<GoToJailSquare> result = Core.Data.Data.GetGoToJailSquareData(gameRules);

            // Assert
            Assert.All(result, item => Assert.IsType<GoToJailSquare>(item));
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
                item => Assert.Equal("Kings Cross Station", item.Name),
                item => Assert.Equal("Marylebone Station", item.Name),
                item => Assert.Equal("Fenchurch Street Station", item.Name),
                item => Assert.Equal("Liverpool Street Station", item.Name)
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
                    Assert.Equal("Income Tax", item.Name);
                    Assert.Equal("Pay tax", item.Info);
                },
                item =>
                {
                    Assert.Equal(expectedSecondPositionTax, item.Position);
                    Assert.Equal(100, item.Price);
                    Assert.Equal("Luxury Tax", item.Name);
                    Assert.Equal("Pay tax", item.Info);
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
                    Assert.Equal("Electric Company", item.Name);
                    Assert.Equal(150, item.Price);
                    Assert.Equal(4, item.RentOneUtility);
                    Assert.Equal(10, item.RentTwoUtility);
                    Assert.Equal(75, item.MortgageValue);
                },
                item =>
                {
                    Assert.Equal(expectedSecondPositionUtility, item.Position);
                    Assert.Equal("Water Works", item.Name);
                    Assert.Equal(150, item.Price);
                    Assert.Equal(4, item.RentOneUtility);
                    Assert.Equal(10, item.RentTwoUtility);
                    Assert.Equal(75, item.MortgageValue);
                }
            );
        }
    }
}
