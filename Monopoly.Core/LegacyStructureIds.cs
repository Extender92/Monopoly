namespace Monopoly.Core;

internal static class LegacyStructureIds
{
    internal static DeckId PrimaryDeck { get; } = new("legacy.deck.primary");
    internal static DeckId SecondaryDeck { get; } = new("legacy.deck.secondary");

    internal static SpaceId Space(int position) => new($"legacy.space-{position}");

    internal static CardId Card(DeckId deckId, int ordinal) =>
        new($"{deckId.Value}.card-{ordinal}");
}
