using Monopoly.Core.Data;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models.FortuneCard;
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
            var street = new PropertySquare(color, name, rent, rentWithColor, rentOneHouse, rentTwoHouses,
                        rentThreeHouses, rentFourHouses, rentHotels, housesCost, hotelsCost,
                        price, mortgageValue, position);

            //Assert
            Assert.Equal(color, street.Color);
            Assert.Equal(name, street.Name);
            Assert.Equal(rent, street.Rent);
            Assert.Equal(rentWithColor, street.RentWithColorGroup);
            Assert.Equal(rentOneHouse, street.RentOneHouse);
            Assert.Equal(rentTwoHouses, street.RentTwoHouses);
            Assert.Equal(rentThreeHouses, street.RentThreeHouses);
            Assert.Equal(rentFourHouses, street.RentFourHouses);
            Assert.Equal(rentHotels, street.RentHotel);
            Assert.Equal(housesCost, street.BuildHouseCost);
            Assert.Equal(hotelsCost, street.BuildHotelCost);
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
            var street = new PropertySquare(color, name, rent, rentWithColor, rentOneHouse, rentTwoHouses,
                        rentThreeHouses, rentFourHouses, rentHotels, housesCost, hotelsCost,
                        price, mortgageValue, position);

            // Assert
            Assert.Equal(color, street.Color);
            Assert.Equal(name, street.Name);
            Assert.Equal(rent, street.Rent);
            Assert.Equal(rentWithColor, street.RentWithColorGroup);
            Assert.Equal(rentOneHouse, street.RentOneHouse);
            Assert.Equal(rentTwoHouses, street.RentTwoHouses);
            Assert.Equal(rentThreeHouses, street.RentThreeHouses);
            Assert.Equal(rentFourHouses, street.RentFourHouses);
            Assert.Equal(rentHotels, street.RentHotel);
            Assert.Equal(housesCost, street.BuildHouseCost);
            Assert.Equal(hotelsCost, street.BuildHotelCost);
            Assert.Equal(price, street.Price);
            Assert.Equal(mortgageValue, street.MortgageValue);
        }

        [Fact]
        public void GetStreetCardsReturnsCorrectNumberOfCards()
        {
            //Arrange
            var expectedNumberOfStreet = 22;
            var streetCards = Data.GetPropertySquareData(new Core.GameRules(0, 0, 0));

            //Act
            var numberOfCards = streetCards.Count;

            //Assert
            Assert.Equal(expectedNumberOfStreet, numberOfCards);
        }
        [Fact]
        public void GetStreetCardsReturnsValidStreet()
        {
            //Arrange
            var streetCards = Data.GetPropertySquareData(new Core.GameRules(0, 0, 0));

            //Act and Assert
            foreach (var card in streetCards)
            {
                Assert.NotNull(card);
                Assert.IsType<PropertySquare>(card);
                Assert.True(card.Rent > 0);
            }
        }

        [Fact]
        public void CanCreateOneChanceCard()
        {
            //Arrange
            string expectedInfo = "Get Out of Jail Free";
            UKChanceCard.UKChanceCardType type = UKChanceCard.UKChanceCardType.GetOutOfJailFree;

            //Act
            var chance = new UKChanceCard(expectedInfo, type);

            //Assert
            Assert.Equal(expectedInfo, chance.Info);

        }

        [Fact]
        public void GetChanceCardsReturnsValidChance()
        {
            // Arrange
            var gameRulesUK = new Core.GameRules(0, 0, 0) { GameLanguage = Core.GameRules.Language.UK };
            var gameRulesUS = new Core.GameRules(0, 0, 0) { GameLanguage = Core.GameRules.Language.US };

            // Act
            var chanceCardsUK = Data.GetChanceCardData(gameRulesUK);
            var chanceCardsUS = Data.GetChanceCardData(gameRulesUS);

            // Assert
            foreach (var card in chanceCardsUK)
            {
                Assert.NotNull(card);
                Assert.NotNull(card.Info);
                Assert.IsType<UKChanceCard>(card); 
            }

            foreach (var card in chanceCardsUS)
            {
                Assert.NotNull(card);
                Assert.NotNull(card.Info);
                Assert.IsType<USChanceCard>(card); 
            }
        }
        [Fact]
        public void CanCreateOneCommunityChestCard()
        {
            //Arrange
            string expectedInfo = "Advance to Go (Collect £200)";

            //Act
            var communityChest = new UKCommunityChestCard(expectedInfo, UKCommunityChestCard.UKCommunityChestCardType.AdvanceToGo);

            //Assert
            Assert.Equal(expectedInfo, communityChest.Info);

        }


        [Fact]
        public void GetCommunityChestCardsReturnsValidCommunityChest()
        {
            // Arrange
            var gameRulesUK = new Core.GameRules(0, 0, 0) { GameLanguage = Core.GameRules.Language.UK };
            var gameRulesUS = new Core.GameRules(0, 0, 0) { GameLanguage = Core.GameRules.Language.US };

            // Act
            var communityChestCardsUK = Data.GetCommunityChestCardData(gameRulesUK);
            var communityChestCardsUS = Data.GetCommunityChestCardData(gameRulesUS);

            // Assert
            foreach (var card in communityChestCardsUK)
            {
                Assert.NotNull(card);
                Assert.NotNull(card.Info);
                Assert.IsType<UKCommunityChestCard>(card); // Update the type here
            }

            foreach (var card in communityChestCardsUS)
            {
                Assert.NotNull(card);
                Assert.NotNull(card.Info);
                Assert.IsType<USCommunityChestCard>(card); // Update the type here
            }
        }
    }
}
