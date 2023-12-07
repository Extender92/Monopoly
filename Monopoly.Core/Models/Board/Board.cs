﻿using Monopoly.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class Board
    {
        private List<Square> Squares; // Array or list of all squares on the board

        public Board(GameRules gameRules)
        {
            Squares = SquareBuilder.GetBoardSquares(gameRules);
        }

        public void HandlePlayerLanding(Player player, int position)
        {
            Squares.First(s => s.Position == position).LandOn(player);
        }

        internal Square GetSquareAtPosition(int position)
        {
            return Squares.First(s => s.Position == position);
        }
    }
}
