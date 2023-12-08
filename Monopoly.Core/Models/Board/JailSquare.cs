using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class JailSquare : Square
    {
        public string InJail {  get; set; }
        public JailSquare(int position, string info, string InJail)
        {
            Position = position;
            Info = info;
            InJail = InJail;
        }


        public override void LandOn(Player player)
        {
            // Logic for when a player lands on
        }
    }
}
