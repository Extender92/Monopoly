using Monopoly.Core.Models.FortuneCard;
using Monopoly.Core.Notifications;
using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

public class ChanceSquare : Square
{
    public ChanceSquare(int position, PresentationMetadata presentation)
        : base(position, presentation)
    {
    }

    internal ChanceSquare(int position, string name, string info)
        : this(position, LegacyPresentationFactory.Space(position, name, info))
    {
    }

    internal override void LandOn(Player player, Game game)
    {
        IChanceCard card = game.FortuneCard.DrawNextChanceCard();
        game.PublishNotification(new CardDrawnNotification(card, PresentationTokens.PrimaryDeck));
        card.ExecuteEffect(player, game);
    }
}
