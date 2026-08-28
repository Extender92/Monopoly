using Monopoly.Core.Models.FortuneCard;
using Monopoly.Core.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    public class ChanceSquare : Square
    {
        public ChanceSquare(int position, string name, string info)
        {
            Position = position;
            Name = name;
            Info = info;
        }
        internal override void LandOn(Player player, Game game)
        {
            IChanceCard chanceCard = game.FortuneCard.DrawNextChanceCard();
            game.PublishNotification(new CardDrawnNotification(chanceCard, "event.primary"));
            chanceCard.ExecuteEffect(player, game);
        }
    }
}
