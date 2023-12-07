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

        private Game Game { get; set; }
        private List<TablePiece> TablePieces { get; set; }

        public Run(Game game, List<TablePiece> tablePieces)
        {
            _console = new ConsoleWrapper();
            Game = game;
            TablePieces = tablePieces;
        }

        internal void RunGame()
        {
            System.Console.Clear();
            ConsolePrinter.PrintGameBoard(Game.Players, TablePieces);
            while (true)
            {
                foreach (var player in Game.Players)
                {
                    ConsolePrinter.DisplayPlayerTurn(player);
                    Game.PlayerTurn(player);

                    //ConsolePrinter.PrintGameBoard(Game.Players, TablePieces);
                    //Core.EventHandler eventHandler = new Core.EventHandler();
                    //eventHandler.HandleEvent(player);


                    Square landedSquare = Game.Board.GetSquareAtPosition(player.Position);

                    player.LandOnSquare(landedSquare);

                    //_console.ReadKey();
                    _console.Clear();
                    ConsolePrinter.PrintGameBoard(Game.Players, TablePieces);
                }
            }
        }
    }
}
