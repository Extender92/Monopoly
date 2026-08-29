using Monopoly.Core.Models.FortuneCard;
using Monopoly.Core.Notifications;
using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

public class CommunityChestSquare : Square
{
    public CommunityChestSquare(int position, PresentationMetadata presentation)
        : base(position, presentation)
    {
    }

    internal CommunityChestSquare(int position, string name, string info)
        : this(position, LegacyPresentationFactory.Space(position, name, info))
    {
    }

    internal override void LandOn(Player player, Game game)
    {
        ICommunityChestCard card = game.FortuneCard.DrawNextCommunityChestCard();
        game.PublishNotification(new CardDrawnNotification(card, PresentationTokens.SecondaryDeck));
        card.ExecuteEffect(player, game);
    }
}
