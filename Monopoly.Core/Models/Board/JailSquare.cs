using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class JailSquare : Square
    {
        public JailSquare()
        {
            Position = 10;
            Info = "Visiting Jail";
        }


        public override void LandOn(Player player)
        {
            // Logic for when a player lands on
        }
    }
}
