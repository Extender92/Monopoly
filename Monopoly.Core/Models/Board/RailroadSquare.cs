using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class RailroadSquare : Square
    {
        public RailroadSquare(int position)
        {
            Position = position;
            Info = "Railroad";
        }


        public override void LandOn(Player player)
        {
            // Logic for when a player lands on a railroad square
        }
    }
}
