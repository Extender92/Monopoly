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
    internal class Run
    {
        private readonly IConsoleWrapper _console;

        internal List<TablePiece> TablePieces { get; set; }

        public Run(List<TablePiece> tablePieces)
        {
            _console = new ConsoleWrapper();
            TablePieces = tablePieces;

        }

        internal void RunGame()
        {
            ConsoleEventHandler.SubscribeToEvents();

            System.Console.Clear();
            ConsolePrinter.PrintGameBoard(TablePieces);

            while (Game.Players.Count(p => !p.IsBankrupt) > 1)
            {
                foreach (var player in Game.Players.Where(p => !p.IsBankrupt))
                {
                    ConsolePrinter.WaitForInput(player);
                    Game.NextPlayerTakeTurn(player);
                    Square landedSquare = Game.Board.GetSquareAtPosition(player.Position);

                    Printer(landedSquare, player);

                    if (!player.IsBankrupt)
                    {
                        player.LandOnSquare(landedSquare);

                        Printer(landedSquare, player);
                    }
                }
            }
            Game.Winning(Game.Players.First(p => !p.IsBankrupt));
        }

        private void Printer(Square landedSquare, Player player)
        {
            _console.Clear();
            ConsolePrinter.PrintGameBoard(TablePieces);
            ConsolePrinter.DisplayPlayersInformation(player);
            ConsolePrinter.PrepareAndPrintSquareCard(landedSquare.Position);
            ConsolePrinter.PrintNewestLogs(10);
        }
    }
}
