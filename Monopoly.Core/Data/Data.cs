using System;
using System.Collections.Generic;
using System.Linq;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models.FortuneCard;

namespace Monopoly.Core.Data
{
    internal class Data
    {
        public static List<PropertySquare> GetPropertySquareData(GameRules gameRules)
        {
            List<PropertySquare> data = new List<PropertySquare>();

            if (gameRules.GameLanguage == GameRules.Language.UK)
            {
                data = new List<PropertySquare> {
                    new PropertySquare(ConsoleColor.DarkYellow, "Old Kent Road", 2, 4, 10, 30, 90, 160, 250, 50, 50, 60, 30, 1),
                    new PropertySquare(ConsoleColor.DarkYellow, "Whitechapel Road", 4, 8, 20, 60, 180, 320, 450, 50, 50, 60, 30, 3),
                    new PropertySquare(ConsoleColor.Cyan, "The Angel Islington", 6, 12, 30, 90, 270, 400, 550, 50, 50, 100, 50, 6),
                    new PropertySquare(ConsoleColor.Cyan, "Euston Road", 6, 12, 30, 90, 270, 400, 550, 50, 50, 100, 50, 8),
                    new PropertySquare(ConsoleColor.Cyan, "Pentonville Road", 8, 16, 40, 100, 300, 450, 600, 50, 50, 120, 60, 9),
                    new PropertySquare(ConsoleColor.Magenta, "Pall Mall", 10, 20, 50, 150, 450, 625, 750, 100, 100, 140, 70, 11),
                    new PropertySquare(ConsoleColor.Magenta, "Whitehall", 10, 20, 50, 150, 450, 625, 750, 100, 100, 140, 70, 13),
                    new PropertySquare(ConsoleColor.Magenta, "Northumberland Avenue", 12, 24, 60, 180, 500, 700, 900, 100, 100, 160, 80, 14),
                    new PropertySquare(ConsoleColor.DarkYellow, "Bow Street", 14, 28, 70, 200, 550, 750, 950, 100, 100, 180, 90, 16),
                    new PropertySquare(ConsoleColor.DarkYellow, "Marlborough Street", 14, 28, 70, 200, 550, 750, 950, 100, 100, 180, 90, 18),
                    new PropertySquare(ConsoleColor.DarkYellow, "Vine Street", 16, 32, 80, 220, 600, 800, 1000, 100, 100, 200, 100, 19),
                    new PropertySquare(ConsoleColor.Red, "The Strand", 18, 36, 90, 250, 700, 875, 1050, 150, 150, 220, 110, 21),
                    new PropertySquare(ConsoleColor.Red, "Fleet Street", 18, 36, 90, 250, 700, 875, 1050, 150, 150, 220, 110, 23),
                    new PropertySquare(ConsoleColor.Red, "Trafalgar Square", 20, 40, 100, 300, 750, 925, 1100, 150, 150, 240, 120, 24),
                    new PropertySquare(ConsoleColor.Yellow, "Leicester Square", 22, 44, 110, 330, 800, 975, 1150, 150, 150, 260, 130, 26),
                    new PropertySquare(ConsoleColor.Yellow, "Coventry Street", 22, 44, 110, 330, 800, 975, 1150, 150, 150, 260, 130, 27),
                    new PropertySquare(ConsoleColor.Yellow, "Piccadilly", 24, 48, 120, 360, 850, 1025, 1200, 150, 150, 280, 150, 29),
                    new PropertySquare(ConsoleColor.Green, "Regent Street", 26, 52, 130, 390, 900, 1100, 1275, 200, 200, 300, 150, 31),
                    new PropertySquare(ConsoleColor.Green, "Oxford Street", 26, 52, 130, 390, 900, 1100, 1275, 200, 200, 300, 150, 32),
                    new PropertySquare(ConsoleColor.Green, "Bond Street", 28, 56, 150, 450, 1000, 1200, 1400, 200, 200, 320, 160, 34),
                    new PropertySquare(ConsoleColor.DarkBlue, "Park Lane", 35, 70, 175, 500, 1100, 1300, 1500, 200, 200, 350, 175, 37),
                    new PropertySquare(ConsoleColor.DarkBlue, "Mayfair", 50, 100, 200, 600, 1400, 1700, 2000, 200, 200, 400, 200, 39),
                };
            }
            else if (gameRules.GameLanguage == GameRules.Language.US)
            {

            }
            else
            {
                throw new ArgumentException("Unsupported language");
            }

            return data;
        }

