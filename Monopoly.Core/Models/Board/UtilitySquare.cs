using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class UtilitySquare : Square
    {
        public int Price { get; set; }  
        public UtilitySquare(int position , string info)
        {
            Position = position;
            Info = info;
            Price = 150;
        }
        public override void LandOn(Player player)
        {
            // Logic for when a player lands on
        }
    }
}
