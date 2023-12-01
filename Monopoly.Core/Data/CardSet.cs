using System;
using System.Collections.Generic;
using System.Linq;
using Monopoly.Core.Models;

namespace Monopoly.Core.Data
{
    internal class CardSet
    {
        public static List<Street> GetStreetCards()
        {
            List<Street> cards =
            [
                new Street(ConsoleColor.Magenta, "Old Kent Road", 2, 4, 10, 30, 90, 160, 250, 50, 50, 60, 30),
                new Street(ConsoleColor.Magenta, "Whitechapel Road", 4, 8, 20, 60, 180, 320, 450, 50, 50, 60, 30),
                new Street(ConsoleColor.Cyan, "The Angel Islington", 6, 12, 30, 90, 270, 400, 550, 50, 50, 100, 50),
                new Street(ConsoleColor.Cyan, "Euston Road", 6, 12, 30, 90, 270, 400, 550, 50, 50, 100, 50),
                new Street(ConsoleColor.Cyan, "Pentonville Road", 8, 16, 40, 100, 300, 450, 600, 50, 50, 120, 60),
                new Street(ConsoleColor.DarkGreen, "Pall Mall", 10, 20, 50, 150, 450, 625, 750, 100, 100, 140, 70),
                new Street(ConsoleColor.DarkGreen, "Whitehall", 10, 20, 50, 150, 450, 625, 750, 100, 100, 140, 70),
                new Street(ConsoleColor.DarkGreen, "Northumberland Avenue", 12, 24, 60, 180, 500, 700, 900, 100, 100, 160, 80),
                new Street(ConsoleColor.DarkGray, "Bow Street", 14, 28, 70, 200, 550, 750, 950, 100, 100, 180, 90),
                new Street(ConsoleColor.DarkGray, "Marlborough Street", 14, 28, 70, 200, 550, 750, 950, 100, 100, 180, 90),
                new Street(ConsoleColor.DarkGray, "Vine Street", 16, 32, 80, 220, 600, 800, 1000, 100, 100, 200, 100),
                new Street(ConsoleColor.Red, "The Strand", 18, 36, 90, 250, 700, 875, 1050, 150, 150, 220, 110),
                new Street(ConsoleColor.Red, "Fleet Street", 18, 36, 90, 250, 700, 875, 1050, 150, 150, 220, 110),
                new Street(ConsoleColor.Red, "Trafalgar Square", 20, 40, 100, 300, 750, 925, 1100, 150, 150, 240, 120),
                new Street(ConsoleColor.Yellow, "Leicester Square", 22, 44, 110, 330, 800, 975, 1150, 150, 150, 260, 130),
                new Street(ConsoleColor.Yellow, "Coventry Street", 22, 44, 110, 330, 800, 975, 1150, 150, 150, 260, 130),
                new Street(ConsoleColor.Yellow, "Piccadilly", 24, 48, 120, 360, 850, 1025, 1200, 150, 150, 280, 150),
                new Street(ConsoleColor.Green, "Regent Street", 26, 52, 130, 390, 900, 1100, 1275, 200, 200, 300, 150),
                new Street(ConsoleColor.Green, "Oxford Street", 26, 52, 130, 390, 900, 1100, 1275, 200, 200, 300, 150),
                new Street(ConsoleColor.Green, "Bond Street", 28, 56, 150, 450, 1000, 1200, 1400, 200, 200, 320, 160),
                new Street(ConsoleColor.DarkBlue, "Park Lane", 35, 70, 175, 500, 1100, 1300, 1500, 200, 200, 350, 175),
                new Street(ConsoleColor.DarkBlue, "Mayfair", 50, 100, 200, 600, 1400, 1700, 2000, 200, 200, 400, 200),
            ];
            return cards;
        }

        public static List<Chance> GetChanceCards()
        {

            List<Chance> chances =
            [
                new Chance("Advance to Go (Collect £200)"),
                new Chance("Advance to Trafalgar Square. If you pass Go, collect £200"),
                new Chance("Advance to Mayfair"),
                new Chance("Advance to Pall Mall. If you pass Go, collect £20)"),
                new Chance("Advance to the nearest Station. If unowned, you may buy it from the Bank. If owned, pay wonder twice the rental to which they are otherwise entitled"),
                new Chance("Advance to the nearest Station. If unowned, you may buy it from the Bank. If owned, pay wonder twice the rental to which they are otherwise entitled"),
                new Chance("Advance token to nearest Utility.If unowned, you may buy it from the Bank.If owned, throw dice and pay owner a total ten times amount thrown."),
                new Chance("Bank pays you dividend of £50"),
                new Chance("Get Out of Jail Free"),
                new Chance("Go Back 3 Spaces"),
                new Chance("Go to Jail. Go directly to Jail, do not pass Go, do not collect £200"),
                new Chance("Make general repairs on all your property. For each house pay £25. For each hotel pay £100"),
                new Chance("Speeding fine £15"),
                new Chance("Take a trip to Kings Cross Station. If you pass Go, collect £200"),
                new Chance("You have been elected Chairman of the Board. Pay each player £50"),
                new Chance("Your building loan matures. Collect £150)"),
            ];

            return chances;

        }



        public static List<CommunityChest> GetCommunityChestCards()
        {
            List<CommunityChest> communityChests =
            [
                new CommunityChest("Advance to Go (Collect £200)"),
                new CommunityChest("Bank error in your favour. Collect £200"),
                new CommunityChest("Doctor’s fee. Pay £50"),
                new CommunityChest("From sale of stock you get £50"),
                new CommunityChest("Get Out of Jail Free"),
                new CommunityChest("Go to Jail. Go directly to jail, do not pass Go, do not collect £200"),
                new CommunityChest("Holiday fund matures. Receive £100"),
                new CommunityChest("Income tax refund. Collect £20"),
                new CommunityChest("It is your birthday. Collect £10 from every player"),
                new CommunityChest("Life insurance matures. Collect £100"),
                new CommunityChest("Pay hospital fees of £100"),
                new CommunityChest("Pay school fees of £50"),
                new CommunityChest("Receive £25 consultancy fee"),
                new CommunityChest("You are assessed for street repairs. £40 per house. £115 per hotel"),
                new CommunityChest("You have won second prize in a beauty contest. Collect £10"),
                new CommunityChest("You inherit £100"),
            ];

            return communityChests;
        }
    }
}