        public static List<IChanceCard> GetChanceCardData(GameRules gameRules)
        {
            List<IChanceCard> data = new List<IChanceCard>();

            if (gameRules.GameLanguage == GameRules.Language.UK)
            {
                data = new List<IChanceCard> {
                    new UKChanceCard("Advance to Go (Collect £200)", UKChanceCard.UKChanceCardType.AdvanceToGo),
                    new UKChanceCard("Advance to Trafalgar Square. If you pass Go, collect £200", UKChanceCard.UKChanceCardType.AdvanceToTrafalgarSquare),
                    new UKChanceCard("Advance to Mayfair", UKChanceCard.UKChanceCardType.AdvanceToMayfair),
                    new UKChanceCard("Advance to Pall Mall. If you pass Go, collect £200", UKChanceCard.UKChanceCardType.AdvanceToPallMall),
                    new UKChanceCard("Advance to the nearest Station. If unowned, you may buy it from the Bank. If owned, pay wonder twice the rental to which they are otherwise entitled", UKChanceCard.UKChanceCardType.AdvanceToNearestStation),
                    new UKChanceCard("Advance token to nearest Utility. If unowned, you may buy it from the Bank. If owned, throw dice and pay owner a total ten times amount thrown.", UKChanceCard.UKChanceCardType.AdvanceTokenToNearestUtility),
                    new UKChanceCard("Bank pays you dividend of £50", UKChanceCard.UKChanceCardType.BankPaysDividend),
                    new UKChanceCard("Get Out of Jail Free", UKChanceCard.UKChanceCardType.GetOutOfJailFree),
                    new UKChanceCard("Go Back 3 Spaces", UKChanceCard.UKChanceCardType.GoBack3Spaces),
                    new UKChanceCard("Go to Jail. Go directly to Jail, do not pass Go, do not collect £200", UKChanceCard.UKChanceCardType.GoToJail),
                    new UKChanceCard("Make general repairs on all your property. For each house pay £25. For each hotel pay £100", UKChanceCard.UKChanceCardType.MakeGeneralRepairs),
                    new UKChanceCard("Speeding fine £15", UKChanceCard.UKChanceCardType.SpeedingFine),
                    new UKChanceCard("Take a trip to Kings Cross Station. If you pass Go, collect £200", UKChanceCard.UKChanceCardType.TakeTripToKingsCrossStation),
                    new UKChanceCard("You have been elected Chairman of the Board. Pay each player £50", UKChanceCard.UKChanceCardType.ElectedChairmanOfTheBoard),
                    new UKChanceCard("Your building loan matures. Collect £150", UKChanceCard.UKChanceCardType.BuildingLoanMatures),
                };
            }
            else if (gameRules.GameLanguage == GameRules.Language.US)
            {
                data = new List<IChanceCard> {
                    new USChanceCard("Advance to Boardwalk", USChanceCard.USChanceCardType.AdvanceToBoardwalk),
                    new USChanceCard("Advance to Go (Collect $200)", USChanceCard.USChanceCardType.AdvanceToGo),
                    new USChanceCard("Advance to Illinois Avenue. If you pass Go, collect $200", USChanceCard.USChanceCardType.AdvanceToIllinoisAvenue),
                    new USChanceCard("Advance to St. Charles Place. If you pass Go, collect $200", USChanceCard.USChanceCardType.AdvanceToStCharlesPlace),
                    new USChanceCard("Advance to the nearest Railroad. If unowned, you may buy it from the Bank. If owned, pay owner twice the rental to which they are otherwise entitled", USChanceCard.USChanceCardType.AdvanceToNearestRailroad),
                    new USChanceCard("Advance token to nearest Utility. If unowned, you may buy it from the Bank. If owned, throw dice and pay owner a total ten times the amount thrown", USChanceCard.USChanceCardType.AdvanceToNearestUtility),
                    new USChanceCard("Bank pays you a dividend of $50", USChanceCard.USChanceCardType.BankPaysDividend),
                    new USChanceCard("Get Out of Jail Free", USChanceCard.USChanceCardType.GetOutOfJailFree),
                    new USChanceCard("Go Back 3 Spaces", USChanceCard.USChanceCardType.GoBack3Spaces),
                    new USChanceCard("Go to Jail. Go directly to Jail, do not pass Go, do not collect $200", USChanceCard.USChanceCardType.GoToJail),
                    new USChanceCard("Make general repairs on all your property. For each house, pay $25. For each hotel, pay $100", USChanceCard.USChanceCardType.MakeGeneralRepairs),
                    new USChanceCard("Speeding fine $15", USChanceCard.USChanceCardType.SpeedingFine),
                    new USChanceCard("Take a trip to Reading Railroad. If you pass Go, collect $200", USChanceCard.USChanceCardType.TakeTripToReadingRailroad),
                    new USChanceCard("You have been elected Chairman of the Board. Pay each player $50", USChanceCard.USChanceCardType.ElectedChairmanOfTheBoard),
                    new USChanceCard("Your building loan matures. Collect $150", USChanceCard.USChanceCardType.BuildingLoanMatures),
                };

            }
            else
            {
                throw new ArgumentException("Unsupported language");
            }

            return data;
        }

