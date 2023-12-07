using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class RailroadSquare : Square
    {
        public int Price { get; set; }
        public RailroadSquare(int position, string info)
        {
            Position = position;
            Info = info;
            Price = 200;
        }


        public override void LandOn(Player player)
        {
            // Logic for when a player lands on a railroad square
        }
    }
}
