using Monopoly.Console.Models;
using Monopoly.Console.Models.Board;
using Monopoly.Core;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static Monopoly.Core.GameRules;

namespace Monopoly.Console.Builder
{
    internal class SquareCardBuilder
    {
        public static string Currency { get; set; }

        internal static List<SquareCard> BuildAllSquareCards(List<Square> squares, GameRules gameRules)
        {
            Currency = gameRules.CurrencySymbol;

            List<SquareCard> squareCardList = new List<SquareCard>();

            foreach (Square square in squares)
            {
                if (square is PropertySquare property)
                {
                    squareCardList.Add(BuildPropertyCardFromProperty(property));
                }
                else if (square is ChanceSquare chance)
                {
                    squareCardList.Add(BuildChanceCardFromChanceSquare(chance));
                }
                else if (square is CommunityChestSquare communityChest)
                {
                    squareCardList.Add(BuildCommunityChestCardFromCommunityChestSquare(communityChest));
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
                Info = $"Mortgage Value: {property.MortgageValue}{Currency}. Houses Cost {property.BuildHouseCost}{Currency} each. " +
                $"Hotels Cost {property.BuildHouseCost}{Currency} plus 4 houses"
            };

            return propertyCard;
        }

        private static ChanceSquareCard BuildChanceCardFromChanceSquare(ChanceSquare chance)
        {
            var chanceCard = new ChanceSquareCard
            {
                Name = chance.Name,
                BoardPosition = chance.Position,
                Info = chance.Info
            };

            return chanceCard;
        }

        private static CommunityChestSquareCard BuildCommunityChestCardFromCommunityChestSquare(CommunityChestSquare communityChest)
        {
            var communityChestCard = new CommunityChestSquareCard
            {
                Name = communityChest.Name,
                BoardPosition = communityChest.Position,
                Info = communityChest.Info
            };

            return communityChestCard;
        }

        private static GoSquareCard BuildGoCardFromGo(GoSquare go)
        {
            var goCard = new GoSquareCard
            {
                BoardPosition = go.Position,
                Name = go.Name,
                Info = $"Collect {Game.Rules.Salary}{Currency} salary as you pass {go.Name}"
            };

            return goCard;
        }

        private static GoToJailSquareCard BuildGoToJailCardFromGoToJail(GoToJailSquare goToJail)
        {
            var goToJailCard = new GoToJailSquareCard
            {
                Name = goToJail.Name,
                BoardPosition = goToJail.Position,
                Info = $"Go to jail!. Do not collect {Game.Rules.Salary}{Currency} salary for passing GO"
        };

            return goToJailCard;
        }

        private static JailSquareCard BuildJailCardFromJail(JailSquare jail)
        {
            var jailCard = new JailSquareCard
            {
                BoardPosition = jail.Position,
                Name = jail.Name,
                Info = jail.Info + " || " + jail.InJailInfo
            };

            return jailCard;
        }

        private static ParkingSquareCard BuildParkingCardFromParking(ParkingSquare parking)
        {
            string info = "";
            if (Game.Rules.FreeParking == Parking.SetFee)
            {
                info = $"Collect {(int)Parking.SetFee}{Currency}";
            }
            else if (Game.Rules.FreeParking == Parking.Fines)
            {
                info = $"Collect fines{Currency}";
            }
            var parkingCard = new ParkingSquareCard
            {
                Name = parking.Name,
                BoardPosition = parking.Position,
                Info = info
            };

            return parkingCard;
        }

        private static RailroadSquareCard BuildRailroadCardFromRailroad(RailroadSquare railroad)
        {
            var railroadCard = new RailroadSquareCard
            {
                Name = railroad.Name,
                BoardPosition = railroad.Position,
                Prop = new List<string>
                {
                    "Railroad Price",
                    "Rent",
                    "if 2 stations are owned",
                    "if 3 stations are owned",
                    "if 4 stations are owned"
                },
                Rent = new List<string>
                {
                    $"{railroad.Price}{Currency}",
                    $"{railroad.RentOneStation}{Currency}",
                    $"{railroad.RentTwoStation}{Currency}",
                    $"{railroad.RentThreeStation}{Currency}",
                    $"{railroad.RentFourStation}{Currency}"
                },
                Info = $"Mortgage Value: {railroad.MortgageValue}{Currency}"
            };

            return railroadCard;
        }

        private static TaxSquareCard BuildTaxCardFromTax(TaxSquare tax)
        {
            var taxCard = new TaxSquareCard
            {
                Name = tax.Name,
                BoardPosition = tax.Position,
                Info = $"Pay {tax.Price}{Currency}"
            };

            return taxCard;
        }

        private static UtilitySquareCard BuildUtilityCardFromUtility(UtilitySquare utility)
        {
            var utilityCard = new UtilitySquareCard
            {
                Name = utility.Name,
                BoardPosition = utility.Position,
                Info = $"If one utility is owned rent is {utility.RentOneUtility} times amount shown on dice. if both utilities are owned rent is" +
                $" {utility.RentTwoUtility} times amount shown on dice. Mortgage value: {utility.MortgageValue}{Currency}"
            };

            return utilityCard;
        }
    }
}
