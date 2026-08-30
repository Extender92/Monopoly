using Monopoly.Core.Notifications;
using Monopoly.Core.Persistence;

namespace Monopoly.Tests.CoreTests;

public sealed class EffectChainExecutionTests
{
    [Fact]
    public void OrderedEffectsCommitOnceAndReportTheResolvedFinalSpace()
    {
        DeckId deckId = new("deck.chain");
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 5,
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [1] = [new DrawCapabilityDefinition(deckId)],
                [3] =
                [
                    new OwnableCapabilityDefinition(),
                    new PurchasableCapabilityDefinition(new ResourceAmount(ExecutionProfileFactory.Credits, 0))
                ]
            },
            decks:
            [
                new TestDeckSpec("deck.chain",
                [
                    new TestCardSpec("card.chain",
                    [
                        new ResourceChangeEffectDefinition(ExecutionProfileFactory.Score, 2),
                        new MoveEffectDefinition(new RelativeMoveTarget(1), PassOriginPolicy.Ignore, resolveDestination: false),
                        new ResourceChangeEffectDefinition(ExecutionProfileFactory.Credits, 3),
                        new MoveEffectDefinition(new RelativeMoveTarget(1), PassOriginPolicy.Ignore, resolveDestination: true)
                    ])
                ])
            ]);
        Game game = GameSetup.Create(profile, [new PlayerSetup(7, "Chain")], new ScriptedMatchRandomSource(1));
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);

        GameActionResult pending = game.PlayTurn();

        PurchaseDecision decision = Assert.IsType<PurchaseDecision>(pending.PendingDecision);
        Assert.Equal(new SpaceId("space.execution-3"), decision.SpaceId);
        Assert.Equal(decision.SpaceId, game.CurrentPlayer.CurrentSpaceId);
        Assert.Equal(2, game.CurrentPlayer.Resources[ExecutionProfileFactory.Score]);
        Assert.Equal(23, game.CurrentPlayer.Resources[ExecutionProfileFactory.Credits]);
        Assert.Equal(3, notifications.OfType<PlayerMovedNotification>().Count());
        Assert.Equal(2, notifications.OfType<ResourceChangedNotification>().Count());
        Assert.Single(notifications.OfType<CardDrawnNotification>());

        GameActionResult completed = game.SubmitDecision(new DecisionResponse(
            decision.DecisionId,
            decision.PlayerId,
            DecisionOptions.Decline));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.Equal(decision.SpaceId, completed.TurnResult!.LandedSpace.Id);
        Assert.Single(notifications.OfType<DecisionResolvedNotification>());
        Assert.Single(notifications.OfType<TurnAdvancedNotification>());
    }

    [Fact]
    public void AcyclicNestedDrawsAcrossDecksResolveInExecutionOrder()
    {
        DeckId firstDeck = new("deck.chain-a");
        DeckId secondDeck = new("deck.chain-b");
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 5,
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [1] = [new DrawCapabilityDefinition(firstDeck)],
                [2] = [new DrawCapabilityDefinition(secondDeck)]
            },
            decks:
            [
                new TestDeckSpec("deck.chain-a",
                [
                    new TestCardSpec("card.chain-a",
                    [
                        new MoveEffectDefinition(
                            new AbsoluteMoveTarget(new SpaceId("space.execution-2")),
                            PassOriginPolicy.Ignore,
                            resolveDestination: true)
                    ])
                ]),
                new TestDeckSpec("deck.chain-b",
                [
                    new TestCardSpec("card.chain-b",
                    [
                        new MoveEffectDefinition(
                            new AbsoluteMoveTarget(new SpaceId("space.execution-4")),
                            PassOriginPolicy.Ignore,
                            resolveDestination: true)
                    ])
                ])
            ]);
        Game game = GameSetup.Create(profile, [new PlayerSetup(3, "Nested")], new ScriptedMatchRandomSource(1));
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);

        GameActionResult result = game.PlayTurn();

        Assert.Equal(GameActionStatus.TurnCompleted, result.Status);
        Assert.Equal(new SpaceId("space.execution-4"), result.TurnResult!.LandedSpace.Id);
        Assert.Equal(
            [
                typeof(PlayerMovedNotification),
                typeof(CardDrawnNotification),
                typeof(PlayerMovedNotification),
                typeof(CardDrawnNotification),
                typeof(PlayerMovedNotification),
                typeof(TurnAdvancedNotification)
            ],
            notifications.Select(notification => notification.GetType()));
    }

    [Fact]
    public void NestedDrawFromTheSameDeckUsesItsNextCurrentCard()
    {
        DeckId deckId = new("deck.shared");
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 4,
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [1] = [new DrawCapabilityDefinition(deckId)],
                [2] = [new DrawCapabilityDefinition(deckId)]
            },
            decks:
            [
                new TestDeckSpec("deck.shared",
                [
                    new TestCardSpec("card.shared-resource",
                    [
                        new ResourceChangeEffectDefinition(ExecutionProfileFactory.Score, 4)
                    ]),
                    new TestCardSpec("card.shared-move",
                    [
                        new MoveEffectDefinition(new RelativeMoveTarget(1), PassOriginPolicy.Ignore, resolveDestination: true)
                    ])
                ])
            ]);
        Game game = GameSetup.Create(profile, [new PlayerSetup(2, "Shared")], new ScriptedMatchRandomSource(0, 1));
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);

        GameActionResult result = game.PlayTurn();

        Assert.Equal(GameActionStatus.TurnCompleted, result.Status);
        Assert.Equal(new SpaceId("space.execution-2"), result.TurnResult!.LandedSpace.Id);
        Assert.Equal(4, game.CurrentPlayer.Resources[ExecutionProfileFactory.Score]);
        Assert.Equal(
            [new CardId("card.shared-move"), new CardId("card.shared-resource")],
            notifications.OfType<CardDrawnNotification>().Select(notification => notification.Card.Id));
        Assert.Equal(
            [new CardId("card.shared-move"), new CardId("card.shared-resource")],
            game.Decks.Resolve(deckId).Cards.Select(card => card.Id));
    }

    [Fact]
    public void ResumingANestedLandingCanPauseAtASecondPurchaseDecision()
    {
        DeckId firstDeck = new("deck.decision-a");
        DeckId secondDeck = new("deck.decision-b");
        IReadOnlyList<CapabilityDefinition> purchasableWithDraw(DeckId deckId) =>
        [
            new OwnableCapabilityDefinition(),
            new PurchasableCapabilityDefinition(new ResourceAmount(ExecutionProfileFactory.Credits, 1)),
            new DrawCapabilityDefinition(deckId)
        ];
        IReadOnlyList<CapabilityDefinition> purchasable() =>
        [
            new OwnableCapabilityDefinition(),
            new PurchasableCapabilityDefinition(new ResourceAmount(ExecutionProfileFactory.Credits, 1))
        ];
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 4,
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [1] = [new DrawCapabilityDefinition(firstDeck)],
                [2] = purchasableWithDraw(secondDeck),
                [3] = purchasable()
            },
            decks:
            [
                new TestDeckSpec("deck.decision-a",
                [
                    new TestCardSpec("card.decision-a",
                    [
                        new MoveEffectDefinition(
                            new AbsoluteMoveTarget(new SpaceId("space.execution-2")),
                            PassOriginPolicy.Ignore,
                            resolveDestination: true)
                    ])
                ]),
                new TestDeckSpec("deck.decision-b",
                [
                    new TestCardSpec("card.decision-b",
                    [
                        new MoveEffectDefinition(
                            new AbsoluteMoveTarget(new SpaceId("space.execution-3")),
                            PassOriginPolicy.Ignore,
                            resolveDestination: true)
                    ])
                ])
            ]);
        Game game = GameSetup.Create(profile, [new PlayerSetup(9, "Decider")], new ScriptedMatchRandomSource(1));
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);

        PurchaseDecision first = Assert.IsType<PurchaseDecision>(game.PlayTurn().PendingDecision);
        Assert.Equal(new SpaceId("space.execution-2"), first.SpaceId);

        GameActionResult secondPending = game.SubmitDecision(new DecisionResponse(
            first.DecisionId,
            first.PlayerId,
            DecisionOptions.Accept));

        Assert.Equal(GameActionStatus.DecisionRequired, secondPending.Status);
        PurchaseDecision second = Assert.IsType<PurchaseDecision>(secondPending.PendingDecision);
        Assert.NotEqual(first.DecisionId, second.DecisionId);
        Assert.Equal(new SpaceId("space.execution-3"), second.SpaceId);
        GameProgressState progress = GameProgressStateMapper.ToState(game);
        Assert.Equal(second.DecisionId, progress.PendingDecision!.DecisionId);
        Assert.Equal(second.SpaceId, progress.Continuation!.SpaceId);
        Assert.Equal([1], progress.Continuation.DiceResults);
        Assert.Contains(first.DecisionId, progress.ConsumedDecisionIds);

        GameActionResult completed = game.SubmitDecision(new DecisionResponse(
            second.DecisionId,
            second.PlayerId,
            DecisionOptions.Decline));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.Equal(second.SpaceId, completed.TurnResult!.LandedSpace.Id);
        Assert.Equal(2, notifications.OfType<CardDrawnNotification>().Count());
        Assert.Equal(3, notifications.OfType<PlayerMovedNotification>().Count());
        Assert.Equal(2, notifications.OfType<DecisionResolvedNotification>().Count());
        Assert.Single(notifications.OfType<TurnAdvancedNotification>());
    }

    [Fact]
    public void FailureLateInNestedChainRollsBackEveryPreparedChange()
    {
        DeckId firstDeck = new("deck.atomic-a");
        DeckId secondDeck = new("deck.atomic-b");
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 3,
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [1] = [new DrawCapabilityDefinition(firstDeck)],
                [2] = [new DrawCapabilityDefinition(secondDeck)]
            },
            decks:
            [
                new TestDeckSpec("deck.atomic-a",
                [
                    new TestCardSpec("card.atomic-a",
                    [
                        new ResourceChangeEffectDefinition(ExecutionProfileFactory.Credits, 1),
                        new MoveEffectDefinition(
                            new AbsoluteMoveTarget(new SpaceId("space.execution-2")),
                            PassOriginPolicy.Ignore,
                            resolveDestination: true)
                    ])
                ]),
                new TestDeckSpec("deck.atomic-b",
                [
                    new TestCardSpec("card.atomic-b",
                    [
                        new ResourceChangeEffectDefinition(ExecutionProfileFactory.Score, 1)
                    ])
                ])
            ],
            startingScore: int.MaxValue);
        Game game = GameSetup.Create(profile, [new PlayerSetup(1, "Atomic")], new ScriptedMatchRandomSource(1));
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);
        string before = GameTestSnapshot.Capture(game);

        ProfileExecutionException exception = Assert.Throws<ProfileExecutionException>(() => game.PlayTurn());

        Assert.Equal(ProfileExecutionErrorKind.ResourceOverflow, exception.Kind);
        Assert.Equal(before, GameTestSnapshot.Capture(game));
        Assert.Empty(notifications);
    }

    [Fact]
    public void EveryMovementAppliesItsOwnOriginPolicyExactlyOnce()
    {
        DeckId deckId = new("deck.origin");
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 4,
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [3] = [new DrawCapabilityDefinition(deckId)]
            },
            decks:
            [
                new TestDeckSpec("deck.origin",
                [
                    new TestCardSpec("card.origin",
                    [
                        new MoveEffectDefinition(new RelativeMoveTarget(5), PassOriginPolicy.ApplyProfileReward, resolveDestination: false),
                        new MoveEffectDefinition(
                            new AbsoluteMoveTarget(new SpaceId("space.execution-2")),
                            PassOriginPolicy.ApplyProfileReward,
                            resolveDestination: true)
                    ])
                ])
            ],
            passReward: 2);
        Game game = GameSetup.Create(profile, [new PlayerSetup(1, "Walker")], new ScriptedMatchRandomSource(3));
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);

        GameActionResult result = game.PlayTurn();

        Assert.Equal(new SpaceId("space.execution-2"), result.TurnResult!.LandedSpace.Id);
        Assert.Equal(24, game.CurrentPlayer.Resources[ExecutionProfileFactory.Credits]);
        Assert.Equal([0, 2, 0], notifications.OfType<PlayerMovedNotification>().Select(notification => notification.OriginPasses));
    }

    [Theory]
    [InlineData(int.MaxValue, 0)]
    [InlineData(int.MinValue, 1)]
    public void RelativeMovementSupportsTheWholeNonZeroIntRange(int offset, int expectedIndex)
    {
        DeckId deckId = new("deck.offset");
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 4,
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [1] = [new DrawCapabilityDefinition(deckId)]
            },
            decks:
            [
                new TestDeckSpec("deck.offset",
                [
                    new TestCardSpec("card.offset",
                    [
                        new MoveEffectDefinition(new RelativeMoveTarget(offset), PassOriginPolicy.Ignore, resolveDestination: false)
                    ])
                ])
            ]);
        Game game = GameSetup.Create(profile, [new PlayerSetup(1, "Offset")], new ScriptedMatchRandomSource(1));

        GameActionResult result = game.PlayTurn();

        Assert.Equal(new SpaceId($"space.execution-{expectedIndex}"), result.TurnResult!.LandedSpace.Id);
    }

    [Fact]
    public void ZeroRelativeMovementAndMissingAbsoluteTargetAreRejectedBeforeSetup()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RelativeMoveTarget(0));

        DeckId deckId = new("deck.missing");
        ProfileValidationException exception = Assert.Throws<ProfileValidationException>(() =>
            ExecutionProfileFactory.Create(
                spaceCount: 2,
                spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
                {
                    [1] = [new DrawCapabilityDefinition(deckId)]
                },
                decks:
                [
                    new TestDeckSpec("deck.missing",
                    [
                        new TestCardSpec("card.missing",
                        [
                            new MoveEffectDefinition(
                                new AbsoluteMoveTarget(new SpaceId("space.not-on-track")),
                                PassOriginPolicy.Ignore,
                                resolveDestination: true)
                        ])
                    ])
                ]));

        Assert.Equal(ProfileValidationErrorKind.BrokenReference, exception.Kind);
        Assert.Equal("ruleGraph", exception.Path);
    }

    [Fact]
    public void TwoSpaceAndLongerMixedCyclesAreRejectedDeterministically()
    {
        ValidatedGameProfile twoSpace = ChainProfile(
            (1, "deck.a", new AbsoluteMoveTarget(new SpaceId("space.execution-2"))),
            (2, "deck.b", new AbsoluteMoveTarget(new SpaceId("space.execution-1"))));
        ValidatedGameProfile longer = ChainProfile(
            (1, "deck.a", new AbsoluteMoveTarget(new SpaceId("space.execution-2"))),
            (2, "deck.b", new RelativeMoveTarget(1)),
            (3, "deck.c", new AbsoluteMoveTarget(new SpaceId("space.execution-1"))));

        GameSetupException twoSpaceError = Assert.Throws<GameSetupException>(() =>
            GameSetup.Create(twoSpace, [new PlayerSetup(1, "Blocked")], new MinimumMatchRandomSource()));
        GameSetupException longerError = Assert.Throws<GameSetupException>(() =>
            GameSetup.Create(longer, [new PlayerSetup(1, "Blocked")], new MinimumMatchRandomSource()));

        Assert.Equal(GameSetupErrorKind.UnsupportedComponent, twoSpaceError.Kind);
        Assert.Equal("profile.decks[1].cards[0].effects[0]", twoSpaceError.Path);
        Assert.Contains("space.execution-1 -> space.execution-2 -> space.execution-1", twoSpaceError.Message, StringComparison.Ordinal);
        Assert.Equal(GameSetupErrorKind.UnsupportedComponent, longerError.Kind);
        Assert.Contains("space.execution-1 -> space.execution-2 -> space.execution-3 -> space.execution-1", longerError.Message, StringComparison.Ordinal);
    }

    private static ValidatedGameProfile ChainProfile(params (int SpaceIndex, string DeckId, MoveTarget Target)[] links)
    {
        Dictionary<int, IReadOnlyList<CapabilityDefinition>> capabilities = links.ToDictionary(
            link => link.SpaceIndex,
            link => (IReadOnlyList<CapabilityDefinition>)[new DrawCapabilityDefinition(new DeckId(link.DeckId))]);
        TestDeckSpec[] decks = links.Select((link, index) => new TestDeckSpec(
            link.DeckId,
            [
                new TestCardSpec($"card.cycle-{index}",
                [
                    new MoveEffectDefinition(link.Target, PassOriginPolicy.Ignore, resolveDestination: true)
                ])
            ])).ToArray();
        return ExecutionProfileFactory.Create(
            spaceCount: links.Max(link => link.SpaceIndex) + 1,
            spaceCapabilities: capabilities,
            decks: decks);
    }
}
