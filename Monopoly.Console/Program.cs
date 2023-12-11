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
            int numberOfDice = 2;
            int dieSides = 6;
            System.Console.WriteLine("How many players?");
            List<string> menuChoices = Helpers.StringHelper.CreateStringList("2", "3", "4", "5", "6", "7", "8");
            int index = MenuOptionSelector.GetSelectedOption(menuChoices, menuChoices.Max(s => s.Length), 0, 3);
            int numberOfPlayers = index + 2;

            GameRules gameRules = new GameRules(numberOfPlayers, numberOfDice, dieSides);
            ConsoleGameSetup gameSetup = new ConsoleGameSetup(gameRules);
            gameSetup.Setup();
            Run run = new Run(gameSetup.TablePieces);

            run.RunGame();
        }

        public static void TestCards()
        {
            while (true)
            {

                foreach (var landedSquare in Game.Board.Squares)
                {
                    System.Console.Clear();
                    if (landedSquare is PropertySquare)
                    {
                        ConsolePrinter.PrepareAndPrintPropertyCard(landedSquare.Position);
                    }
                    else
                    {
                        ConsolePrinter.PrepareAndPrintSquareCard(landedSquare.Position);
                    }
                    System.Console.ReadLine();
                }
            }
        }

        public static void PrintColors()
        {
            // Get an array with the values of ConsoleColor enumeration members.
            ConsoleColor[] colors = (ConsoleColor[])ConsoleColor.GetValues(typeof(ConsoleColor));
            // Save the current background and foreground colors.
            ConsoleColor currentBackground = System.Console.BackgroundColor;
            ConsoleColor currentForeground = System.Console.ForegroundColor;

            // Display all foreground colors except the one that matches the background.
            System.Console.WriteLine("All the foreground colors except {0}, the background color:",
                              currentBackground);
            foreach (var color in colors)
            {
                if (color == currentBackground) continue;

                System.Console.ForegroundColor = color;
                System.Console.WriteLine("   The foreground color is {0}.", color);
            }
            System.Console.WriteLine();
            // Restore the foreground color.
            System.Console.ForegroundColor = currentForeground;

            
            // Restore the original console colors.
            System.Console.ResetColor();
            System.Console.WriteLine("\nOriginal colors restored...");
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

                GUI.ConsolePrinter.PrintCard(chanceHeader, chanceHorizontalSize, chanceVerticalSize, chanceInfo, ConsoleColor.Green, ConsoleColor.Yellow);

                System.Console.ReadLine();

                // Clear the chanceInfo list for the next iteration
                chanceInfo.Clear();
            }

        }
    }
}