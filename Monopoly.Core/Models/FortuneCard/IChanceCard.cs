using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.FortuneCard
{
    public interface IFortuneCardView
    {
        string Info { get; }
    }

    internal interface IChanceCard : IFortuneCardView
    {
        void ExecuteEffect(Player player, Game game);
    }
}
