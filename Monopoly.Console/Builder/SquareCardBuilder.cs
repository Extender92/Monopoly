using Monopoly.Console.Models;
using Monopoly.Console.Models.Board;
using Monopoly.Core;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.Builder
{
    internal class SquareCardBuilder
    {
        public static string Currency { get; set; }
        internal static List<SquareCard> BuildAllSquareCards(List<Square> squares, GameRules gameRules)
        {
            gameRules.CurrencySymbol = Currency;

            List<SquareCard> squareCardList = new List<SquareCard>();

            foreach (Square square in squares)
            {
                if (square is PropertySquare property)
                {
                    squareCardList.Add(BuildPropertyCardFromProperty(property));
                }
                else if (square is ChanceSquare chance)
                {
                    squareCardList.Add(BuildChanceCardFromChance(chance));
                }
                else if (square is CommunityChestSquare communityChest)
                {
                    squareCardList.Add(BuildCommunityChestCardFromCommunityChest(communityChest));
                }
                else if (square is GoSquare go)
                {
                    squareCardList.Add(BuildGoCardFromGo(go));
                }
                else if (square is GoToJailSquare goToJail)
                {
                    squareCardList.Add(BuildGoToJailCardFromGoToJail(goToJail));
                }
                else if (square is JailSquare jail)
                {
                    squareCardList.Add(BuildJailCardFromJail(jail));
                }
                else if (square is ParkingSquare parking)
                {
                    squareCardList.Add(BuildParkingCardFromParking(parking));
                }
                else if (square is RailroadSquare railroad)
                {
                    squareCardList.Add(BuildRailroadCardFromRailroad(railroad));
                }
                else if (square is TaxSquare tax)
                {
                    squareCardList.Add(BuildTaxCardFromTax(tax));
                }
                else if (square is UtilitySquare utility)
                {
                    squareCardList.Add(BuildUtilityCardFromUtility(utility));
                }
            }

            return squareCardList;
        }

        private static PropertySquareCard BuildPropertyCardFromProperty(PropertySquare property)
        {
            var propertyCard = new PropertySquareCard
            {
                Name = property.Name,
                BoardPosition = property.Position,
                BorderColor = property.Color,
                Prop = new List<string>
                {
                    "Property Price",
                    "Rent",
                    "Rent with color set",
                    "Rent with 1 house",
                    "Rent with 2 house",
                    "Rent with 3 house",
                    "Rent with 4 house",
                    "Rent with hotel",
                },
                Rent = new List<string>
                {
                    $"{property.Price}{Currency}",
                    $"{property.Rent}{Currency}",
                    $"{property.RentWithColorGroup}{Currency}",
                    $"{property.RentOneHouse}{Currency}",
                    $"{property.RentTwoHouses}{Currency}",
                    $"{property.RentThreeHouses}{Currency}",
                    $"{property.RentFourHouses}{Currency}",
                    $"{property.RentHotel}{Currency}",
                },
                Info = $"Mortgage Value {property.MortgageValue}{Currency}. Houses Cost {property.BuildHouseCost}{Currency} each. " +
                $"Hotels Cost {property.BuildHouseCost}{Currency} plus 4 houses"
            };

            return propertyCard;
        }

        private static ChanceSquareCard BuildChanceCardFromChance(ChanceSquare chance)
        {
            // Implement the Chance card builder logic here
            return new ChanceSquareCard();
        }

        private static CommunityChestSquareCard BuildCommunityChestCardFromCommunityChest(CommunityChestSquare communityChest)
        {
            // Implement the Community Chest card builder logic here
            return new CommunityChestSquareCard();
        }

        private static GoSquareCard BuildGoCardFromGo(GoSquare go)
        {
            // Implement the Go card builder logic here
            return new GoSquareCard();
        }

        private static GoToJailSquareCard BuildGoToJailCardFromGoToJail(GoToJailSquare goToJail)
        {
            // Implement the Go To Jail card builder logic here
            return new GoToJailSquareCard();
        }

        private static JailSquareCard BuildJailCardFromJail(JailSquare jail)
        {
            // Implement the Jail card builder logic here
            return new JailSquareCard();
        }

        private static ParkingSquareCard BuildParkingCardFromParking(ParkingSquare parking)
        {
            // Implement the Parking card builder logic here
            return new ParkingSquareCard();
        }

        private static RailroadSquareCard BuildRailroadCardFromRailroad(RailroadSquare railroad)
        {
            // Implement the Railroad card builder logic here
            return new RailroadSquareCard();
        }

        private static TaxSquareCard BuildTaxCardFromTax(TaxSquare tax)
        {
            // Implement the Tax card builder logic here
            return new TaxSquareCard();
        }

        private static UtilitySquareCard BuildUtilityCardFromUtility(UtilitySquare utility)
        {
            // Implement the Utility card builder logic here
            return new UtilitySquareCard();
        }
    }
}