        public static List<ICommunityChestCard> GetCommunityChestCardData(GameRules gameRules)
        {
            List<ICommunityChestCard> data = new List<ICommunityChestCard>();

            if (gameRules.GameLanguage == GameRules.Language.UK)
            {
                data = new List<ICommunityChestCard> {
                    new UKCommunityChestCard("Advance to Go (Collect £200)", UKCommunityChestCard.UKCommunityChestCardType.AdvanceToGo),
                    new UKCommunityChestCard("Bank error in your favour. Collect £200", UKCommunityChestCard.UKCommunityChestCardType.BankErrorInYourFavour),
                    new UKCommunityChestCard("Doctor’s fee. Pay £50", UKCommunityChestCard.UKCommunityChestCardType.DoctorsFee),
                    new UKCommunityChestCard("From sale of stock you get £50", UKCommunityChestCard.UKCommunityChestCardType.FromSaleOfStock),
                    new UKCommunityChestCard("Get Out of Jail Free", UKCommunityChestCard.UKCommunityChestCardType.GetOutOfJailFree),
                    new UKCommunityChestCard("Go to Jail. Go directly to jail, do not pass Go, do not collect £200", UKCommunityChestCard.UKCommunityChestCardType.GoToJail),
                    new UKCommunityChestCard("Holiday fund matures. Receive £100", UKCommunityChestCard.UKCommunityChestCardType.HolidayFundMatures),
                    new UKCommunityChestCard("Income tax refund. Collect £20", UKCommunityChestCard.UKCommunityChestCardType.IncomeTaxRefund),
                    new UKCommunityChestCard("It is your birthday. Collect £10 from every player", UKCommunityChestCard.UKCommunityChestCardType.ItIsYourBirthday),
                    new UKCommunityChestCard("Life insurance matures. Collect £100", UKCommunityChestCard.UKCommunityChestCardType.LifeInsuranceMatures),
                    new UKCommunityChestCard("Pay hospital fees of £100", UKCommunityChestCard.UKCommunityChestCardType.PayHospitalFees),
                    new UKCommunityChestCard("Pay school fees of £50", UKCommunityChestCard.UKCommunityChestCardType.PaySchoolFees),
                    new UKCommunityChestCard("Receive £25 consultancy fee", UKCommunityChestCard.UKCommunityChestCardType.ReceiveConsultancyFee),
                    new UKCommunityChestCard("You are assessed for street repairs. £40 per house. £115 per hotel", UKCommunityChestCard.UKCommunityChestCardType.AssessedForStreetRepairs),
                    new UKCommunityChestCard("You have won second prize in a beauty contest. Collect £10", UKCommunityChestCard.UKCommunityChestCardType.WonSecondPrizeInBeautyContest),
                    new UKCommunityChestCard("You inherit £100", UKCommunityChestCard.UKCommunityChestCardType.Inherit),
                };

            }
            else if (gameRules.GameLanguage == GameRules.Language.US)
            {
                data = new List<ICommunityChestCard> {
                    new USCommunityChestCard("Advance to Go (Collect $200)", USCommunityChestCard.USCommunityChestCardType.AdvanceToGo),
                    new USCommunityChestCard("Bank error in your favor. Collect $200", USCommunityChestCard.USCommunityChestCardType.BankErrorInYourFavour),
                    new USCommunityChestCard("Doctor’s fee. Pay $50", USCommunityChestCard.USCommunityChestCardType.DoctorsFee),
                    new USCommunityChestCard("From sale of stock you get $50", USCommunityChestCard.USCommunityChestCardType.FromSaleOfStock),
                    new USCommunityChestCard("Get Out of Jail Free", USCommunityChestCard.USCommunityChestCardType.GetOutOfJailFree),
                    new USCommunityChestCard("Go to Jail. Go directly to jail, do not pass Go, do not collect $200", USCommunityChestCard.USCommunityChestCardType.GoToJail),
                    new USCommunityChestCard("Holiday fund matures. Receive $100", USCommunityChestCard.USCommunityChestCardType.HolidayFundMatures),
                    new USCommunityChestCard("Income tax refund. Collect $20", USCommunityChestCard.USCommunityChestCardType.IncomeTaxRefund),
                    new USCommunityChestCard("It is your birthday. Collect $10 from every player", USCommunityChestCard.USCommunityChestCardType.ItIsYourBirthday),
                    new USCommunityChestCard("Life insurance matures. Collect $100", USCommunityChestCard.USCommunityChestCardType.LifeInsuranceMatures),
                    new USCommunityChestCard("Pay hospital fees of $100", USCommunityChestCard.USCommunityChestCardType.PayHospitalFees),
                    new USCommunityChestCard("Pay school fees of $50", USCommunityChestCard.USCommunityChestCardType.PaySchoolFees),
                    new USCommunityChestCard("Receive $25 consultancy fee", USCommunityChestCard.USCommunityChestCardType.ReceiveConsultancyFee),
                    new USCommunityChestCard("You are assessed for street repair. $40 per house. $115 per hotel", USCommunityChestCard.USCommunityChestCardType.AssessedForStreetRepairs),
                    new USCommunityChestCard("You have won second prize in a beauty contest. Collect $10", USCommunityChestCard.USCommunityChestCardType.WonSecondPrizeInBeautyContest),
                    new USCommunityChestCard("You inherit $100", USCommunityChestCard.USCommunityChestCardType.Inherit),
                };

            }
            else
            {
                throw new ArgumentException("Unsupported language");
            }

            return data;
        }


