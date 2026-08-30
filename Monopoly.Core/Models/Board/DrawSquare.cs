using Monopoly.Core.Notifications;
using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

internal sealed class DrawSquare : Square, IDeckReferenceSpace
{
    internal DrawSquare(int position, PresentationMetadata presentation, DeckId deckId)
        : base(position, presentation)
    {
        if (!deckId.IsValid) throw new ArgumentException("The deck ID is invalid.", nameof(deckId));
        DeckId = deckId;
    }

    public DeckId DeckId { get; }

    internal override void LandOn(Player player, Game game)
    {
        RuntimeCard card = game.DeckRuntime.DrawNextCard(DeckId);
        PresentationToken deckToken = game.Decks.Resolve(DeckId).PresentationToken;
        game.PublishNotification(new CardDrawnNotification(card, DeckId, deckToken));
        card.ExecuteEffect(player, game);
    }
}
