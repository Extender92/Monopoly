using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class ChanceSquare : Square
    {
        public ChanceSquare(int position)
        {
            Position = position;
            Info = "Chance";
        }
        public override void LandOn(Player player)
        {
            // Logic for when a player lands on a chance square
        }
    }
}
