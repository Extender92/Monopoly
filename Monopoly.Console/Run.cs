using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core;
using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console
{
    internal class Run
    {
        private readonly IConsoleWrapper _console;

        private Game Game { get; set; }

        public Run(IConsoleWrapper consoleWrapper,)
        {
            _console = consoleWrapper;
            Game = new Game(dice, players, rules);
        }

        internal static void RunGame()
        {
            IConsoleWrapper _console = new ConsoleWrapper();

            ConsoleGameSetup setup = new ConsoleGameSetup();

            System.Console.Clear();
            while (true)
            {
                foreach (var player in Game.Players)
                {
                    ConsolePrinter.PrintGameBoard(Game.Players, tablePieces);
                    _console.SetPosition(0, 0);
                    _console.WriteLine(player.Name + "'s Turn");
                    _console.WriteLine("Press Enter To Roll Dice");
                    _console.ReadLine();
                    Game.PlayerTurn(player);
                    _console.Clear();
                }
            }
        }
    }
}
