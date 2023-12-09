using Monopoly.Core.Models.FortuneCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class CommunityChestSquare : Square
    {
        public CommunityChestSquare(int position, string info)
        {
            Position = position;
            Info = info;
        }
        public override void LandOn(Player player)
        {
            ICommunityChestCard communityChestCard = Game.FortuneCard.DrawNextCommunityChestCard();
            communityChestCard.ExecuteEffect(player);
        }
    }
}
