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
        internal Game TheGame { get; set; }
        internal GameRules GameRules { get; set; }

        public ConsoleGameSetup(GameRules gameRules)
        {
            _tablePieceSelector = new TablePieceInputManager();
            GameRules = gameRules;
        }

        public void Setup() 
        {
            TheGame = CoreGameSetup.Setup(GameRules);

            TablePieces = new();
            foreach (Player player in TheGame.Players)
            {
                TablePieces.Add(_tablePieceSelector.GetTablePieceFromUserInput(player.Id, TablePieces));
            }
        }
    }
}
