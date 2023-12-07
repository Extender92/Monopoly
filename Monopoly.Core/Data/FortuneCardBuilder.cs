using Monopoly.Core.Models.FortuneCard;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Data
{
    internal class FortuneCardBuilder
    {
        internal static List<IChanceCard> GetChanceCards(GameRules gameRules)
        {
            return Data.GetChanceCardData(gameRules);
        }

        internal static List<ICommunityChestCard> GetCommunityChestCards(GameRules gameRules)
        {
            return Data.GetCommunityChestCardData(gameRules);
        }
    }
}
