using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core;
using Monopoly.Core.Models;
using System;
using System.Runtime.InteropServices;

namespace Monopoly.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {


            //List<string> infoLines = new List<string>();
            //infoLines.Add("info");
            //infoLines.Add("info again info");
            //infoLines.Add("info again info again info");
            //infoLines.Add("info again info");
            //infoLines.Add("På info");

            //List<string> rents = new List<string>();
            //rents.Add("rent");
            //rents.Add("rent");
            //rents.Add("rent again rent");
            //rents.Add("rent again rent");
            //rents.Add("På rent");

            //List<string> info = new List<string>();

            //int infoTextLength = infoLines.Select((line, i) => line.Length + rents[i].Length + 4).Max();

            //string header = "train station";

            //int positionX = 5;
            //int positionY = 5;

            //int HorizontalSize = 30;
            //int VerticalSize = 9;

            //HorizontalSize = Math.Max(HorizontalSize, Math.Max(header.Length + 2, infoTextLength));

            //for (int i = 0; i < infoLines.Count; i++)
            //{
            //    int space = HorizontalSize - (infoLines[i].Length + rents[i].Length + 2);
            //    info.Add(infoLines[i] + ":".PadRight(space) + rents[i]);
            //    //info.Add("how do you");
            //    //info.Add("how do you");
            //}

            //header = Helpers.StringHelper.CenterString(header, HorizontalSize);

            //GUI.ConsolePrinter.PrintCard(header, positionX, positionY, HorizontalSize, VerticalSize, info, ConsoleColor.Red, ConsoleColor.Blue);

            //System.Console.ReadLine();




            //info.Clear();

            //for (int i = 0; i < infoLines.Count; i++)
            //{
            //    int space = HorizontalSize - (infoLines[i].Length + rents[i].Length + 2);
            //    info.Add(rents[i] + ":".PadRight(space) + infoLines[i]);
            //}

            //GUI.Print.PrintCard(header, positionX, positionY, HorizontalSize, VerticalSize, info, ConsoleColor.Green, ConsoleColor.Yellow);

            //System.Console.ReadLine();


            //System.Console.Clear();
            //info.Clear();

            //string infoText = "very lång text Based på Train station för att testa how Code handle Line break!";

            //HorizontalSize = 30;
            //header = Helpers.StringHelper.CenterString(header, HorizontalSize);
            //int length = HorizontalSize - 1;

            //info = Helpers.StringHelper.CenterStringInList(Helpers.StringHelper.GetListOfStringsFromString(infoText, length), length);

            //GUI.Print.PrintCard(header, positionX, positionY, HorizontalSize, VerticalSize, info, ConsoleColor.Green, ConsoleColor.Yellow);

            //foreach (var card in CardSet.GetStreetCards())
            //{
            //    List<string> cardInfo = new()
            //    {
            //          $"Rent: {card.Rent}",
            //          $"Rent with color set: {card.RentWithColor}",
            //          $"Rent (1 House): {card.RentOneHouses}",
            //          $"Rent (2 Houses): {card.RentTwoHouses}",
            //          $"Rent (3 Houses): {card.RentThreeHouses}",
            //          $"Rent (4 Houses): {card.RentFourHouses}",
            //          $"Rent (Hotel): {card.RentHotels}",
            //          $"Houses Cost: {card.HousesCost}",
            //          $"Hotels Cost : {card.HotelsCost}",
            //          $"Price: {card.Price}",
            //          $"Mortgage Value: {card.MortgageValue}"
            //    };
            //    Print.PrintCard(card.UKName, 2, 3, 25, 10, cardInfo, card.Color, System.ConsoleColor.White);
            //    System.Console.ReadKey();

            //}



            int numberOfDice = 2;
            int dieSides = 6;
            int numberOfPlayers = 1;
            GameRules gameRules = new GameRules(numberOfPlayers, numberOfDice, dieSides);
            DrawPropertyCards(gameRules);
            System.Console.WriteLine("How many players?");
            List<string> choices = Helpers.StringHelper.CreateStringList("1", "2", "3", "4", "5", "6", "7", "8");
            int index = MenuOptionSelector.GetSelectedOption(choices);
            //int numberOfPlayers = index + 1;

