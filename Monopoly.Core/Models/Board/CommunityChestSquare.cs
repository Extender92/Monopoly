using Monopoly.Core.Notifications;
using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

    internal class CommunityChestSquare : Square, IDeckReferenceSpace
{
    DeckId IDeckReferenceSpace.DeckId => LegacyStructureIds.SecondaryDeck;

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
        RuntimeCard card = game.CardHandler.DrawNextCard(LegacyStructureIds.SecondaryDeck);
        game.PublishNotification(new CardDrawnNotification(
            card,
            LegacyStructureIds.SecondaryDeck,
            PresentationTokens.SecondaryDeck));
        card.ExecuteEffect(player, game);
    }
}
