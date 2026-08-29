using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.FortuneCard
{
    internal interface ILegacyCard
    {
        Monopoly.Core.Presentation.PresentationMetadata Presentation { get; }
        Monopoly.Core.Presentation.PresentationToken PresentationToken { get; }
        void ExecuteEffect(Player player, Game game);
    }

    internal interface IChanceCard : ILegacyCard
    {
    }
}
