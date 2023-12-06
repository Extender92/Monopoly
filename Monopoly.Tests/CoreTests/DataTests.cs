using Monopoly.Core.Models;
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
        public void SquarePositionSetAndGetCorrectly()     //SquareTest
        {
            // Arrange
            var square = new Square
            {
                // Act
                Position = 42
            };

            // Assert
            Assert.Equal(42, square.Position);
        }

        [Fact]
        public void SquareInfoSetAndGetCorrectly()     //SquareTest
        {
            // Arrange
            var square = new Square
            {
                // Act
                Info = "TestInfo"
            };

            // Assert
            Assert.Equal("TestInfo", square.Info);
        }

        [Fact]
        public void TaxSetsPositionAndInfoCorrectly()    //TaxTest
        {
            // Arrange
            int expectedPosition = 10;

            // Act
            var tax = new Tax(expectedPosition);

            // Assert
            Assert.Equal(expectedPosition, tax.Position);
            Assert.Equal("Tax", tax.Info);
        }



        [Fact]
        public void RailroadSetsPositionAndInfoCorrectly() // RailroadTest
        {
            // Arrange
            int expectedPosition = 5;

            // Act
            var railroad = new Railroad(expectedPosition);

            // Assert
            Assert.Equal(expectedPosition, railroad.Position);
            Assert.Equal("Railroad", railroad.Info);
        }

        [Fact]
        public void ParkingSpaceSetsPositionAndInfoCorrectly() // ParkingSpaceTest
        {
            // Act
            var parkingSpace = new ParkingSpace();

            // Assert
            Assert.Equal(20, parkingSpace.Position);
            Assert.Equal("Free Parking", parkingSpace.Info);
        }

        [Fact]
        public void JailSpaceSetsPositionAndInfoCorrectly() // JailSpaceTest
        {
            // Act
            var jailSpace = new JailSpace();

            // Assert
            Assert.Equal(30, jailSpace.Position);
            Assert.Equal("Go To Jail", jailSpace.Info);
        }

        [Fact]
        public void JailSetsPositionAndInfoCorrectly() // JailTest
        {
            // Act
            var jail = new Jail();

            // Assert
            Assert.Equal(10, jail.Position);
            Assert.Equal("Visiting Jail", jail.Info);
        }

        [Fact]
        public void GoSpaceSetsPositionAndInfoCorrectly()  //  GoSpaceTest
        {
            // Act
            var goSpace = new GoSpace();

            // Assert
            Assert.Equal(0, goSpace.Position);
            Assert.Equal("Go", goSpace.Info);
        }
    }
}
