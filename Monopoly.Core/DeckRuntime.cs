using Monopoly.Core.Presentation;
using Monopoly.Core.Randomness;

namespace Monopoly.Core;

internal sealed class DeckRuntime
{
    private readonly Dictionary<DeckId, RuntimeDeck> _decks;

    private DeckRuntime(IEnumerable<RuntimeDeck> decks) =>
        _decks = decks.ToDictionary(deck => deck.Id);

    internal static DeckRuntime CreateForProfile(
        IEnumerable<DeckDefinition> definitions,
        MatchRandomizer randomizer,
        bool shuffleDecks)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(randomizer);

        DeckDefinition[] ordered = definitions.OrderBy(definition => definition.Id).ToArray();
        int sequenceIndex = 0;
        List<RuntimeDeck> decks = [];
        foreach (DeckDefinition definition in ordered)
        {
            CardDefinition[] cards = definition.Cards.ToArray();
            if (shuffleDecks)
            {
                for (int index = cards.Length - 1; index > 0; index--)
                {
                    int selected = randomizer.NextInt(new RandomRequest(
                        RandomPurpose.DeckShuffle,
                        0,
                        index + 1,
                        sequenceIndex++));
                    (cards[index], cards[selected]) = (cards[selected], cards[index]);
                }
            }

            decks.Add(new RuntimeDeck(definition.Id, definition.PresentationToken, cards));
        }

        return new DeckRuntime(decks);
    }

    internal DeckCollection CreateSnapshot() =>
        new(_decks.Values.OrderBy(deck => deck.Id).Select(deck => deck.CreateView()));

    internal Dictionary<DeckId, List<CardDefinition>> CaptureOrders() =>
        _decks.ToDictionary(entry => entry.Key, entry => entry.Value.Cards.ToList());

    internal void ValidateOrders(IReadOnlyDictionary<DeckId, List<CardDefinition>> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);
        if (!_decks.Keys.ToHashSet().SetEquals(orders.Keys))
            throw new InvalidOperationException("The prepared deck state does not match the match decks.");

        foreach ((DeckId id, RuntimeDeck deck) in _decks)
        {
            if (!orders.TryGetValue(id, out List<CardDefinition>? replacement) ||
                replacement is null ||
                replacement.Any(card => card is null) ||
                replacement.Count != deck.Cards.Count ||
                replacement.Select(card => card.Id).Distinct().Count() != replacement.Count ||
                !replacement.Select(card => card.Id).ToHashSet().SetEquals(deck.Cards.Select(card => card.Id)))
            {
                throw new InvalidOperationException($"The prepared order for deck '{id}' is invalid.");
            }
        }
    }

    internal void ApplyOrders(IReadOnlyDictionary<DeckId, List<CardDefinition>> orders)
    {
        ValidateOrders(orders);
        foreach ((DeckId id, List<CardDefinition> cards) in orders)
            _decks[id].Replace(cards);
    }
}

internal sealed class RuntimeDeck
{
    private List<CardDefinition> _cards;

    internal RuntimeDeck(DeckId id, PresentationToken presentationToken, IEnumerable<CardDefinition> cards)
    {
        Id = id;
        PresentationToken = presentationToken;
        _cards = cards.ToList();
        if (_cards.Count == 0) throw new ArgumentException("A runtime deck cannot be empty.", nameof(cards));
    }

    internal DeckId Id { get; }
    internal PresentationToken PresentationToken { get; }
    internal IReadOnlyList<CardDefinition> Cards => _cards;

    internal void Replace(IEnumerable<CardDefinition> cards)
    {
        CardDefinition[] replacement = cards.ToArray();
        if (replacement.Length == 0) throw new ArgumentException("A runtime deck cannot be empty.", nameof(cards));
        _cards = replacement.ToList();
    }

    internal DeckView CreateView() => new(
        Id,
        PresentationToken,
        _cards.Select(card => (ICardView)new CardView(card.Id, card.PresentationToken)));
}
