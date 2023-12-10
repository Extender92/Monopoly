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
            while (true)
            {
                foreach (var player in Game.Players)
                {
                    ConsolePrinter.DisplayPlayerTurnAndWaitForInput(player);
                    Game.NextPlayerTakeTurn(player);

                    _console.Clear();
                    ConsolePrinter.PrintGameBoard(Game.Players, TablePieces);

                    Square landedSquare = Game.Board.GetSquareAtPosition(player.Position);
                    player.LandOnSquare(landedSquare);
                }
            }
        }
    }
}
