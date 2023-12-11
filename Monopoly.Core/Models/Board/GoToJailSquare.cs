using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class GoToJailSquare : Square
    {
        public GoToJailSquare(int position, string name, string info)
        {
            Position = position;
            Name = name;
            Info = info;
        }

        public override void LandOn(Player player)
        {
            Game.Jail.PlayerGoToJail(player);
        }
    }
}
