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

        public ConsoleGameSetup()
        {
            _tablePieceSelector = new TablePieceInputManager(new ConsoleWrapper());
        }

        public void Setup() 
        {
            int numberOfDice = 2;
            int dieSides = 6;

            System.Console.WriteLine("How many players?");
            List<string> choices = Helpers.StringHelper.CreateStringList("1", "2", "3", "4", "5", "6", "7", "8");
            int index = MenuOptionSelector.GetSelectedOption(choices);
            int numberOfPlayers = index + 1;

            GameRules gameRules = new GameRules(numberOfPlayers, numberOfDice, dieSides);
            TheGame = CoreGameSetup.Setup(gameRules);

            TablePieces = new();
            foreach (Player player in TheGame.Players)
            {
                TablePieces.Add(_tablePieceSelector.GetTablePieceFromUserInput(player.Id, TablePieces));
            }
        }
    }
}
