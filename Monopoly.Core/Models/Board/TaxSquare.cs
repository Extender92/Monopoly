using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class TaxSquare : Square
    {
        public TaxSquare(int position)
        {
            Position = position;
            Info = "Tax";
        }

        public override void LandOn(Player player)
        {
            // Logic for when a player lands on
        }
    }
}
