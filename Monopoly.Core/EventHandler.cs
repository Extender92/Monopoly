using Monopoly.Core.Data;
using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    internal class EventHandler
    {

        /*
            Console.WriteLine($"Rent: {currentStreet.Rent}\n" +
            $"Rent with color set: {currentStreet.RentWithColor}\n" +
            $"Rent with color set: {currentStreet.RentWithColor}\n" +
            $"Rent (1 House): {currentStreet.RentOneHouses}\n" +
            $"Rent (2 Houses): {currentStreet.RentTwoHouses}\n" +
            $"Rent (3 Houses): {currentStreet.RentThreeHouses}\n" +
            $"Rent (4 Houses): {currentStreet.RentFourHouses}\n" +
            $"Rent (Hotel): {currentStreet.RentHotels}\n" +
            $"Houses Cost: {currentStreet.HousesCost}\n" +
            $"Hotels Cost : {currentStreet.HotelsCost}\n" +
            $"Price: {currentStreet.Price}\n" +
            $"Mortgage Value: {currentStreet.MortgageValue}")*/
        internal void HandleEvent(Player player)
        {
            List<Street> streets = CardSet.GetStreetCards();
            Street currentStreet = streets.FirstOrDefault(s => s.Position == player.Position);
            switch (player.Position)
            {
                case 0:
                    GoSpace goSpace = new GoSpace();
                    goSpace.GetGoSpace();
                    break;
                case 1:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 2:
                    CardSet.GetRandomCommunityChest();
                    break;
                case 3:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 4:
                    Tax tax = new Tax(4);
                    tax.GetTax();
                    break;
                case 5:
                    Railroad railroad = new Railroad(5);
                    railroad.GetRailroad();
                    break;
                case 6:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 7:
                    CardSet.GetRandomChance();
                    break;
                case 8:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 9:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 10:
                    Jail jail = new Jail();
                    jail.GetJail();
                    break;
                case 11:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 12:
                    // Electric ? 
                    Console.WriteLine("Electric");
                    break;
                case 13:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 14:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 15:
                    railroad = new Railroad(15);
                    railroad.GetRailroad();
                    break;
                case 16:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 17:
                    //CommintyChest
                    CardSet.GetCommunityChestCards();
                    break;
                case 18:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 19:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 20:
                    //Parking
                    ParkingSpace parkingSpace = new ParkingSpace();
                    parkingSpace.GetFreeParkingSpace();
                    break;
                case 21:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 22:
                    //Chance
                    CardSet.GetRandomChance();
                    break;
                case 23:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 24:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 25:
                    //Station
                    railroad = new Railroad(25);
                    railroad.GetRailroad();
                    break;
                case 26:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 27:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 28:
                    Console.WriteLine("Water");
                    break;
                case 29:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 30:
                    //Go To Jail
                    JailSpace jailSpace = new JailSpace();
                    jailSpace.GetJailSpace();
                    break;
                case 31:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 32:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 33:
                    CardSet.GetRandomCommunityChest();
                    break;
                case 34:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 35:
                    //Station
                    railroad = new Railroad(35);
                    railroad.GetRailroad();
                    break;
                case 36:
                    CardSet.GetRandomChance();
                    break;
                case 37:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
                case 38:
                    //Tax
                    tax = new Tax(38);
                    tax.GetTax();
                    break;
                case 39:
                    Console.WriteLine($"Player landed on {currentStreet.Name}");
                    break;
            }
        }
    }
}