        internal static List<ChanceSquare> GetChanceSquareData(GameRules gameRules)
        {
            List<ChanceSquare> chanceSquares = new List<ChanceSquare>();

            string info;

            if (gameRules.GameLanguage == GameRules.Language.UK || gameRules.GameLanguage == GameRules.Language.US)
            {
                info = "Chance Card";
                chanceSquares.Add(new ChanceSquare(7, info));
                chanceSquares.Add(new ChanceSquare(22, info));
                chanceSquares.Add(new ChanceSquare(36, info));
            }
            else
            {
                throw new ArgumentException("Unsupported language");
            }

            return chanceSquares;
        }

        internal static List<CommunityChestSquare> GetCommunityChestSquareData(GameRules gameRules)
        {
            List<CommunityChestSquare> communityChestSquares = new List<CommunityChestSquare>();

            string info;

            if (gameRules.GameLanguage == GameRules.Language.UK || gameRules.GameLanguage == GameRules.Language.US)
            {
                info = "CommunityChest Card";
                communityChestSquares.Add(new CommunityChestSquare(2, info));
                communityChestSquares.Add(new CommunityChestSquare(17, info));
                communityChestSquares.Add(new CommunityChestSquare(33, info));
            }
            else
            {
                throw new ArgumentException("Unsupported language");
            }

            return communityChestSquares;
        }

        internal static List<GoSquare> GetGoSquareData(GameRules gameRules)
        {
            List<GoSquare> goSquares = new List<GoSquare>();

            int position = 0;
            string info;

            if (gameRules.GameLanguage == GameRules.Language.UK || gameRules.GameLanguage == GameRules.Language.US)
            {
                info = "Go Square";
                goSquares.Add(new GoSquare(position, info));
            }
            else
            {
                throw new ArgumentException("Unsupported language");
            }

            return goSquares;
        }

        internal static List<GoToJailSquare> GetGoToJailSquareData(GameRules gameRules)
        {
            List<GoToJailSquare> goToJailSquares = new List<GoToJailSquare>();

            int position = 30;
            string info;

            if (gameRules.GameLanguage == GameRules.Language.UK || gameRules.GameLanguage == GameRules.Language.US)
            {
                info = "Go to jail square";
                goToJailSquares.Add(new GoToJailSquare(position, info));
            }
            else
            {
                throw new ArgumentException("Unsupported language");
            }

            return goToJailSquares;
        }

