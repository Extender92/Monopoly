using Monopoly.Core.Notifications;
using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

    internal class ChanceSquare : Square, IDeckReferenceSpace
{
    DeckId IDeckReferenceSpace.DeckId => LegacyStructureIds.PrimaryDeck;

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
        RuntimeCard card = game.CardHandler.DrawNextCard(LegacyStructureIds.PrimaryDeck);
        game.PublishNotification(new CardDrawnNotification(
            card,
            LegacyStructureIds.PrimaryDeck,
            PresentationTokens.PrimaryDeck));
        card.ExecuteEffect(player, game);
    }
}
