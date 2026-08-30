using Monopoly.Core.Models.Board;
using Monopoly.Core.Presentation;
using Monopoly.Core.Interface;
using Monopoly.Core.Persistence;
using System.Reflection;

namespace Monopoly.Tests.CoreTests;

public sealed class GameStructureTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(27)]
    [InlineData(53)]
    public void TrackAcceptsProfileDefinedLengths(int count)
    {
        SpaceId[] ids = Enumerable.Range(0, count)
            .Select(index => new SpaceId($"route.space-{index}"))
            .ToArray();

        GameTrack track = new(ids);

        Assert.Equal(count, track.Count);
        Assert.Equal(ids, track.SpaceIds);
        Assert.Equal(ids[^1], track.GetSpaceIdAt(count - 1));
        Assert.Throws<NotSupportedException>(() => ((IList<SpaceId>)track.SpaceIds).Clear());
    }

    [Fact]
    public void TrackResolvesIdentityAndNormalizesMovementInBothDirections()
    {
        GameTrack track = new(
        [
            new SpaceId("route.alpha"),
            new SpaceId("route.beta"),
            new SpaceId("route.gamma")
        ]);

        Assert.Equal(1, track.GetIndex(new SpaceId("route.beta")));
        Assert.Equal(1, track.NormalizeIndex(4));
        Assert.Equal(2, track.NormalizeIndex(-1));
        Assert.Equal(new SpaceId("route.alpha"),
            track.GetSpaceIdAfter(new SpaceId("route.beta"), 2));
        Assert.Equal(new SpaceId("route.gamma"),
            track.GetSpaceIdAfter(new SpaceId("route.alpha"), -1));
    }

    [Fact]
    public void StructuralIdentifiersValidateAndRemainStronglyTyped()
    {
        SpaceId space = new("route.alpha-1");
        DeckId deck = new("route.alpha-1");
        CardId card = new("route.alpha-1");

        Assert.True(space.IsValid);
        Assert.True(deck.IsValid);
        Assert.True(card.IsValid);
        Assert.False(default(SpaceId).IsValid);
        Assert.NotEqual(typeof(SpaceId), typeof(DeckId));
        Assert.NotEqual(typeof(DeckId), typeof(CardId));
        Assert.Throws<ArgumentException>(() => new SpaceId("Route Alpha"));
        Assert.Throws<ArgumentException>(() => new DeckId("deck_alpha"));
        Assert.Throws<ArgumentException>(() => new CardId(""));
    }

    [Fact]
    public void BoardCopiesOrderedSpacesAndRejectsDuplicateOrNonContiguousStructure()
    {
        List<Square> source =
        [
            new SyntheticSquare(new SpaceId("route.alpha"), 0),
            new SyntheticSquare(new SpaceId("route.beta"), 1)
        ];
        GameBoard board = new(source);

        source.Clear();

        Assert.Equal(2, board.Track.Count);
        Assert.Equal(new SpaceId("route.beta"), board.GetSquareAtPosition(1).Id);
        Assert.Same(board.GetSquareAtPosition(1), board.GetSquare(new SpaceId("route.beta")));
        Assert.Throws<ArgumentException>(() => new GameBoard(
        [
            new SyntheticSquare(new SpaceId("route.same"), 0),
            new SyntheticSquare(new SpaceId("route.same"), 1)
        ]));
        Assert.Throws<ArgumentException>(() => new GameBoard(
        [
            new SyntheticSquare(new SpaceId("route.alpha"), 0),
            new SyntheticSquare(new SpaceId("route.beta"), 2)
        ]));
    }

    [Fact]
    public void LayoutHintsNeverDefineAuthoritativeSpaceIdentity()
    {
        PresentationToken sharedLayout = new("layout.shared");
        SyntheticSquare first = new(new SpaceId("route.first"), 0, sharedLayout);
        SyntheticSquare second = new(new SpaceId("route.second"), 1, sharedLayout);
        GameBoard board = new([first, second]);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(first.Presentation.LayoutToken, second.Presentation.LayoutToken);
        Assert.Equal([first.Id, second.Id], board.Track.SpaceIds);
    }

    [Fact]
    public void MissingDeckReferenceIsRejectedBeforeGameCompositionCompletes()
    {
        GameBoard board = new(
        [
            new SyntheticDeckReferenceSquare(
                new SpaceId("route.draw"),
                0,
                new DeckId("deck.missing"))
        ]);
        DeckRuntime noDecks = new(
            [],
            new MatchRandomizer(new MinimumMatchRandomSource()),
            shuffleDecks: false);

        Assert.Throws<ArgumentException>(() =>
            noDecks.EnsureReferences(board.ReferencedDeckIds));
    }

    [Fact]
    public void PublicRuntimeSurfaceExposesOnlyGenericTrackAndDeckContracts()
    {
        Assembly core = typeof(Game).Assembly;

        Assert.Equal(typeof(DeckCollection), typeof(Game).GetProperty(nameof(Game.Decks))!.PropertyType);
        Assert.Equal(typeof(DeckCollection), typeof(IGame).GetProperty(nameof(IGame.Decks))!.PropertyType);
        Assert.Equal(typeof(GameTrack), typeof(GameBoard).GetProperty(nameof(GameBoard.Track))!.PropertyType);
        Assert.Null(typeof(Game).GetProperty("FortuneCard"));
        Assert.Null(typeof(IGame).GetProperty("FortuneCard"));
        Assert.False(typeof(DeckRuntime).IsPublic);
        Assert.Null(core.GetType("Monopoly.Core.Models.FortuneCard.IFortuneCardView"));
        Assert.DoesNotContain(
            typeof(PresentationTokens).GetProperties(BindingFlags.Public | BindingFlags.Static),
            property => property.Name is "PrimaryDeck" or "SecondaryDeck");

        Assert.DoesNotContain(core.GetTypes(), type =>
            type.Namespace == "Monopoly.Core.Persistence" &&
            (type.Name.EndsWith("V1", StringComparison.Ordinal) ||
             type.Name.EndsWith("V1Mapper", StringComparison.Ordinal)));
    }

    private sealed class SyntheticSquare : Square
    {
        internal SyntheticSquare(SpaceId id, int position, PresentationToken? layout = null)
            : base(
                id,
                position,
                new PresentationMetadata(
                    new PresentationToken($"space.synthetic-{position}"),
                    layoutToken: layout))
        {
        }

        internal override void LandOn(Player player, Game game)
        {
        }
    }

    private sealed class SyntheticDeckReferenceSquare : Square, IDeckReferenceSpace
    {
        internal SyntheticDeckReferenceSquare(SpaceId id, int position, DeckId deckId)
            : base(id, position, new PresentationMetadata(new PresentationToken("space.synthetic-draw"))) =>
            DeckId = deckId;

        public DeckId DeckId { get; }

        internal override void LandOn(Player player, Game game)
        {
        }
    }
}