        internal static List<JailSquare> GetJailSquareData(GameRules gameRules)
        {
            List<JailSquare> jailSquares = new List<JailSquare>();

            int Position = 10;
            string info;
            string InJail;

            if (gameRules.GameLanguage == GameRules.Language.UK || gameRules.GameLanguage == GameRules.Language.US)
            {
                info = "Visiting Jail";
                InJail = "In Jail";
                jailSquares.Add(new JailSquare(Position, info, InJail));
            }
            else
            {
                throw new ArgumentException("Unsupported language");
            }

            return jailSquares;
        }

        internal static List<ParkingSquare> GetParkingSquareData(GameRules gameRules)
        {
            List<ParkingSquare> parkingSquares = new List<ParkingSquare>();

            int position = 20;
            string info;

            if (gameRules.GameLanguage == GameRules.Language.UK || gameRules.GameLanguage == GameRules.Language.US)
            {
                info = "Free Parking";
                parkingSquares.Add(new ParkingSquare(position, info));
            }
            else
            {
                throw new ArgumentException("Unsupported language");
            }

            return parkingSquares;
        }

        internal static List<RailroadSquare> GetRailroadSquareData(GameRules gameRules)
        {
            List<RailroadSquare> railroadSquares = new List<RailroadSquare>();

            int price = 200;
            int mortgageValue = price / 2;
            int rentOneStation = 25;
            int rentTwoStation = 50;
            int rentThreeStation = 100;
            int rentFourStation = 200;

            if (gameRules.GameLanguage == GameRules.Language.UK)
            {
                railroadSquares.Add(new RailroadSquare(5, "Kings Cross Station", price, rentOneStation, rentTwoStation, rentThreeStation, rentFourStation, mortgageValue));
                railroadSquares.Add(new RailroadSquare(15, "Marylebone Station", price, rentOneStation, rentTwoStation, rentThreeStation, rentFourStation, mortgageValue));
                railroadSquares.Add(new RailroadSquare(25, "Fenchurch Street Station", price, rentOneStation, rentTwoStation, rentThreeStation, rentFourStation, mortgageValue));
                railroadSquares.Add(new RailroadSquare(35, "Liverpool Street Station", price, rentOneStation, rentTwoStation, rentThreeStation, rentFourStation, mortgageValue));
            }
            else if(gameRules.GameLanguage == GameRules.Language.US)
            {
                railroadSquares.Add(new RailroadSquare(5, "Reading Railroad", price, rentOneStation, rentTwoStation, rentThreeStation, rentFourStation, mortgageValue));
                railroadSquares.Add(new RailroadSquare(15, "Pennsylvania Railroad", price, rentOneStation, rentTwoStation, rentThreeStation, rentFourStation, mortgageValue));
                railroadSquares.Add(new RailroadSquare(25, "B&O Railroad", price, rentOneStation, rentTwoStation, rentThreeStation, rentFourStation, mortgageValue));
                railroadSquares.Add(new RailroadSquare(35, "Short Line", price, rentOneStation, rentTwoStation, rentThreeStation, rentFourStation, mortgageValue));
            }
            else
            {
                throw new ArgumentException("Unsupported language");
            }

            return railroadSquares;
        }

        internal static List<TaxSquare> GetTaxSquareData(GameRules gameRules)
        {
            List<TaxSquare> taxSquares = new List<TaxSquare>();

            if (gameRules.GameLanguage == GameRules.Language.UK || gameRules.GameLanguage == GameRules.Language.US)
            {
                taxSquares.Add(new TaxSquare(4, 200, "Income Tax"));
                taxSquares.Add(new TaxSquare(38, 100, "Luxury Tax"));
            }
            else
            {
                throw new ArgumentException("Unsupported language");
            }

            return taxSquares;
        }

        internal static List<UtilitySquare> GetUtilitySquareData(GameRules gameRules)
        {
            List<UtilitySquare> utilitySquares = new List<UtilitySquare>();

            int price = 150;
            int mortgageValue = price / 2;
            int rentOneUtility = 4;
            int rentTwoUtility = 10;

            if (gameRules.GameLanguage == GameRules.Language.UK || gameRules.GameLanguage == GameRules.Language.US)
            {
                utilitySquares.Add(new UtilitySquare(12, "Electric Company", price, rentOneUtility, rentTwoUtility, mortgageValue));
                utilitySquares.Add(new UtilitySquare(27, "Water Works", price, rentOneUtility, rentTwoUtility, mortgageValue));
            }
            else
            {
                throw new ArgumentException("Unsupported language");
            }

            return utilitySquares;
        }

    }
}
