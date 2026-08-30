using Monopoly.Core.Models;
using Monopoly.Core.Models.FortuneCard;
using Monopoly.Core.Presentation;
using Monopoly.Core.Randomness;

namespace Monopoly.Core;

internal sealed class DeckRuntime
{
    private readonly Dictionary<DeckId, RuntimeDeck> _decks;

    internal DeckRuntime(
        IEnumerable<RuntimeDeckRegistration> registrations,
        MatchRandomizer randomizer,
        bool shuffleDecks)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(randomizer);

        RuntimeDeckRegistration[] orderedRegistrations = registrations
            .Select(registration => registration ?? throw new ArgumentException("Deck registrations cannot contain null entries.", nameof(registrations)))
            .OrderBy(registration => registration.Id)
            .ToArray();
        if (orderedRegistrations.Select(registration => registration.Id).Distinct().Count() != orderedRegistrations.Length)
            throw new ArgumentException("Deck IDs must be unique.", nameof(registrations));

        HashSet<CardId> cardIds = [];
        int sequenceIndex = 0;
        Dictionary<DeckId, RuntimeDeck> prepared = [];
        foreach (RuntimeDeckRegistration registration in orderedRegistrations)
        {
            foreach (RuntimeCardRegistration card in registration.Cards)
            {
                if (!cardIds.Add(card.Id))
                    throw new ArgumentException($"Card ID '{card.Id}' occurs in more than one deck.", nameof(registrations));
            }

            RuntimeCard[] canonical = registration.Cards
                .Select(card => card.CreateRuntimeCard())
                .ToArray();
            IReadOnlyList<RuntimeCard> initialOrder = shuffleDecks
                ? ShuffleCopy(canonical, randomizer, ref sequenceIndex)
                : Array.AsReadOnly(canonical.ToArray());
            prepared.Add(
                registration.Id,
                new RuntimeDeck(registration.Id, registration.PresentationToken, canonical, initialOrder));
        }

        _decks = prepared;
    }

    internal static DeckRuntime CreateForProfile(
        IEnumerable<DeckDefinition> definitions,
        MatchRandomizer randomizer,
        bool shuffleDecks) => new(
            (definitions ?? throw new ArgumentNullException(nameof(definitions)))
                .Select(definition => new RuntimeDeckRegistration(definition)),
            randomizer,
            shuffleDecks);

    internal DeckCollection CreateSnapshot() =>
        new(_decks.Values.OrderBy(deck => deck.Id).Select(deck => deck.CreateView()));

    internal RuntimeCard DrawNextCard(DeckId deckId) => Resolve(deckId).DrawNext();

    internal IReadOnlyList<PresentationToken> RequiredPresentationTokens =>
        Array.AsReadOnly(_decks.Values
            .OrderBy(deck => deck.Id)
            .SelectMany(deck => new[] { deck.PresentationToken }
                .Concat(deck.CanonicalCards.Select(card => card.PresentationToken)))
            .ToArray());

    internal void EnsureReferences(IEnumerable<DeckId> references)
    {
        ArgumentNullException.ThrowIfNull(references);
        foreach (DeckId reference in references)
        {
            if (!reference.IsValid || !_decks.ContainsKey(reference))
                throw new ArgumentException($"Deck ID '{reference}' is referenced but missing.", nameof(references));
        }
    }

    private RuntimeDeck Resolve(DeckId deckId)
    {
        if (!deckId.IsValid) throw new ArgumentException("The deck ID is invalid.", nameof(deckId));
        return _decks.TryGetValue(deckId, out RuntimeDeck? deck)
            ? deck
            : throw new KeyNotFoundException($"Deck ID '{deckId}' is not defined.");
    }

    private static IReadOnlyList<RuntimeCard> ShuffleCopy(
        IReadOnlyList<RuntimeCard> source,
        MatchRandomizer randomizer,
        ref int sequenceIndex)
    {
        RuntimeCard[] shuffled = source.ToArray();
        for (int index = shuffled.Length - 1; index > 0; index--)
        {
            int selectedIndex = randomizer.NextInt(new RandomRequest(
                RandomPurpose.DeckShuffle,
                0,
                index + 1,
                sequenceIndex++));
            (shuffled[index], shuffled[selectedIndex]) = (shuffled[selectedIndex], shuffled[index]);
        }

        return Array.AsReadOnly(shuffled);
    }
}

