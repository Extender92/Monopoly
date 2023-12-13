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
    internal class ConsoleGameSetup
    {
        private readonly TablePieceInputManager _tablePieceSelector;
        internal List<TablePiece> TablePieces {  get; set; }
        internal GameRules GameRules { get; set; }

        public ConsoleGameSetup(GameRules gameRules, TablePieceInputManager tablePieceInputManager)
        {
            _tablePieceSelector = tablePieceInputManager;
            GameRules = gameRules;
        }

        public ConsoleGame Setup(Game game, IConsoleWrapper consoleWrapper, ConsolePrinter consolePrinter, Input input, ConsoleLogPrinter logPrint, ConsoleCardPrinter cardPrinter) 
        {
            CoreGameSetup.Setup(GameRules);

            IMenuOptionSelector menu = new MenuOptionSelector(consoleWrapper);
            menu.SetPositions();


            TablePieces = new();
            foreach (Player player in game.Players)
            {
                TablePieces.Add(_tablePieceSelector.GetTablePieceFromUserInput(player.Id, TablePieces, input));
            }

            ConsoleGame consoleGame = new ConsoleGame(game, consolePrinter, TablePieces, input, logPrint, cardPrinter);
            return consoleGame;
        }
    }
}
