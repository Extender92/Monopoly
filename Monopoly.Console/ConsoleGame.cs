using Monopoly.Console.Events;
using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console
{
    internal class ConsoleGame
    {
        internal static ConsolePrinter Printer { get; } = new ConsolePrinter(new ConsoleWrapper());

        internal static List<TablePiece> TablePieces { get; set; }
        internal static Input PlayerInput { get; set; }


        internal static void StartGame()
        {
            ConsoleEventHandler.SubscribeToEvents();

            System.Console.Clear();
            Printer.PrintGameBoard(TablePieces);

            while (Game.Players.Count(p => !p.IsBankrupt) > 1)
            {
                foreach (var player in Game.Players.Where(p => !p.IsBankrupt))
                {
                    do
                    {
                        Printer.WaitForInputToStartTurn(player);

                        if (player.InJail)
                        {
                            Game.TheJail.TakeTurnInJail(player);
                        }
                        else
                        {
                            Game.RoleDiceAndMovePlayer(player);
                        }

                        Square landedSquare = Game.Board.GetSquareAtPosition(player.Position);

                        UpdateGameInformation(landedSquare, player);

                        if (!player.IsBankrupt)
                        {
                            player.LandOnSquare(landedSquare);

                            UpdateGameInformation(landedSquare, player);

                            // Check for a double roll
                            if (Game.IsDiceDouble())
                            {
                                Printer.PrintText($"{player.Name} rolled a double! Taking another turn.");
                            }
                        }

                        Printer.WaitForInputToEndTurn(player);
                    } while (Game.IsDiceDouble() && !player.IsBankrupt);
                }
            }
            Game.Winning(Game.Players.First(p => !p.IsBankrupt));
        }

        private static void UpdateGameInformation(Square landedSquare, Player player)
        {
            Printer.Console.Clear();
            Printer.PrintGameBoard(TablePieces);
            Printer.DisplayPlayersInformation(player);
            Printer.PrepareAndPrintSquareCard(landedSquare.Position);
            Printer.PrintNewestLogs(10);
        }
    }
}
