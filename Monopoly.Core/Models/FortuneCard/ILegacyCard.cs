using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.FortuneCard;

internal interface ILegacyCard
{
    PresentationToken PresentationToken { get; }
    void ExecuteEffect(Player player, Game game);
}