internal sealed class RuntimeCard : ICardView
{
    private readonly ILegacyCard? _legacyCard;

    internal RuntimeCard(CardId id, ILegacyCard card)
    {
        if (!id.IsValid) throw new ArgumentException("The card ID is invalid.", nameof(id));
        _legacyCard = card ?? throw new ArgumentNullException(nameof(card));
        Id = id;
        PresentationToken = card.PresentationToken;
    }

    internal RuntimeCard(CardDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition;
        Id = definition.Id;
        PresentationToken = definition.PresentationToken;
    }

    public CardId Id { get; }
    public PresentationToken PresentationToken { get; }
    internal CardDefinition? Definition { get; }
    internal void ExecuteEffect(Player player, Game game) =>
        (_legacyCard ?? throw new InvalidOperationException("Profile card execution is not available yet."))
            .ExecuteEffect(player, game);
}

internal sealed class RuntimeCardRegistration
{
    private readonly CardDefinition? _definition;

    internal RuntimeCardRegistration(CardId id, ILegacyCard card)
    {
        if (!id.IsValid) throw new ArgumentException("The card ID is invalid.", nameof(id));
        Id = id;
        LegacyCard = card ?? throw new ArgumentNullException(nameof(card));
    }

    internal RuntimeCardRegistration(CardDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Id = definition.Id;
    }

    internal CardId Id { get; }
    internal ILegacyCard? LegacyCard { get; }
    internal RuntimeCard CreateRuntimeCard() => _definition is null
        ? new RuntimeCard(Id, LegacyCard!)
        : new RuntimeCard(_definition);
}

internal sealed class RuntimeDeckRegistration
{
    internal RuntimeDeckRegistration(
        DeckId id,
        PresentationToken presentationToken,
        IEnumerable<RuntimeCardRegistration> cards)
    {
        if (!id.IsValid) throw new ArgumentException("The deck ID is invalid.", nameof(id));
        if (!presentationToken.IsValid)
            throw new ArgumentException("The deck presentation token is invalid.", nameof(presentationToken));
        ArgumentNullException.ThrowIfNull(cards);

        RuntimeCardRegistration[] copiedCards = cards.ToArray();
        if (copiedCards.Length == 0)
            throw new ArgumentException("A declared deck requires at least one card.", nameof(cards));
        if (copiedCards.Any(card => card is null))
            throw new ArgumentException("A deck cannot contain null cards.", nameof(cards));
        if (copiedCards.Select(card => card.Id).Distinct().Count() != copiedCards.Length)
            throw new ArgumentException("Card IDs must be unique within a deck.", nameof(cards));

        Id = id;
        PresentationToken = presentationToken;
        Cards = Array.AsReadOnly(copiedCards);
    }

    internal RuntimeDeckRegistration(DeckDefinition definition)
        : this(
            (definition ?? throw new ArgumentNullException(nameof(definition))).Id,
            definition.PresentationToken,
            definition.Cards.Select(card => new RuntimeCardRegistration(card)))
    {
    }

    internal DeckId Id { get; }
    internal PresentationToken PresentationToken { get; }
    internal IReadOnlyList<RuntimeCardRegistration> Cards { get; }
}

internal sealed class RuntimeDeck
{
    private Queue<RuntimeCard> _cards;

    internal RuntimeDeck(
        DeckId id,
        PresentationToken presentationToken,
        IReadOnlyList<RuntimeCard> canonicalCards,
        IReadOnlyList<RuntimeCard> initialOrder)
    {
        Id = id;
        PresentationToken = presentationToken;
        CanonicalCards = Array.AsReadOnly(canonicalCards.ToArray());
        _cards = new Queue<RuntimeCard>(initialOrder);
    }

    internal DeckId Id { get; }
    internal PresentationToken PresentationToken { get; }
    internal IReadOnlyList<RuntimeCard> CanonicalCards { get; }
    internal IReadOnlyList<RuntimeCard> CurrentCards => Array.AsReadOnly(_cards.ToArray());

    internal RuntimeCard DrawNext()
    {
        RuntimeCard card = _cards.Dequeue();
        _cards.Enqueue(card);
        return card;
    }

    internal DeckView CreateView() => new(Id, PresentationToken, CurrentCards);
}
