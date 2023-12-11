using Monopoly.Core.Models.FortuneCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class ChanceSquare : Square
    {
        public ChanceSquare(int position, string name, string info)
        {
            Position = position;
            Name = name;
            Info = info;
        }
        public override void LandOn(Player player)
        {
            IChanceCard chanceCard = Game.FortuneCard.DrawNextChanceCard();
            chanceCard.ExecuteEffect(player);
        }
    }
}
