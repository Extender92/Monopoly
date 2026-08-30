using Monopoly.Core.Models;
using Monopoly.Core.Models.FortuneCard;
using Monopoly.Core.Presentation;

namespace Monopoly.Tests.CoreTests;

public sealed class DeckCollectionTests
{
    [Fact]
    public void RuntimeSupportsZeroOneAndMultipleDecksWithoutNamedRoles()
    {
        DeckRuntime empty = CreateHandler([]);
        DeckRuntime single = CreateHandler([Registration("deck.alpha", "card.alpha")]);
        DeckRuntime multiple = CreateHandler(
        [
            Registration("deck.zeta", "card.zeta"),
            Registration("deck.alpha", "card.alpha")
        ]);

        Assert.Empty(empty.CreateSnapshot().Entries);
        Assert.Single(single.CreateSnapshot().Entries);
        Assert.Equal(
            [new DeckId("deck.alpha"), new DeckId("deck.zeta")],
            multiple.CreateSnapshot().Entries.Select(deck => deck.Id));
    }

    [Fact]
    public void DrawRotatesOnlyTheSelectedDeckAndOldSnapshotsStayDetached()
    {
        DeckId selectedId = new("deck.selected");
        DeckId otherId = new("deck.other");
        DeckRuntime handler = CreateHandler(
        [
            Registration("deck.selected", "card.one", "card.two"),
            Registration("deck.other", "card.other")
        ]);
        DeckCollection before = handler.CreateSnapshot();

        RuntimeCard drawn = handler.DrawNextCard(selectedId);
        DeckCollection after = handler.CreateSnapshot();

        Assert.Equal(new CardId("card.one"), drawn.Id);
        Assert.Equal(
            [new CardId("card.one"), new CardId("card.two")],
            before.Resolve(selectedId).Cards.Select(card => card.Id));
        Assert.Equal(
            [new CardId("card.two"), new CardId("card.one")],
            after.Resolve(selectedId).Cards.Select(card => card.Id));
        Assert.Equal(
            before.Resolve(otherId).Cards.Select(card => card.Id),
            after.Resolve(otherId).Cards.Select(card => card.Id));
    }

    [Fact]
    public void PublicSnapshotsCannotMutateRuntimeState()
    {
        DeckRuntime handler = CreateHandler([Registration("deck.alpha", "card.alpha")]);
        DeckCollection snapshot = handler.CreateSnapshot();

        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<DeckId, DeckView>)snapshot.ById).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IList<DeckView>)snapshot.Entries).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IList<ICardView>)snapshot.Entries[0].Cards).Clear());
        Assert.All(snapshot.Entries[0].Cards, card => Assert.IsType<CardView>(card));
        Assert.Single(handler.CreateSnapshot().Entries);
        Assert.Single(handler.CreateSnapshot().Entries[0].Cards);
    }

    [Fact]
    public void InvalidDeckDefinitionsAreRejectedBeforeRuntimeIsAvailable()
    {
        RuntimeCardRegistration shared = Card("card.shared");

        Assert.Throws<ArgumentException>(() =>
            new RuntimeDeckRegistration(
                new DeckId("deck.empty"),
                new PresentationToken("deck.empty"),
                []));
        Assert.Throws<ArgumentException>(() => CreateHandler(
        [
            new RuntimeDeckRegistration(new DeckId("deck.duplicate"), new PresentationToken("deck.one"), [Card("card.one")]),
            new RuntimeDeckRegistration(new DeckId("deck.duplicate"), new PresentationToken("deck.two"), [Card("card.two")])
        ]));
        Assert.Throws<ArgumentException>(() => CreateHandler(
        [
            new RuntimeDeckRegistration(new DeckId("deck.one"), new PresentationToken("deck.one"), [shared]),
            new RuntimeDeckRegistration(new DeckId("deck.two"), new PresentationToken("deck.two"), [shared])
        ]));
    }

    [Fact]
    public void MultipleDecksUseDeterministicOrdinalShuffleOrder()
    {
        ScriptedMatchRandomSource random = new(0, 0, 0, 0);
        DeckRuntime handler = new(
        [
            Registration("deck.zeta", "card.zeta-one", "card.zeta-two", "card.zeta-three"),
            Registration("deck.alpha", "card.alpha-one", "card.alpha-two", "card.alpha-three")
        ],
            new MatchRandomizer(random),
            shuffleDecks: true);

        Assert.Equal(
            [0, 1, 2, 3],
            random.Requests.Select(request => request.SequenceIndex));
        Assert.Equal(
            [new CardId("card.alpha-two"), new CardId("card.alpha-three"), new CardId("card.alpha-one")],
            handler.CreateSnapshot().Resolve(new DeckId("deck.alpha")).Cards.Select(card => card.Id));
        Assert.Equal(
            [new CardId("card.zeta-two"), new CardId("card.zeta-three"), new CardId("card.zeta-one")],
            handler.CreateSnapshot().Resolve(new DeckId("deck.zeta")).Cards.Select(card => card.Id));
    }

    private static DeckRuntime CreateHandler(IEnumerable<RuntimeDeckRegistration> registrations) =>
        new(registrations, new MatchRandomizer(new MinimumMatchRandomSource()), shuffleDecks: false);

    private static RuntimeDeckRegistration Registration(string deckId, params string[] cardIds) =>
        new(
            new DeckId(deckId),
            new PresentationToken(deckId),
            cardIds.Select(Card));

    private static RuntimeCardRegistration Card(string id) =>
        new(new CardId(id), new TestCard(id));

    private sealed class TestCard : ILegacyCard
    {
        internal TestCard(string id) =>
            Presentation = new PresentationMetadata(new PresentationToken($"presentation.{id}"));

        public PresentationMetadata Presentation { get; }
        public PresentationToken PresentationToken => Presentation.Token;
        public void ExecuteEffect(Player player, Game game)
        {
        }
    }
}
