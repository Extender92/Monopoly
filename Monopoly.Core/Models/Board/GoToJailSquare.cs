using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class GoToJailSquare : Square
    {
        public GoToJailSquare()
        {
            Position = 30;
            Info = "Go To Jail";
        }

        public override void LandOn(Player player)
        {
            // Logic for when a player lands on
        }
    }
}
