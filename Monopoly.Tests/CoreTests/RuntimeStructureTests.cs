using System.Reflection;
using Monopoly.Core.Interface;
using Monopoly.Core.Models.Board;

namespace Monopoly.Tests.CoreTests;

public sealed class RuntimeStructureTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(3, 1)]
    [InlineData(27, 2)]
    [InlineData(53, 3)]
    public void RuntimeUsesGenericTracksAndDeckCollections(int spaces, int decks)
    {
        ValidatedGameProfile profile = GameProfileValidator.Validate(ProfileTestFactory.Create(
            spaceCount: spaces,
            deckCount: decks,
            cardsPerDeck: 2));
        Game game = GameSetup.Create(profile, [new PlayerSetup(1, "Reader")], new MinimumMatchRandomSource());

        Assert.Equal(spaces, game.Board.Spaces.Count);
        Assert.Equal(decks, game.Decks.Count);
        Assert.Equal(profile.RuleGraph.Track.SpaceIds, game.Board.Track.SpaceIds);
        Assert.Throws<NotSupportedException>(() => ((IList<SpaceView>)game.Board.Spaces).Clear());
        if (decks > 0)
            Assert.Throws<NotSupportedException>(() => ((IList<ICardView>)game.Decks.Entries[0].Cards).Clear());
    }

    [Fact]
    public void UnsupportedNestedDrawShapeFailsBeforeMatchConstruction()
    {
        DeckId deckId = new("deck.execution");
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 2,
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [1] = [new DrawCapabilityDefinition(deckId)]
            },
            decks:
            [
                new TestDeckSpec("deck.execution",
                [
                    new TestCardSpec("card.execution-loop",
                    [
                        new MoveEffectDefinition(
                            new AbsoluteMoveTarget(new SpaceId("space.execution-1")),
                            PassOriginPolicy.Ignore,
                            resolveDestination: true)
                    ])
                ])
            ]);

        GameSetupException exception = Assert.Throws<GameSetupException>(() =>
            GameSetup.Create(profile, [new PlayerSetup(1, "Blocked")], new MinimumMatchRandomSource()));

        Assert.Equal(GameSetupErrorKind.UnsupportedComponent, exception.Kind);
        Assert.Contains("#36", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MultipleMovementEffectsFailBeforeMatchConstruction()
    {
        DeckId deckId = new("deck.execution");
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 3,
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [1] = [new DrawCapabilityDefinition(deckId)]
            },
            decks:
            [
                new TestDeckSpec("deck.execution",
                [
                    new TestCardSpec("card.execution-multiple-moves",
                    [
                        new MoveEffectDefinition(new RelativeMoveTarget(1), PassOriginPolicy.Ignore, resolveDestination: false),
                        new MoveEffectDefinition(new RelativeMoveTarget(1), PassOriginPolicy.Ignore, resolveDestination: false)
                    ])
                ])
            ]);

        GameSetupException exception = Assert.Throws<GameSetupException>(() =>
            GameSetup.Create(profile, [new PlayerSetup(1, "Blocked")], new MinimumMatchRandomSource()));

        Assert.Equal(GameSetupErrorKind.UnsupportedComponent, exception.Kind);
        Assert.Contains("at most one movement effect", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvingMovementMustBeTheLastEffectUntilComplexChainsAreSupported()
    {
        DeckId deckId = new("deck.execution");
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 3,
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [1] = [new DrawCapabilityDefinition(deckId)]
            },
            decks:
            [
                new TestDeckSpec("deck.execution",
                [
                    new TestCardSpec("card.execution-resolving-first",
                    [
                        new MoveEffectDefinition(new RelativeMoveTarget(1), PassOriginPolicy.Ignore, resolveDestination: true),
                        new ResourceChangeEffectDefinition(ExecutionProfileFactory.Credits, 1)
                    ])
                ])
            ]);

        GameSetupException exception = Assert.Throws<GameSetupException>(() =>
            GameSetup.Create(profile, [new PlayerSetup(1, "Blocked")], new MinimumMatchRandomSource()));

        Assert.Equal(GameSetupErrorKind.UnsupportedComponent, exception.Kind);
        Assert.Contains("final baseline effect", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicRuntimeContainsNoProductShapedParallelTypes()
    {
        Assembly core = typeof(Game).Assembly;
        string[] removedTypes =
        [
            "Monopoly.Core.GameRules",
            "Monopoly.Core.GameHandler",
            "Monopoly.Core.Transaction",
            "Monopoly.Core.Jail",
            "Monopoly.Core.Models.Board.Square",
            "Monopoly.Core.Models.Board.PropertySquare",
            "Monopoly.Core.Models.Board.RailroadSquare",
            "Monopoly.Core.Models.Board.UtilitySquare",
            "Monopoly.Core.Models.FortuneCard.ILegacyCard"
        ];

        Assert.All(removedTypes, name => Assert.Null(core.GetType(name)));
        Assert.Equal(typeof(ValidatedGameProfile), typeof(IGame).GetProperty(nameof(IGame.Profile))!.PropertyType);
        Assert.Null(typeof(Game).GetProperty("Rules", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        Assert.Null(typeof(Game).GetProperty("Fines"));
        Assert.Null(typeof(Game).GetProperty("ConsecutiveDoubles"));
        Assert.Null(typeof(TurnResult).GetProperty("ExtraTurn"));
        Assert.NotNull(typeof(DecisionResponse).GetProperty(nameof(DecisionResponse.PlayerId)));
        Assert.DoesNotContain(typeof(GameActionRejectionReason).GetEnumNames(), name => name == "CapabilityExecutionUnavailable");
    }
}
