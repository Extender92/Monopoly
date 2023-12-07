//using Monopoly.Core.Data;
//using Monopoly.Core.Models;
//using Monopoly.Core.Models.Board;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Monopoly.Core
//{
//    internal class EventHandler
//    {

//        /*
//            Console.WriteLine($"Rent: {currentStreet.Rent}\n" +
//            $"Rent with color set: {currentStreet.RentWithColor}\n" +
//            $"Rent with color set: {currentStreet.RentWithColor}\n" +
//            $"Rent (1 House): {currentStreet.RentOneHouses}\n" +
//            $"Rent (2 Houses): {currentStreet.RentTwoHouses}\n" +
//            $"Rent (3 Houses): {currentStreet.RentThreeHouses}\n" +
//            $"Rent (4 Houses): {currentStreet.RentFourHouses}\n" +
//            $"Rent (Hotel): {currentStreet.RentHotels}\n" +
//            $"Houses Cost: {currentStreet.HousesCost}\n" +
//            $"Hotels Cost : {currentStreet.HotelsCost}\n" +
//            $"Price: {currentStreet.Price}\n" +
//            $"Mortgage Value: {currentStreet.MortgageValue}")*/
//        GameRules GameRules {  get; set; }
//        public EventHandler(GameRules gameRules)
//        {
//            GameRules = gameRules;
//        }

//        internal void HandleEvent(Events events)
//        {
//            switch (events.Id)
//            {
//                // Go Past Go
//                case 10:
//                    events.Player.Money += 200;
//                    break;

//                // On Board
//                // On Go
//                case 100:
//                    //if (GameRules.DoubleOnGo) { events.Player.Money += 200; }
//                    break;
//                // On Street
//                case 101:
//                    //HandlePlayerOnStreet(events.Player);
//                    break;
//                // On RailRoad
//                case 102:
//                    //HandlePlayerOnRailroad(events.Player)
//                    break;
//                // On Utility
//                case 103:
//                    //HandlePlayerOnUtility(events.Player)
//                    break;
//                // On Tax
//                case 104:
//                    //HandlePlayerOnTax(events.Player)
//                    break;
//                // On Go To Jail
//                case 105:
//                    //events.id
//                    break;
//                // On CommunityChest
//                case 106:
//                    //Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                // On Chance
//                case 107:
//                    CardSet.GetRandomChance();
//                    break;
//                case 108:
//                    //Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 109:
//                    //Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 110:
//                    JailSquare jail = new JailSquare();
//                    //jail.GetJail();
//                    break;
//                case 111:
//                    //Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 112:
//                    // Electric ? 
//                    Console.WriteLine("Electric");
//                    break;
//                case 113:
//                    //Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 114:
//                    //Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 115:
//                    railroad = new RailRoadSquare(15);
//                    railroad.GetRailroad();
//                    break;
//                case 116:
//                    Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 117:
//                    CardSet.GetCommunityChestCards();
//                    break;
//                case 118:
//                    Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 19:
//                    Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 120:
//                    //Parking
//                    ParkingSquare parkingSpace = new ParkingSquare();
//                    parkingSpace.GetFreeParkingSpace();
//                    break;
//                case 121:
//                    Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 122:
//                    CardSet.GetRandomChance();
//                    break;
//                case 123:
//                    Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 24:
//                    Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 25:
//                    //Station
//                    railroad = new RailRoadSquare(25);
//                    railroad.GetRailroad();
//                    break;
//                case 26:
//                    Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 27:
//                    Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 28:
//                    Console.WriteLine("Water");
//                    break;
//                case 29:
//                    Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 30:
//                    //Go To Jail
//                    GoToJailSquare jailSpace = new GoToJailSquare();
//                    jailSpace.GetJailSpace();
//                    break;
//                case 31:
//                    Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 32:
//                    Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 33:
//                    CardSet.GetRandomCommunityChest();
//                    break;
//                case 34:
//                    Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 35:
//                    //Station
//                    railroad = new RailRoadSquare(35);
//                    railroad.GetRailroad();
//                    break;
//                case 36:
//                    CardSet.GetRandomChance();
//                    break;
//                case 37:
//                    Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//                case 38:
//                    //Tax
//                    tax = new TaxSquare(38);
//                    tax.GetTax();
//                    break;
//                case 39:
//                    Console.WriteLine($"Player landed on {currentStreet.Name}");
//                    break;
//            }
//            events.IsFinished = true;
//        }
//    }
//}

