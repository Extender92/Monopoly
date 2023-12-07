using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
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

            // Act
            var tax = new TaxSquare(expectedPosition);

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
            var railroad = new RailroadSquare(expectedPosition);

            // Assert
            Assert.Equal(expectedPosition, railroad.Position);
            Assert.Equal("Railroad", railroad.Info);
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
            // Act
            var goSpace = new GoSquare();

            // Assert
            Assert.Equal(0, goSpace.Position);
            Assert.Equal("Go", goSpace.Info);
        }
    }
}
