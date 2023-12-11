using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Runtime.InteropServices;

namespace Monopoly.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IConsoleWrapper consoleWrapper = new ConsoleWrapper();
            GameRules gameRules = SetupRules(consoleWrapper);
            ConsoleGameSetup gameSetup = new ConsoleGameSetup(gameRules);
            gameSetup.Setup(consoleWrapper);
            ConsoleGame.StartGame();
        }

        private static GameRules SetupRules(IConsoleWrapper consoleWrapper)
        {
            MenuOptionSelector menu = new MenuOptionSelector(consoleWrapper);
            Input input = new Input(consoleWrapper, menu);
            int numberOfDice = 2;
            int dieSides = 6;
            int numberOfPlayers = input.GetNumberOfPlayers();
            return new GameRules(numberOfPlayers, numberOfDice, dieSides);
        }

        public void TestCards()
        {
            while (true)
            {

                foreach (var landedSquare in Game.Board.Squares)
                {
                    System.Console.Clear();
                    ConsoleGame.Printer.PrepareAndPrintSquareCard(landedSquare.Position);
                    System.Console.ReadLine();
                }
            }
        }

        public static void DrawChanceCards(GameRules rules)
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

                ConsoleGame.Printer.PrintCard(chanceHeader, chanceHorizontalSize, chanceVerticalSize, chanceInfo, ConsoleColor.Green, ConsoleColor.Yellow);

                System.Console.ReadLine();

                // Clear the chanceInfo list for the next iteration
                chanceInfo.Clear();
            }
        }
    }
}