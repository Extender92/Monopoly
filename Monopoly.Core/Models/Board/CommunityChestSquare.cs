using Monopoly.Core.Models.FortuneCard;
using Monopoly.Core.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    public class CommunityChestSquare : Square
    {
        public CommunityChestSquare(int position, string name, string info)
        {
            Position = position;
            Name = name;
            Info = info;
        }
        internal override void LandOn(Player player, Game game)
        {
            ICommunityChestCard communityChestCard = game.FortuneCard.DrawNextCommunityChestCard();
            game.PublishNotification(new CardDrawnNotification(communityChestCard, "event.secondary"));
            communityChestCard.ExecuteEffect(player, game);
        }
    }
}
