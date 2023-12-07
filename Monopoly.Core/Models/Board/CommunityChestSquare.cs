using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class CommunityChestSquare : Square
    {
        public CommunityChestSquare(int position)
        {
            Position = position;
            Info = "Community Chest";
        }
        public override void LandOn(Player player)
        {
            // Logic for when a player lands on
        }
    }
}
