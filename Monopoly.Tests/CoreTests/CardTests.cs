using Monopoly.Core.Data;
using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Monopoly.Tests.CoreTests
{
    public class CardTests
    {
        [Fact]
        public void CanCreateOneStreetCard()
        {
            // Arrange
            ConsoleColor color = ConsoleColor.Blue;
            string name = "Oxford Street";
            int rent = 100;
            int rentWithColor = 150;
            int rentOneHouse = 200;
            int rentTwoHouses = 300;
            int rentThreeHouses = 400;
            int rentFourHouses = 500;
            int rentHotels = 600;
            int housesCost = 1000;
            int hotelsCost = 2000;
            int price = 5000;
            int mortgageValue = 2500;
            int position = 1;

            //Act 
            var street = new Street(color, name, rent, rentWithColor, rentOneHouse, rentTwoHouses,
                        rentThreeHouses, rentFourHouses, rentHotels, housesCost, hotelsCost,
                        price, mortgageValue, position);

            //Assert
            Assert.Equal(color, street.Color);
            Assert.Equal(name, street.Name);
            Assert.Equal(rent, street.Rent);
            Assert.Equal(rentWithColor, street.RentWithColor);
            Assert.Equal(rentOneHouse, street.RentOneHouses);
            Assert.Equal(rentTwoHouses, street.RentTwoHouses);
            Assert.Equal(rentThreeHouses, street.RentThreeHouses);
            Assert.Equal(rentFourHouses, street.RentFourHouses);
            Assert.Equal(rentHotels, street.RentHotels);
            Assert.Equal(housesCost, street.HousesCost);
            Assert.Equal(hotelsCost, street.HotelsCost);
            Assert.Equal(price, street.Price);
            Assert.Equal(mortgageValue, street.MortgageValue);

        }

        [Theory]
        [InlineData(ConsoleColor.Blue, "Oxford Street", 500, 100, 152, 150, 200, 600, 600, 1000, 2000, 4000, 2050,5)]
        [InlineData(ConsoleColor.Green, "Whitechapel Road", 10, 150, 200, 300, 450, 500, 600, 1000, 2000, 3000, 2100,4)]
        [InlineData(ConsoleColor.Red, "The Angel Islington", 20, 150, 200, 302, 400, 500, 600, 1000, 2000, 3000, 2200,2)]
        [InlineData(ConsoleColor.White, "Euston Road", 120, 150, 200, 350, 420, 500, 600, 1000, 2000, 3000, 2250,1)]
        public void CanCreateFourStreetCard(ConsoleColor color , string name, int rent, int rentWithColor,
                                         int rentOneHouse, int rentTwoHouses, int rentThreeHouses,
                                         int rentFourHouses, int rentHotels, int housesCost, int hotelsCost,
                                         int price, int mortgageValue , int position)
        {

            //Act 
            var street = new Street(color, name, rent, rentWithColor, rentOneHouse, rentTwoHouses,
                        rentThreeHouses, rentFourHouses, rentHotels, housesCost, hotelsCost,
                        price, mortgageValue, position);

            // Assert
            Assert.Equal(color, street.Color);
            Assert.Equal(name, street.Name);
            Assert.Equal(rent, street.Rent);
            Assert.Equal(rentWithColor, street.RentWithColor);
            Assert.Equal(rentOneHouse, street.RentOneHouses);
            Assert.Equal(rentTwoHouses, street.RentTwoHouses);
            Assert.Equal(rentThreeHouses, street.RentThreeHouses);
            Assert.Equal(rentFourHouses, street.RentFourHouses);
            Assert.Equal(rentHotels, street.RentHotels);
            Assert.Equal(housesCost, street.HousesCost);
            Assert.Equal(hotelsCost, street.HotelsCost);
            Assert.Equal(price, street.Price);
            Assert.Equal(mortgageValue, street.MortgageValue);
        }

        [Fact]
        public void GetStreetCardsReturnsCorrectNumberOfCards()
        {
            //Arrange
            var expectedNumberOfStreet = 22;
            var streetCards = CardSet.GetStreetCards();

            //Act
            var numberOfCards = streetCards.Count;

            //Assert
            Assert.Equal(expectedNumberOfStreet, numberOfCards);
        }
        [Fact]
        public void GetStreetCardsReturnsValidStreet()
        {
            //Arrange
            var streetCards = CardSet.GetStreetCards();

            //Act and Assert
            foreach (var card in streetCards)
            {
                Assert.NotNull(card);
                Assert.IsType<Street>(card);
                Assert.True(card.Rent > 0);
            }
        }

        [Fact]
        public void CanCreateOneChanceCard()
        {
            //Arrange
            string expectedInfo = "Get Out of Jail Free";

            //Act
            var chance = new Chance(expectedInfo);

            //Assert
            Assert.Equal(expectedInfo, chance.Info);

        }

        [Fact]
        public void GetChanceCardsReturnsValidChance()
        {
            //Arrange
            var chanceCard = CardSet.GetChanceCards();

            //Act and Assert
            foreach(var card in chanceCard)
            {
                Assert.NotNull(card);
                Assert.NotNull(card.Info);
                Assert.IsType<Chance>(card);
            }
        }

        [Fact]
        public void CanCreateOneCommunityChestCard()
        {
            //Arrange
            string expectedInfo = "Advance to Go (Collect £200)";

            //Act
            var communityChest = new CommunityChest(expectedInfo);

            //Assert
            Assert.Equal(expectedInfo, communityChest.Info);

        }


        [Fact]
        public void GetCommunityChestCardsReturnsValidCommunityChest()
        {
            //Arrange
            var communityChestCard = CardSet.GetCommunityChestCards();

            //Act and Assert
            foreach (var card in communityChestCard)
            {
                Assert.NotNull(card);
                Assert.NotNull(card.Info);
                Assert.IsType<CommunityChest>(card);
            }
        }
    }
}
