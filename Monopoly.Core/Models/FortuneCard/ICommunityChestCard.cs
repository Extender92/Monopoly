using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.FortuneCard
{
    internal interface ICommunityChestCard : IFortuneCardView
    {
        void ExecuteEffect(Player player, Game game);
    }
}
