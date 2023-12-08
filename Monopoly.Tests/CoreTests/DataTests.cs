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
            var tax = new TaxSquare(expectedPosition, expectedPrice, expectedInfo);

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
            int rentOneStation = 25;
            int rentTwoStation = 50;
            int rentThreeStation = 100;
            int rentFourStation = 200;
            // Act
            var railroad = new RailroadSquare(expectedPosition, expectedInfo, rentOneStation,rentTwoStation, rentThreeStation, rentFourStation);

            // Assert
            Assert.Equal(expectedPosition, railroad.Position);
            Assert.Equal(expectedInfo, railroad.Info);
            Assert.Equal(200, railroad.Price);
        }

        [Fact]
        public void ParkingSpaceSetsPositionAndInfoCorrectly() // ParkingSpaceTest
        {
            // Act
            var parkingSpace = new ParkingSquare();

            // Assert
            Assert.Equal(20, parkingSpace.Position);
            Assert.Equal("Free Parking", parkingSpace.Info);
        }

        [Fact]
        public void JailSpaceSetsPositionAndInfoCorrectly() // JailSpaceTest
        {
            // Act
            var jailSpace = new GoToJailSquare();

            // Assert
            Assert.Equal(30, jailSpace.Position);
            Assert.Equal("Go To Jail", jailSpace.Info);
        }

        [Fact]
        public void JailSetsPositionAndInfoCorrectly() // JailTest
        {
            // Act
            var jail = new JailSquare();

            // Assert
            Assert.Equal(10, jail.Position);
            Assert.Equal("Visiting Jail", jail.Info);
        }

        [Fact]
        public void GoSpaceSetsPositionAndInfoCorrectly()  //  GoSpaceTest
        {
            //Arrange 
            var expectedInfo = "Go";

            // Act
            var goSpace = new GoSquare();

            // Assert
            Assert.Equal(0, goSpace.Position);
            Assert.Equal(expectedInfo, goSpace.Info);
        }

        [Fact]
        public void UtilitySetsPositionInfoAndPriceCorrectly()
        {
            // Arrange
            int expectedPosition = 12;
            int rentOneStation = 4;
            int rentTwoStation =  10 ;
            // Act
            var utility = new UtilitySquare(expectedPosition, "Utility" , rentOneStation, rentTwoStation);

            // Assert
            Assert.Equal(expectedPosition, utility.Position);
            Assert.Equal("Utility", utility.Info);
            Assert.Equal(150, utility.Price);
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
                item => Assert.Equal("Kings Cross Station", item.Info),
                item => Assert.Equal("Marylebone Station", item.Info),
                item => Assert.Equal("Fenchurch Street Station", item.Info),
                item => Assert.Equal("Liverpool Street Station", item.Info)
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
                    Assert.Equal("Income Tax", item.Info);
                },
                item =>
                {
                    Assert.Equal(expectedSecondPositionTax, item.Position);
                    Assert.Equal(100, item.Price);
                    Assert.Equal("Luxury Tax", item.Info);
                }
            );
        }

        [Fact]
        public void GetUtilitySquareDataReturnsCorrectUtilitySquareProperties()
        {
            // Arrange
            int expectedFirstPositionUtility = 12;
            int expectedSecondPositionUtility = 27;
            GameRules gameRules = new GameRules(2, 1, 6);

            // Act
            List<UtilitySquare> result = Core.Data.Data.GetUtilitySquareData(gameRules);

            // Assert
            Assert.Collection(result,
                item =>
                {
                    Assert.Equal(expectedFirstPositionUtility, item.Position);
                    Assert.Equal("Electric Company", item.Info);
                },
                item =>
                {
                    Assert.Equal(expectedSecondPositionUtility, item.Position);
                    Assert.Equal("Water Works", item.Info);
                }
            );
        }
    }
}
