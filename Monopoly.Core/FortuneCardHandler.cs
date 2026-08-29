using Monopoly.Core.Models.FortuneCard;
using Monopoly.Core.Models;
using Monopoly.Core.Presentation;
using Monopoly.Core.Randomness;

namespace Monopoly.Core;

internal sealed class FortuneCardHandler
{
    private readonly Dictionary<DeckId, RuntimeDeck> _decks;

    internal FortuneCardHandler(GameRules gameRules, MatchRandomizer randomizer, bool shuffleDecks)
        : this(CreateLegacyRegistrations(gameRules), randomizer, shuffleDecks)
    {
    }

    internal FortuneCardHandler(
        IEnumerable<RuntimeDeckRegistration> registrations,
        MatchRandomizer randomizer,
        bool shuffleDecks)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(randomizer);

        RuntimeDeckRegistration[] copiedRegistrations = registrations.ToArray();
        if (copiedRegistrations.Any(registration => registration is null))
            throw new ArgumentException("Deck registrations cannot contain null entries.", nameof(registrations));
        RuntimeDeckRegistration[] orderedRegistrations = copiedRegistrations
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
                .Select(card => new RuntimeCard(card.Id, card.Card))
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

    internal IReadOnlyList<PresentationMetadata> AllPresentationMetadata =>
        Array.AsReadOnly(_decks.Values
            .OrderBy(deck => deck.Id)
            .SelectMany(deck => deck.CanonicalCards)
            .Select(card => card.Presentation)
            .ToArray());

    internal DeckCollection CreateSnapshot() =>
        new(_decks.Values
            .OrderBy(deck => deck.Id)
            .Select(deck => deck.CreateView()));

    internal RuntimeCard DrawNextCard(DeckId deckId) => Resolve(deckId).DrawNext();

    internal void EnsureReferences(IEnumerable<DeckId> references)
    {
        ArgumentNullException.ThrowIfNull(references);
        foreach (DeckId reference in references)
        {
            if (!reference.IsValid || !_decks.ContainsKey(reference))
                throw new ArgumentException($"Deck ID '{reference}' is referenced but missing.", nameof(references));
        }
    }

    internal IReadOnlyList<string> GetLegacyPrimaryDeckOrder() =>
        GetLegacyDeckOrder(LegacyStructureIds.PrimaryDeck);

    internal IReadOnlyList<string> GetLegacySecondaryDeckOrder() =>
        GetLegacyDeckOrder(LegacyStructureIds.SecondaryDeck);

    internal void RestoreLegacyDeckOrder(
        IReadOnlyList<string> primaryOrder,
        IReadOnlyList<string> secondaryOrder)
    {
        ArgumentNullException.ThrowIfNull(primaryOrder);
        ArgumentNullException.ThrowIfNull(secondaryOrder);

        RuntimeDeck primary = Resolve(LegacyStructureIds.PrimaryDeck);
        RuntimeDeck secondary = Resolve(LegacyStructureIds.SecondaryDeck);
        IReadOnlyList<RuntimeCard> preparedPrimary = primary.PrepareLegacyOrder(primaryOrder);
        IReadOnlyList<RuntimeCard> preparedSecondary = secondary.PrepareLegacyOrder(secondaryOrder);

        primary.ReplaceOrder(preparedPrimary);
        secondary.ReplaceOrder(preparedSecondary);
    }

    private RuntimeDeck Resolve(DeckId deckId)
    {
        if (!deckId.IsValid) throw new ArgumentException("The deck ID is invalid.", nameof(deckId));
        return _decks.TryGetValue(deckId, out RuntimeDeck? deck)
            ? deck
            : throw new KeyNotFoundException($"Deck ID '{deckId}' is not defined.");
    }

    private IReadOnlyList<string> GetLegacyDeckOrder(DeckId deckId)
    {
        RuntimeDeck deck = Resolve(deckId);
        return Array.AsReadOnly(deck.CurrentCards
            .Select(card => deck.IndexOfCanonicalCard(card).ToString())
            .ToArray());
    }

    private static RuntimeDeckRegistration[] CreateLegacyRegistrations(GameRules gameRules)
    {
        ArgumentNullException.ThrowIfNull(gameRules);

        return
        [
            CreateLegacyRegistration(
                LegacyStructureIds.PrimaryDeck,
                PresentationTokens.PrimaryDeck,
                Data.FortuneCardBuilder.GetChanceCards(gameRules)),
            CreateLegacyRegistration(
                LegacyStructureIds.SecondaryDeck,
                PresentationTokens.SecondaryDeck,
                Data.FortuneCardBuilder.GetCommunityChestCards(gameRules))
        ];
    }

    private static RuntimeDeckRegistration CreateLegacyRegistration(
        DeckId id,
        PresentationToken presentationToken,
        IReadOnlyList<ILegacyCard> cards) =>
        new(
            id,
            presentationToken,
            cards.Select((card, index) =>
                new RuntimeCardRegistration(LegacyStructureIds.Card(id, index), card)));

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
    private readonly ILegacyCard _card;

    internal RuntimeCard(CardId id, ILegacyCard card)
    {
        if (!id.IsValid) throw new ArgumentException("The card ID is invalid.", nameof(id));
        _card = card ?? throw new ArgumentNullException(nameof(card));
        Id = id;
    }

    public CardId Id { get; }
    public PresentationToken PresentationToken => _card.Presentation.Token;
    internal PresentationMetadata Presentation => _card.Presentation;
    internal void ExecuteEffect(Player player, Game game) => _card.ExecuteEffect(player, game);
}

internal sealed class RuntimeCardRegistration
{
    internal RuntimeCardRegistration(CardId id, ILegacyCard card)
    {
        if (!id.IsValid) throw new ArgumentException("The card ID is invalid.", nameof(id));
        Id = id;
        Card = card ?? throw new ArgumentNullException(nameof(card));
    }

    internal CardId Id { get; }
    internal ILegacyCard Card { get; }
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

    internal DeckId Id { get; }
    internal PresentationToken PresentationToken { get; }
    internal IReadOnlyList<RuntimeCardRegistration> Cards { get; }
}

internal sealed class RuntimeDeck
{
    private Queue<RuntimeCard> _cards;
    private readonly Dictionary<CardId, int> _canonicalIndices;

    internal RuntimeDeck(
        DeckId id,
        PresentationToken presentationToken,
        IReadOnlyList<RuntimeCard> canonicalCards,
        IReadOnlyList<RuntimeCard> initialOrder)
    {
        Id = id;
        PresentationToken = presentationToken;
        CanonicalCards = Array.AsReadOnly(canonicalCards.ToArray());
        _canonicalIndices = CanonicalCards
            .Select((card, index) => (card.Id, index))
            .ToDictionary(entry => entry.Id, entry => entry.index);
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

    internal int IndexOfCanonicalCard(RuntimeCard card) => _canonicalIndices[card.Id];

    internal IReadOnlyList<RuntimeCard> PrepareLegacyOrder(IReadOnlyList<string> order)
    {
        if (order.Count != CanonicalCards.Count ||
            order.Any(key => !int.TryParse(key, out int index) || index < 0 || index >= CanonicalCards.Count) ||
            order.Distinct().Count() != order.Count)
        {
            throw new ArgumentException("Card order must contain every card exactly once.", nameof(order));
        }

        return Array.AsReadOnly(order.Select(key => CanonicalCards[int.Parse(key)]).ToArray());
    }

    internal void ReplaceOrder(IReadOnlyList<RuntimeCard> order) =>
        _cards = new Queue<RuntimeCard>(order);
}
