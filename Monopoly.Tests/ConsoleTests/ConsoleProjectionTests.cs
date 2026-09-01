using Monopoly.Console;
using Monopoly.Console.GUI;
using Monopoly.Core;
using Monopoly.Core.Logs;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Notifications;
using Monopoly.Core.Presentation;
using Monopoly.Tests.TestDoubles;

namespace Monopoly.Tests.ConsoleTests;

public sealed class ConsoleProjectionTests
{
    [Fact]
    public void TrackProjectionRendersGenericCapabilitiesPlayersAndOwnership()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [1] =
                [
                    new OwnableCapabilityDefinition(new GroupId("group.sample")),
                    new PurchasableCapabilityDefinition(new ResourceAmount(ExecutionProfileFactory.Credits, 5)),
                    new UsageFeeCapabilityDefinition(new ResourceAmount(ExecutionProfileFactory.Credits, 2))
                ]
            });
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(0, "Aster")],
            ScriptedMatchRandomSource.ForDice(1));
        _ = game.PlayTurn();
        _ = game.SubmitDecision(new DecisionResponse(
            game.PendingDecision!.DecisionId,
            0,
            DecisionOptions.Accept));

        ConsoleMatchProjection projection = new ConsoleProjectionBuilder().Build(game);
        ConsoleSpaceProjection owned = projection.Spaces[1];

        Assert.Equal("Execution Space 1", owned.Name);
        Assert.Equal("Aster", owned.Owner);
        Assert.Contains("Aster", owned.Players);
        Assert.Contains(owned.Capabilities, value => value.Contains("price 5", StringComparison.Ordinal));
        Assert.Contains(owned.Capabilities, value => value.Contains("usage fee 2", StringComparison.Ordinal));
    }

    [Fact]
    public void ZeroOneAndMultipleDecksRenderByIdWithoutFutureCardContents()
    {
        AssertDeckProjection([], []);
        AssertDeckProjection(
            [new TestDeckSpec("deck.single", [Card("card.single")])],
            ["deck.single"]);
        AssertDeckProjection(
            [
                new TestDeckSpec("deck.zulu", [Card("card.zulu")]),
                new TestDeckSpec("deck.alpha", [Card("card.alpha"), Card("card.beta")])
            ],
            ["deck.alpha", "deck.zulu"]);
    }

    [Fact]
    public void DrawnCardAndAllStructuredNotificationsFormatInOrder()
    {
        TestDeckSpec deck = new("deck.events", [Card("card.message")]);
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(decks: [deck]);
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(0, "Aster"), new PlayerSetup(1, "Bramble")],
            new MinimumMatchRandomSource());
        SpaceId first = game.Board.Spaces[0].Id;
        SpaceId second = game.Board.Spaces[1].Id;
        IReadOnlyList<GameNotification> notifications =
        [
            new LogAddedNotification(new Log { Info = "Log entry" }, profile.PresentationToken),
            new PlayerMovedNotification(0, first, second, 0, game.Board.Spaces[1].PresentationToken),
            new ResourceChangedNotification(0, ExecutionProfileFactory.Credits, 20, 18, new PresentationToken("resource.credits")),
            new OwnershipChangedNotification(second, null, 0, game.Board.Spaces[1].PresentationToken),
            new DecisionResolvedNotification(Guid.NewGuid(), 0, DecisionKinds.Purchase, DecisionOptions.Accept, game.Board.Spaces[1].PresentationToken),
            new CardDrawnNotification(new CardView(new CardId("card.message"), new PresentationToken("card.message")), new DeckId("deck.events"), new PresentationToken("deck.events")),
            new TurnAdvancedNotification(1, 1, profile.PresentationToken),
            new MatchEndedNotification(0, 5, ExecutionProfileFactory.Score, profile.PresentationToken)
        ];

        IReadOnlyList<string> messages = new ConsoleNotificationFormatter().Format(game, notifications);

        Assert.Equal(8, messages.Count);
        Assert.Equal("Log entry", messages[0]);
        Assert.Contains("moved", messages[1], StringComparison.Ordinal);
        Assert.Contains("Credits", messages[2], StringComparison.Ordinal);
        Assert.Contains("acquired", messages[3], StringComparison.Ordinal);
        Assert.Contains("accept", messages[4], StringComparison.Ordinal);
        Assert.Contains("card.message", messages[5], StringComparison.Ordinal);
        Assert.Contains("Bramble is next", messages[6], StringComparison.Ordinal);
        Assert.Contains("Aster won", messages[7], StringComparison.Ordinal);
    }

    [Fact]
    public void PresentationUsesNeutralFallbackAndFailsClearlyForMissingRequiredToken()
    {
        Assert.Equal(
            ConsoleColor.White,
            ConsolePresentationResolver.GetColorHint(new PresentationToken("accent.unknown")));
        ConsolePresentationResolver resolver = new(new ProfilePresentation([]));

        ConsoleProjectionException exception = Assert.Throws<ConsoleProjectionException>(
            () => resolver.GetDisplayText(new PresentationToken("space.missing")));

        Assert.Equal(ConsoleProjectionErrorKind.MissingPresentation, exception.Kind);
    }

    [Theory]
    [InlineData("accent.moss", ConsoleColor.DarkGreen)]
    [InlineData("accent.glass", ConsoleColor.Cyan)]
    [InlineData("accent.river", ConsoleColor.Blue)]
    [InlineData("accent.copper", ConsoleColor.DarkYellow)]
    [InlineData("accent.lantern", ConsoleColor.Yellow)]
    [InlineData("accent.event", ConsoleColor.DarkCyan)]
    [InlineData("accent.neutral", ConsoleColor.White)]
    public void KnownDemoAccentTokensMapToFrontendColors(string token, ConsoleColor expected)
    {
        Assert.Equal(expected, ConsolePresentationResolver.GetColorHint(new PresentationToken(token)));
    }

    [Fact]
    public void UntrustedTextIsRenderedWithoutTerminalControlCharacters()
    {
        Assert.Equal("Aster [31m", ConsoleText.Sanitize("Aster\u001b[31m"));
        Assert.False(ConsoleText.IsSafePlayerName("Aster\nInjected"));
    }

    private static void AssertDeckProjection(
        IReadOnlyList<TestDeckSpec> decks,
        IReadOnlyList<string> expectedIds)
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(decks: decks);
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(0, "Aster")],
            new MinimumMatchRandomSource());
        ConsoleMatchProjection projection = new ConsoleProjectionBuilder().Build(game);

        Assert.Equal(expectedIds, projection.Decks.Select(deck => deck.Id));
        TestConsole console = new(string.Empty);
        new ConsoleRenderer(console).RenderDecks(projection);
        foreach (TestDeckSpec deck in decks)
        {
            Assert.Contains(deck.Id, console.Output, StringComparison.Ordinal);
            foreach (TestCardSpec card in deck.Cards)
                Assert.DoesNotContain(card.Id, console.Output, StringComparison.Ordinal);
        }
        Assert.Contains("deck order are not shown", console.Output, StringComparison.Ordinal);
    }

    private static TestCardSpec Card(string id) => new(
        id,
        [new ResourceChangeEffectDefinition(ExecutionProfileFactory.Credits, 1)]);
}
