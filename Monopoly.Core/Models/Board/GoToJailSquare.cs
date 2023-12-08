using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class GoToJailSquare : Square
    {
        public GoToJailSquare(int position, string info)
        {
            Position = position;
            Info = info;
        }

        public override void LandOn(Player player)
        {
            // Logic for when a player lands on
        }
    }
}
