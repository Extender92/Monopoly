using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core;
using Monopoly.Core.Models;
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
            while (true)
            {
                foreach (var player in Game.Players)
                {
                    ConsolePrinter.PrintGameBoard(Game.Players, TablePieces);
                    _console.SetPosition(0, 0);
                    _console.WriteLine(player.Name + "'s Turn");
                    _console.WriteLine("Press Enter To Roll Dice");
                    _console.ReadLine();
                    Game.PlayerTurn(player);
                    ConsolePrinter.PrintGameBoard(Game.Players, TablePieces);
                    ConsolePrinter.PrintGameBoard(Game.Players, TablePieces); // PrintCard
                    Core.EventHandler eventHandler = new Core.EventHandler();
                    eventHandler.HandleEvent(player);
                    //_console.ReadKey();
                    //_console.Clear();

                }
            }
        }
    }
}
