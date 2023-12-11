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

        private List<TablePiece> TablePieces { get; set; }

        public Run(List<TablePiece> tablePieces)
        {
            _console = new ConsoleWrapper();
            TablePieces = tablePieces;

        }

        internal void RunGame()
        {
            ConsoleEventHandler.SubscribeToEvents();

            System.Console.Clear();
            ConsolePrinter.PrintGameBoard(Game.Players, TablePieces);

            while (Game.Players.Count(p => !p.IsBankrupt) > 1)
            {
                foreach (var player in Game.Players.Where(p => !p.IsBankrupt))
                {
                    ConsolePrinter.WaitForInput(player);
                    Game.NextPlayerTakeTurn(player);
                    Square landedSquare = Game.Board.GetSquareAtPosition(player.Position);

                    _console.Clear();
                    ConsolePrinter.PrintGameBoard(Game.Players, TablePieces);
                    ConsolePrinter.DisplayPlayersInformation(Game.Players);
                    PrintCard(landedSquare);
                    ConsolePrinter.PrintNewestLogs(Game.Logs.LogList, 10);

                    if (!player.IsBankrupt)
                    {
                        player.LandOnSquare(landedSquare);

                        _console.Clear();
                        ConsolePrinter.PrintGameBoard(Game.Players, TablePieces);
                        ConsolePrinter.DisplayPlayersInformation(Game.Players);
                        PrintCard(landedSquare);
                        ConsolePrinter.PrintNewestLogs(Game.Logs.LogList, 10);
                    }
                }
            }
            Game.Winning(Game.Players.First(p => !p.IsBankrupt));
        }

        private void PrintCard(Square landedSquare)
        {
            if (landedSquare is PropertySquare)
            {
                ConsolePrinter.PrepareAndPrintPropertyCard(landedSquare.Position);
            }
            else
            {
                ConsolePrinter.PrepareAndPrintSquareCard(landedSquare.Position);
            }
        }
    }
}
