using System.Collections.ObjectModel;
using Monopoly.Core.Presentation;

namespace Monopoly.Core;

public interface ICardView
{
    CardId Id { get; }
    PresentationToken PresentationToken { get; }
}

/// <summary>An immutable card identity and presentation reference.</summary>
public sealed class CardView : ICardView
{
    public CardView(CardId id, PresentationToken presentationToken)
    {
        if (!id.IsValid) throw new ArgumentException("The card ID is invalid.", nameof(id));
        if (!presentationToken.IsValid)
            throw new ArgumentException("The card presentation token is invalid.", nameof(presentationToken));

        Id = id;
        PresentationToken = presentationToken;
    }

    public CardId Id { get; }
    public PresentationToken PresentationToken { get; }
}

/// <summary>An immutable read model of one deck in its current card order.</summary>
public sealed class DeckView
{
    private readonly ReadOnlyCollection<ICardView> _cards;

    public DeckView(DeckId id, PresentationToken presentationToken, IEnumerable<ICardView> cards)
    {
        if (!id.IsValid) throw new ArgumentException("The deck ID is invalid.", nameof(id));
        if (!presentationToken.IsValid)
            throw new ArgumentException("The deck presentation token is invalid.", nameof(presentationToken));
        ArgumentNullException.ThrowIfNull(cards);

        ICardView[] suppliedCards = cards.ToArray();
        if (suppliedCards.Length == 0)
            throw new ArgumentException("A declared deck requires at least one card.", nameof(cards));
        if (suppliedCards.Any(card => card is null))
            throw new ArgumentException("A deck contains a null card.", nameof(cards));

        ICardView[] copiedCards = suppliedCards
            .Select(card => (ICardView)new CardView(card.Id, card.PresentationToken))
            .ToArray();
        if (copiedCards.Select(card => card.Id).Distinct().Count() != copiedCards.Length)
            throw new ArgumentException("Card IDs must be unique within a deck.", nameof(cards));

        Id = id;
        PresentationToken = presentationToken;
        _cards = Array.AsReadOnly(copiedCards);
    }

    public DeckId Id { get; }
    public PresentationToken PresentationToken { get; }
    public IReadOnlyList<ICardView> Cards => _cards;
}

/// <summary>An immutable, ordinally ordered collection indexed by stable deck identity.</summary>
public sealed class DeckCollection
{
    private readonly ReadOnlyCollection<DeckView> _entries;
    private readonly ReadOnlyDictionary<DeckId, DeckView> _byId;

    public DeckCollection(IEnumerable<DeckView> decks)
    {
        ArgumentNullException.ThrowIfNull(decks);

        DeckView[] entries = decks.ToArray();
        if (entries.Any(deck => deck is null))
            throw new ArgumentException("A deck collection cannot contain null entries.", nameof(decks));

        Dictionary<DeckId, DeckView> byId = [];
        HashSet<CardId> cardIds = [];
        foreach (DeckView deck in entries)
        {
            if (!byId.TryAdd(deck.Id, deck))
                throw new ArgumentException($"Deck ID '{deck.Id}' is duplicated.", nameof(decks));

            foreach (ICardView card in deck.Cards)
            {
                if (!cardIds.Add(card.Id))
                    throw new ArgumentException($"Card ID '{card.Id}' occurs in more than one deck.", nameof(decks));
            }
        }

        DeckView[] sorted = entries.OrderBy(deck => deck.Id).ToArray();
        _entries = Array.AsReadOnly(sorted);
        _byId = new ReadOnlyDictionary<DeckId, DeckView>(
            sorted.ToDictionary(deck => deck.Id));
    }

    public IReadOnlyList<DeckView> Entries => _entries;
    public IReadOnlyDictionary<DeckId, DeckView> ById => _byId;
    public int Count => _entries.Count;

    public DeckView Resolve(DeckId id)
    {
        if (!id.IsValid) throw new ArgumentException("The deck ID is invalid.", nameof(id));
        return _byId.TryGetValue(id, out DeckView? deck)
            ? deck
            : throw new KeyNotFoundException($"Deck ID '{id}' is not defined.");
    }

    public bool TryResolve(DeckId id, out DeckView? deck)
    {
        if (!id.IsValid)
        {
            deck = null;
            return false;
        }

        return _byId.TryGetValue(id, out deck);
    }
}
