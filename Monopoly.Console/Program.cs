using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core.Models;
using System;

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
            //    info.Add("how do you");
            //    info.Add("how do you");
            //}

            //header = Helpers.StringHelper.CenterString(header, HorizontalSize);

            //GUI.Print.PrintCard(header, positionX, positionY, HorizontalSize, VerticalSize, info, ConsoleColor.Red, ConsoleColor.Blue);

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
            ConsoleGameSetup setup = new ConsoleGameSetup();
            setup.Setup();
            Run run = new Run(new ConsoleWrapper(), setup.TheGame, setup.TablePieces);
            run.RunGame();
        }
    }
}