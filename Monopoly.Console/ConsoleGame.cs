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
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console
{
    internal class ConsoleGame
    {
        private readonly IConsoleWrapper _console;

        internal List<TablePiece> TablePieces { get; set; }

        public ConsoleGame(List<TablePiece> tablePieces)
        {
            _console = new ConsoleWrapper();
            TablePieces = tablePieces;
        }

        internal void StartGame()
        {
            ConsoleEventHandler.SubscribeToEvents();

            System.Console.Clear();
            ConsolePrinter.PrintGameBoard(TablePieces);

            while (Game.Players.Count(p => !p.IsBankrupt) > 1)
            {
                foreach (var player in Game.Players.Where(p => !p.IsBankrupt))
                {
                    do
                    {
                        ConsolePrinter.WaitForInputToStartTurn(player);

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
                                ConsolePrinter.PrintText($"{player.Name} rolled a double! Taking another turn.");
                            }
                        }

                        ConsolePrinter.WaitForInputToEndTurn(player);
                    } while (Game.IsDiceDouble() && !player.IsBankrupt);
                }
            }
            Game.Winning(Game.Players.First(p => !p.IsBankrupt));
        }

        private void UpdateGameInformation(Square landedSquare, Player player)
        {
            _console.Clear();
            ConsolePrinter.PrintGameBoard(TablePieces);
            ConsolePrinter.DisplayPlayersInformation(player);
            ConsolePrinter.PrepareAndPrintSquareCard(landedSquare.Position);
            ConsolePrinter.PrintNewestLogs(10);
        }
    }
}