            //GameRules gameRules = new GameRules(numberOfPlayers, numberOfDice, dieSides);
            ConsoleGameSetup gameSetup = new ConsoleGameSetup(gameRules);
            gameSetup.Setup();
            Run run = new Run(gameSetup.TheGame, gameSetup.TablePieces);
            run.RunGame();
        }

        public static void DrawPropertyCards(GameRules rules)
        {

            List<string> infoLines = new List<string>();
            infoLines.Add("Rent");
            infoLines.Add("Rent with color set");
            infoLines.Add("Rent (1 House)");
            infoLines.Add("Rent (2 Houses)");
            infoLines.Add("Rent (3 Houses)");
            infoLines.Add("Rent (4 Houses)");
            infoLines.Add("Rent (Hotel)");
            infoLines.Add("Houses Cost");
            infoLines.Add("Hotels Cost");
            infoLines.Add("Price");
            infoLines.Add("Mortgage Value");

            List<string> info = new List<string>();

            int infoTextLength = infoLines.Max(line => line.Length) + 4;

            int positionX = 85;
            int positionY = 5;

            int HorizontalSize = 30;
            int VerticalSize = 9;

            foreach (var card in Core.Data.Data.GetPropertySquareData(rules))
            {
                List<string> rents = new List<string>
                {
                    $"{card.Rent}",
                    $"{card.RentWithColor}",
                    $"{card.RentOneHouses}",
                    $"{card.RentTwoHouses}",
                    $"{card.RentThreeHouses}",
                    $"{card.RentFourHouses}",
                    $"{card.RentHotels}",
                    $"{card.HousesCost}",
                    $"{card.HotelsCost}",
                    $"{card.HotelsCost}",
                    $"{card.Price}",
                    $"{card.MortgageValue}"
                };

                string header = card.Name;

                HorizontalSize = Math.Max(HorizontalSize, Math.Max(header.Length + 2, infoTextLength));

                for (int i = 0; i < infoLines.Count; i++)
                {
                    int space = HorizontalSize - (infoLines[i].Length + rents[i].Length + 2);
                    info.Add(infoLines[i] + ":".PadRight(space) + rents[i]);
                }

                header = Helpers.StringHelper.CenterString(header, HorizontalSize);

                GUI.ConsolePrinter.PrintCard(header, positionX, positionY, HorizontalSize, VerticalSize, info, card.Color, ConsoleColor.White);

                System.Console.ReadLine();

                info.Clear();
            }

        }

        public static void DrawChancetCards(GameRules rules)
        {
            List<string> chanceInfoLines = new List<string>
            {
                "Action",
                "Description",
            };

            int chanceInfoTextLength = chanceInfoLines.Max(line => line.Length) + 4;

            int chancePositionX = 5;
            int chancePositionY = 5;

            int chanceHorizontalSize = 40;
            int chanceVerticalSize = 5;

            foreach (var chanceCard in Core.Data.Data.GetChanceCardData(rules))
            {
                List<string> chanceInfo = new List<string>
                {
                    $"{chanceCard.Info}",
                    $"{chanceCard.GetType}"
                };

                string chanceHeader = chanceCard.Info;

                chanceHorizontalSize = Math.Max(chanceHorizontalSize, Math.Max(chanceHeader.Length + 2, chanceInfoTextLength));

                for (int i = 0; i < chanceInfoLines.Count; i++)
                {
                    int space = chanceHorizontalSize - (chanceInfoLines[i].Length + chanceInfo[i].Length + 2);
                    chanceInfo[i] = chanceInfoLines[i] + ":".PadRight(space) + chanceInfo[i];
                }

                chanceHeader = Helpers.StringHelper.CenterString(chanceHeader, chanceHorizontalSize);

                GUI.ConsolePrinter.PrintCard(chanceHeader, chancePositionX, chancePositionY, chanceHorizontalSize, chanceVerticalSize, chanceInfo, ConsoleColor.Green, ConsoleColor.Yellow);

                System.Console.ReadLine();

                // Clear the chanceInfo list for the next iteration
                chanceInfo.Clear();
            }

        }
    }
}