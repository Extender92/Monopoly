using Monopoly.Console.GUI;
using Monopoly.Console.Models;
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
        public void Setup() 
        {
            int numberOfPlayers = 2;
            int numberOfDice = 2;
            int dieSides = 6;

            Core.Game Game = Core.GameSetup.Setup(numberOfPlayers, numberOfDice, dieSides);

            List<TablePiece> tablePieces = new List<TablePiece>();

            TablePieceInputManager tablePieceSelector = new(new ConsoleWrapper());

            

            foreach (Player player in Game.Players)
            {
                tablePieces.Add(tablePieceSelector.GetTablePieceFromUserInput(player.Id));
            }
        }
    }
}
