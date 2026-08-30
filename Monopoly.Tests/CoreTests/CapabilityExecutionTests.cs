using Infrastructure.Profiles;
using Monopoly.Core.Notifications;
using Monopoly.Core.Persistence;

namespace Monopoly.Tests.CoreTests;

public sealed class CapabilityExecutionTests
{
    private static readonly string DemoPath = Path.Combine(
        AppContext.BaseDirectory,
        "TestData",
        "Profiles",
        "Demo",
        "lantern-vale-v1.json");

    [Fact]
    public void DiceMovementAppliesEveryOriginPassWithoutGivingDoublesAnExtraTurn()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 3,
            diceCount: 2,
            dieSides: 6,
            passReward: 2);
        ScriptedMatchRandomSource random = new(3, 3);
        Game game = GameSetup.Create(profile, [new PlayerSetup(7, "Solo")], random);
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);

        GameActionResult result = game.PlayTurn();

        Assert.Equal(GameActionStatus.TurnCompleted, result.Status);
        Assert.True(result.TurnResult!.WasDouble);
        Assert.Equal(new SpaceId("space.execution-0"), result.TurnResult.LandedSpace.Id);
        Assert.Equal(24, game.CurrentPlayer.Resources[ExecutionProfileFactory.Credits]);
        Assert.Equal(2, game.RoundNumber);
        Assert.Equal([RandomPurpose.TurnDice, RandomPurpose.TurnDice], random.Requests.Select(request => request.Purpose));
        Assert.Equal([0, 1], random.Requests.Select(request => request.SequenceIndex));
        PlayerMovedNotification moved = Assert.Single(notifications.OfType<PlayerMovedNotification>());
        Assert.Equal(2, moved.OriginPasses);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PurchaseDecisionAcceptsOrLeavesSpaceUnowned(bool accept)
    {
        ValidatedGameProfile profile = PurchasableProfile(price: 5, fee: 2, startingCredits: 20);
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(10, "First"), new PlayerSetup(20, "Second")],
            new ScriptedMatchRandomSource(1));
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);

        GameActionResult pending = game.PlayTurn();
        PurchaseDecision decision = Assert.IsType<PurchaseDecision>(pending.PendingDecision);
        Assert.Equal(GameActionStatus.DecisionRequired, pending.Status);
        Assert.Equal(new SpaceId("space.execution-1"), decision.SpaceId);
        Assert.Equal(game.Board.GetSpace(decision.SpaceId).PresentationToken, decision.PresentationToken);
        Assert.NotEqual(Guid.Empty, decision.DecisionId);
        Assert.Same(decision, game.PendingDecision);
        Assert.Equal([DecisionOptions.Accept, DecisionOptions.Decline], decision.AllowedResponses);
        Assert.Equal(new ResourceAmount(ExecutionProfileFactory.Credits, 5), decision.Price);
        Assert.Equal(20, game.CurrentPlayer.Resources[ExecutionProfileFactory.Credits]);
        Assert.Null(game.Ownership.BySpaceId[decision.SpaceId].OwnerPlayerId);
        Assert.Empty(notifications.OfType<ResourceChangedNotification>());
        Assert.Empty(notifications.OfType<OwnershipChangedNotification>());
        Assert.Empty(notifications.OfType<DecisionResolvedNotification>());

        GameActionResult completed = game.SubmitDecision(new DecisionResponse(
            decision.DecisionId,
            decision.PlayerId,
            accept ? DecisionOptions.Accept : DecisionOptions.Decline));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.Equal(accept ? 15 : 20, game.Players[0].Resources[ExecutionProfileFactory.Credits]);
        Assert.Equal(accept ? 10 : null, game.Ownership.BySpaceId[decision.SpaceId].OwnerPlayerId);
        Assert.Equal(20, game.CurrentPlayer.Id);
        Assert.Single(notifications.OfType<PlayerMovedNotification>());
        Assert.Single(notifications.OfType<TurnAdvancedNotification>());
        Assert.Equal(accept ? 1 : 0, notifications.OfType<OwnershipChangedNotification>().Count());
        Assert.Equal(accept ? 1 : 0, notifications.OfType<ResourceChangedNotification>().Count());
        DecisionResolvedNotification resolved = Assert.Single(notifications.OfType<DecisionResolvedNotification>());
        Assert.Equal(decision.DecisionId, resolved.DecisionId);
        Assert.Equal(decision.PlayerId, resolved.PlayerId);
        Assert.Equal(DecisionKinds.Purchase, resolved.DecisionKind);
        Assert.Equal(accept ? DecisionOptions.Accept : DecisionOptions.Decline, resolved.Response);
    }

    [Fact]
    public void InitiallyUnaffordablePurchaseRunsTheRegisteredNonPurchasePolicy()
    {
        List<PurchaseNonPurchaseReason> reasons = [];
        PurchasePolicyRegistration policy = new(
            PurchaseDeclinePolicyKind.LeaveUnowned,
            (_, reason) =>
            {
                reasons.Add(reason);
                return ProfilePolicyResult.Continue;
            },
            []);
        Game game = GameSetup.Create(
            PurchasableProfile(price: 5, fee: 2, startingCredits: 3),
            [new PlayerSetup(1, "Unaffordable")],
            new ScriptedMatchRandomSource(1),
            ExecutionRegistry(policy));

        GameActionResult result = game.PlayTurn();

        Assert.Equal(GameActionStatus.TurnCompleted, result.Status);
        Assert.Equal([PurchaseNonPurchaseReason.InsufficientResources], reasons);
        Assert.Null(game.PendingDecision);
        Assert.Equal(3, game.CurrentPlayer.Resources[ExecutionProfileFactory.Credits]);
        Assert.Null(game.Ownership.BySpaceId[new SpaceId("space.execution-1")].OwnerPlayerId);
    }

    [Fact]
    public void BundledDemoDeclineLeavesTheSpaceUnownedWithoutRequestingAnotherCapability()
    {
        ValidatedGameProfile profile = new JsonGameProfileParser().Parse(File.ReadAllBytes(DemoPath));
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(1, "Aster"), new PlayerSetup(2, "Bramble")],
            ScriptedMatchRandomSource.ForDice(1, 2));
        int lumenBefore = game.CurrentPlayer.Resources[new ResourceId("resource.lumen")];

        PurchaseDecision decision = Assert.IsType<PurchaseDecision>(game.PlayTurn().PendingDecision);
        GameActionResult result = game.SubmitDecision(new DecisionResponse(
            decision.DecisionId,
            decision.PlayerId,
            DecisionOptions.Decline));

        Assert.Equal(GameActionStatus.TurnCompleted, result.Status);
        Assert.Equal(new SpaceId("space.s004"), decision.SpaceId);
        Assert.Equal(lumenBefore, game.Players.Single(player => player.Id == 1).Resources[new ResourceId("resource.lumen")]);
        Assert.Null(game.Ownership.BySpaceId[decision.SpaceId].OwnerPlayerId);
    }

    [Theory]
    [InlineData(3, 6, 0)]
    [InlineData(10, 15, 5)]
    public void MandatoryUsageFeeTransfersAtMostTheAvailableBalance(
        int startingCredits,
        int expectedOwnerCredits,
        int expectedVisitorCredits)
    {
        ValidatedGameProfile profile = PurchasableProfile(price: 0, fee: 5, startingCredits: startingCredits);
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(1, "Owner"), new PlayerSetup(2, "Visitor")],
            new ScriptedMatchRandomSource(1, 1));
        PurchaseDecision purchase = Assert.IsType<PurchaseDecision>(game.PlayTurn().PendingDecision);
        game.SubmitDecision(new DecisionResponse(purchase.DecisionId, 1, DecisionOptions.Accept));
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);

        GameActionResult result = game.PlayTurn();

        Assert.Equal(GameActionStatus.TurnCompleted, result.Status);
        Assert.Equal(expectedOwnerCredits, game.Players.Single(player => player.Id == 1).Resources[ExecutionProfileFactory.Credits]);
        Assert.Equal(expectedVisitorCredits, game.Players.Single(player => player.Id == 2).Resources[ExecutionProfileFactory.Credits]);
        Assert.Equal(1, game.Ownership.BySpaceId[new SpaceId("space.execution-1")].OwnerPlayerId);
        Assert.Equal(2, notifications.OfType<ResourceChangedNotification>().Count());
    }

    [Fact]
    public void UsageFeeCreditOverflowLeavesBothPlayersAndTurnStateUnchanged()
    {
        Game game = GameSetup.Create(
            PurchasableProfile(price: 0, fee: 1, startingCredits: int.MaxValue),
            [new PlayerSetup(1, "Owner"), new PlayerSetup(2, "Visitor")],
            new ScriptedMatchRandomSource(1, 1));
        PurchaseDecision purchase = Assert.IsType<PurchaseDecision>(game.PlayTurn().PendingDecision);
        game.SubmitDecision(new DecisionResponse(purchase.DecisionId, 1, DecisionOptions.Accept));
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);
        string before = GameTestSnapshot.Capture(game);

        ProfileExecutionException exception = Assert.Throws<ProfileExecutionException>(() => game.PlayTurn());

        Assert.Equal(ProfileExecutionErrorKind.ResourceOverflow, exception.Kind);
        Assert.Equal(before, GameTestSnapshot.Capture(game));
        Assert.Empty(notifications);
    }

    [Fact]
    public void UnaffordableUnownedAndSelfOwnedSpacesDoNotChargeUsageFee()
    {
        Game unaffordable = GameSetup.Create(
            PurchasableProfile(price: 5, fee: 2, startingCredits: 3),
            [new PlayerSetup(1, "Unaffordable")],
            new ScriptedMatchRandomSource(1));
        Assert.Equal(GameActionStatus.TurnCompleted, unaffordable.PlayTurn().Status);
        Assert.Equal(3, unaffordable.CurrentPlayer.Resources[ExecutionProfileFactory.Credits]);

        Game selfOwned = GameSetup.Create(
            PurchasableProfile(price: 0, fee: 5, startingCredits: 8),
            [new PlayerSetup(2, "Owner")],
            new ScriptedMatchRandomSource(1, 1, 1));
        PurchaseDecision purchase = Assert.IsType<PurchaseDecision>(selfOwned.PlayTurn().PendingDecision);
        selfOwned.SubmitDecision(new DecisionResponse(purchase.DecisionId, 2, DecisionOptions.Accept));
        selfOwned.PlayTurn();
        int balanceBeforeSelfOwnedLanding = selfOwned.CurrentPlayer.Resources[ExecutionProfileFactory.Credits];
        Assert.Equal(GameActionStatus.TurnCompleted, selfOwned.PlayTurn().Status);
        Assert.Equal(balanceBeforeSelfOwnedLanding, selfOwned.CurrentPlayer.Resources[ExecutionProfileFactory.Credits]);
    }

    [Fact]
    public void DrawRotatesCardsAndAppliesOrderedBoundedResourceChanges()
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
                    new TestCardSpec("card.execution-one", [new ResourceChangeEffectDefinition(ExecutionProfileFactory.Score, 5)]),
                    new TestCardSpec("card.execution-two", [new ResourceChangeEffectDefinition(ExecutionProfileFactory.Credits, -100)])
                ])
            ],
            startingCredits: 7);
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(1, "First"), new PlayerSetup(2, "Second")],
            new ScriptedMatchRandomSource(1, 1, 1));
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);

        Assert.Equal(GameActionStatus.TurnCompleted, game.PlayTurn().Status);
        Assert.Equal(5, game.Players[0].Resources[ExecutionProfileFactory.Score]);
        Assert.Equal([new CardId("card.execution-two"), new CardId("card.execution-one")],
            game.Decks.Resolve(deckId).Cards.Select(card => card.Id));
        Assert.Single(notifications.OfType<CardDrawnNotification>());
        Assert.Single(notifications.OfType<ResourceChangedNotification>());
        Assert.Single(notifications.OfType<TurnAdvancedNotification>());

        notifications.Clear();
        Assert.Equal(GameActionStatus.TurnCompleted, game.PlayTurn().Status);
        Assert.Equal(0, game.Players[1].Resources[ExecutionProfileFactory.Credits]);
        Assert.Equal([new CardId("card.execution-one"), new CardId("card.execution-two")],
            game.Decks.Resolve(deckId).Cards.Select(card => card.Id));
    }

    [Fact]
    public void DestinationResolvingMoveUsesTheNormalLandingPipelineOnce()
    {
        DeckId deckId = new("deck.execution");
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 4,
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [1] = [new DrawCapabilityDefinition(deckId)],
                [2] =
                [
                    new OwnableCapabilityDefinition(),
                    new PurchasableCapabilityDefinition(new ResourceAmount(ExecutionProfileFactory.Credits, 0)),
                    new UsageFeeCapabilityDefinition(new ResourceAmount(ExecutionProfileFactory.Credits, 1))
                ]
            },
            decks:
            [
                new TestDeckSpec("deck.execution",
                [
                    new TestCardSpec("card.execution-move",
                    [
                        new MoveEffectDefinition(new RelativeMoveTarget(1), PassOriginPolicy.Ignore, resolveDestination: true)
                    ])
                ])
            ]);
        Game game = GameSetup.Create(profile, [new PlayerSetup(4, "Mover")], new ScriptedMatchRandomSource(1));
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);

        GameActionResult pending = game.PlayTurn();

        PurchaseDecision decision = Assert.IsType<PurchaseDecision>(pending.PendingDecision);
        Assert.Equal(new SpaceId("space.execution-2"), decision.SpaceId);
        Assert.Equal(decision.SpaceId, game.CurrentPlayer.CurrentSpaceId);
        Assert.Equal(2, notifications.OfType<PlayerMovedNotification>().Count());

        GameActionResult completed = game.SubmitDecision(new DecisionResponse(decision.DecisionId, 4, DecisionOptions.Accept));
        Assert.Equal(new SpaceId("space.execution-2"), completed.TurnResult!.LandedSpace.Id);
        Assert.Equal(4, game.Ownership.BySpaceId[decision.SpaceId].OwnerPlayerId);
    }

    [Fact]
    public void NonResolvingMoveDoesNotRunTargetCapabilities()
    {
        DeckId deckId = new("deck.execution");
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 4,
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
                new TestDeckSpec("deck.execution",
                [
                    new TestCardSpec("card.execution-move",
                    [
                        new MoveEffectDefinition(new RelativeMoveTarget(2), PassOriginPolicy.Ignore, resolveDestination: false)
                    ])
                ])
            ]);
        Game game = GameSetup.Create(profile, [new PlayerSetup(4, "Mover")], new ScriptedMatchRandomSource(1));

        GameActionResult result = game.PlayTurn();

        Assert.Equal(GameActionStatus.TurnCompleted, result.Status);
        Assert.Equal(new SpaceId("space.execution-3"), result.TurnResult!.LandedSpace.Id);
        Assert.Null(game.PendingDecision);
        Assert.Null(game.Ownership.BySpaceId[new SpaceId("space.execution-3")].OwnerPlayerId);
    }

    [Fact]
    public void AbsoluteForwardMoveUsesSpaceIdAndAppliesOneOriginReward()
    {
        DeckId deckId = new("deck.execution");
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 4,
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [3] = [new DrawCapabilityDefinition(deckId)]
            },
            decks:
            [
                new TestDeckSpec("deck.execution",
                [
                    new TestCardSpec("card.execution-absolute",
                    [
                        new MoveEffectDefinition(
                            new AbsoluteMoveTarget(new SpaceId("space.execution-1")),
                            PassOriginPolicy.ApplyProfileReward,
                            resolveDestination: false)
                    ])
                ])
            ],
            passReward: 3);
        Game game = GameSetup.Create(profile, [new PlayerSetup(1, "Absolute")], new ScriptedMatchRandomSource(3));

        GameActionResult result = game.PlayTurn();

        Assert.Equal(new SpaceId("space.execution-1"), result.TurnResult!.LandedSpace.Id);
        Assert.Equal(23, game.CurrentPlayer.Resources[ExecutionProfileFactory.Credits]);
    }

    [Fact]
    public void PositiveOverflowLeavesTheWholeTurnUncommitted()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 2,
            startingCredits: int.MaxValue,
            dieSides: 2,
            passReward: 1);
        ScriptedMatchRandomSource random = new(2);
        Game game = GameSetup.Create(profile, [new PlayerSetup(1, "Overflow")], random);
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);
        string before = GameTestSnapshot.Capture(game);

        ProfileExecutionException exception = Assert.Throws<ProfileExecutionException>(() => game.PlayTurn());

        Assert.Equal(ProfileExecutionErrorKind.ResourceOverflow, exception.Kind);
        Assert.Equal(before, GameTestSnapshot.Capture(game));
        Assert.Empty(notifications);
        Assert.Single(random.Requests);
    }

    [Fact]
    public void ExhaustedMultiDieRollLeavesMatchStateAndNotificationsUnchanged()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(diceCount: 2);
        ScriptedMatchRandomSource random = new(2);
        Game game = GameSetup.Create(profile, [new PlayerSetup(1, "Atomic")], random);
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);
        string before = GameTestSnapshot.Capture(game);

        RandomSourceException exception = Assert.Throws<RandomSourceException>(() => game.PlayTurn());

        Assert.Equal(RandomSourceErrorKind.Exhausted, exception.Kind);
        Assert.Equal(RandomPurpose.TurnDice, exception.Request.Purpose);
        Assert.Equal(1, exception.Request.SequenceIndex);
        Assert.Equal(before, GameTestSnapshot.Capture(game));
        Assert.Empty(notifications);
    }

    [Fact]
    public void DrawAndEarlierEffectsAreNotCommittedWhenLaterEffectOverflows()
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
                    new TestCardSpec("card.execution-overflow",
                    [
                        new ResourceChangeEffectDefinition(ExecutionProfileFactory.Score, 1),
                        new ResourceChangeEffectDefinition(ExecutionProfileFactory.Credits, 1)
                    ]),
                    new TestCardSpec("card.execution-other", [new ResourceChangeEffectDefinition(ExecutionProfileFactory.Score, 1)])
                ])
            ],
            startingCredits: int.MaxValue);
        Game game = GameSetup.Create(profile, [new PlayerSetup(1, "Atomic")], new ScriptedMatchRandomSource(1, 1));
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);
        string before = GameTestSnapshot.Capture(game);

        ProfileExecutionException exception = Assert.Throws<ProfileExecutionException>(() => game.PlayTurn());

        Assert.Equal(ProfileExecutionErrorKind.ResourceOverflow, exception.Kind);
        Assert.Equal(before, GameTestSnapshot.Capture(game));
        Assert.Empty(notifications);
    }

    [Fact]
    public void RoundLimitChoosesHighestScoreThenLowestPlayerId()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(spaceCount: 1, roundLimit: 1, passReward: 0);
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(8, "Eight"), new PlayerSetup(3, "Three")],
            new ScriptedMatchRandomSource(1, 1));
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);

        Assert.Equal(GameActionStatus.TurnCompleted, game.PlayTurn().Status);
        notifications.Clear();
        GameActionResult terminal = game.PlayTurn();

        Assert.Equal(GameActionStatus.GameOver, terminal.Status);
        Assert.Equal(GamePhase.GameOver, game.Phase);
        Assert.Equal(1, game.RoundNumber);
        Assert.Equal(3, game.Winner!.Id);
        Assert.Equal(3, terminal.TurnResult!.Winner!.Id);
        Assert.Single(notifications.OfType<MatchEndedNotification>());
        Assert.Empty(notifications.OfType<TurnAdvancedNotification>());
    }

    [Fact]
    public void InvalidAndRepeatedResponsesDoNotMutatePendingState()
    {
        Game game = GameSetup.Create(
            PurchasableProfile(price: 5, fee: 1, startingCredits: 10),
            [new PlayerSetup(1, "One"), new PlayerSetup(2, "Two")],
            new ScriptedMatchRandomSource(1));
        PurchaseDecision decision = Assert.IsType<PurchaseDecision>(game.PlayTurn().PendingDecision);
        string pending = GameTestSnapshot.Capture(game);

        GameActionResult wrong = game.SubmitDecision(new DecisionResponse(decision.DecisionId, 2, DecisionOptions.Accept));
        GameActionResult stale = game.SubmitDecision(new DecisionResponse(Guid.NewGuid(), 1, DecisionOptions.Accept));

        Assert.Equal(GameActionRejectionReason.WrongPlayer, wrong.RejectionReason);
        Assert.Equal(GameActionRejectionReason.StaleDecision, stale.RejectionReason);
        Assert.Equal(pending, GameTestSnapshot.Capture(game));

        Assert.Equal(GameActionStatus.TurnCompleted, game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, 1, DecisionOptions.Decline)).Status);
        string completed = GameTestSnapshot.Capture(game);
        GameActionResult duplicate = game.SubmitDecision(new DecisionResponse(decision.DecisionId, 1, DecisionOptions.Decline));
        Assert.Equal(GameActionRejectionReason.DuplicateDecision, duplicate.RejectionReason);
        Assert.Equal(completed, GameTestSnapshot.Capture(game));
    }

    [Fact]
    public void MalformedAndDisallowedResponsesLeaveThePendingDecisionUnchanged()
    {
        Game game = GameSetup.Create(
            PurchasableProfile(price: 5, fee: 1, startingCredits: 10),
            [new PlayerSetup(1, "One")],
            new ScriptedMatchRandomSource(1));
        PurchaseDecision decision = Assert.IsType<PurchaseDecision>(game.PlayTurn().PendingDecision);
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);
        string before = GameTestSnapshot.Capture(game);

        GameActionResult malformed = game.SubmitDecision(new DecisionResponse(Guid.Empty, 1, DecisionOptions.Accept));
        GameActionResult disallowed = game.SubmitDecision(new DecisionResponse(
            decision.DecisionId,
            1,
            new DecisionOptionId("unsupported")));

        Assert.Equal(GameActionRejectionReason.MalformedResponse, malformed.RejectionReason);
        Assert.Equal(GameActionRejectionReason.ResponseNotAllowed, disallowed.RejectionReason);
        Assert.Equal(before, GameTestSnapshot.Capture(game));
        Assert.Empty(notifications);
    }

    [Fact]
    public void AcceptRejectsChangedAffordabilityWithoutConsumingTheDecision()
    {
        Game game = GameSetup.Create(
            PurchasableProfile(price: 5, fee: 1, startingCredits: 10),
            [new PlayerSetup(1, "One")],
            new ScriptedMatchRandomSource(1));
        PurchaseDecision decision = Assert.IsType<PurchaseDecision>(game.PlayTurn().PendingDecision);
        SetCredits(game.CurrentPlayer, 4);
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);
        string before = GameTestSnapshot.Capture(game);

        GameActionResult result = game.SubmitDecision(new DecisionResponse(
            decision.DecisionId,
            decision.PlayerId,
            DecisionOptions.Accept));

        Assert.Equal(GameActionRejectionReason.InsufficientResources, result.RejectionReason);
        Assert.Same(decision, game.PendingDecision);
        Assert.Equal(before, GameTestSnapshot.Capture(game));
        Assert.Empty(notifications);
    }

    [Fact]
    public void AcceptRejectsChangedDecisionPositionWithoutConsumingTheDecision()
    {
        Game game = GameSetup.Create(
            PurchasableProfile(price: 5, fee: 1, startingCredits: 10),
            [new PlayerSetup(1, "One")],
            new ScriptedMatchRandomSource(1));
        PurchaseDecision decision = Assert.IsType<PurchaseDecision>(game.PlayTurn().PendingDecision);
        game.CurrentPlayer.ApplyState(
            game.CurrentPlayer.Resources.ToDictionary(entry => entry.Key, entry => entry.Value),
            new SpaceId("space.execution-0"),
            0);
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);
        string before = GameTestSnapshot.Capture(game);

        GameActionResult result = game.SubmitDecision(new DecisionResponse(
            decision.DecisionId,
            decision.PlayerId,
            DecisionOptions.Accept));

        Assert.Equal(GameActionRejectionReason.DecisionPreconditionFailed, result.RejectionReason);
        Assert.Same(decision, game.PendingDecision);
        Assert.Equal(before, GameTestSnapshot.Capture(game));
        Assert.Empty(notifications);
    }

    [Fact]
    public void DeclineDispatchesAnIndependentlyRegisteredPolicyCapability()
    {
        CapabilityId requested = new("capability.policy-follow-up");
        int calls = 0;
        PurchasePolicyRegistration policy = RequestingPolicy(requested, [requested]);
        PolicyCapabilityRegistration capability = new(requested, _ => calls++);
        Game game = GameSetup.Create(
            PurchasableProfile(price: 5, fee: 1, startingCredits: 10),
            [new PlayerSetup(1, "One")],
            new ScriptedMatchRandomSource(1),
            ExecutionRegistry(policy, capability));
        PurchaseDecision decision = Assert.IsType<PurchaseDecision>(game.PlayTurn().PendingDecision);

        GameActionResult result = game.SubmitDecision(new DecisionResponse(
            decision.DecisionId,
            decision.PlayerId,
            DecisionOptions.Decline));

        Assert.Equal(GameActionStatus.TurnCompleted, result.Status);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void SetupRejectsDeclaredPolicyCapabilityWithoutTrustedRegistration()
    {
        CapabilityId requested = new("capability.policy-follow-up");
        PurchasePolicyRegistration policy = RequestingPolicy(requested, [requested]);

        GameSetupException exception = Assert.Throws<GameSetupException>(() => GameSetup.Create(
            PurchasableProfile(price: 5, fee: 1, startingCredits: 10),
            [new PlayerSetup(1, "One")],
            new ScriptedMatchRandomSource(1),
            ExecutionRegistry(policy)));

        Assert.Equal(GameSetupErrorKind.UnsupportedComponent, exception.Kind);
        Assert.Equal("profile.policies.purchaseDecline", exception.Path);
    }

    [Fact]
    public void UnexpectedPolicyCapabilityRequestLeavesThePendingMatchUnchanged()
    {
        CapabilityId requested = new("capability.policy-follow-up");
        PurchasePolicyRegistration policy = RequestingPolicy(requested, []);
        Game game = GameSetup.Create(
            PurchasableProfile(price: 5, fee: 1, startingCredits: 10),
            [new PlayerSetup(1, "One")],
            new ScriptedMatchRandomSource(1),
            ExecutionRegistry(policy));
        PurchaseDecision decision = Assert.IsType<PurchaseDecision>(game.PlayTurn().PendingDecision);
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);
        string before = GameTestSnapshot.Capture(game);

        ProfileExecutionException exception = Assert.Throws<ProfileExecutionException>(() => game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, decision.PlayerId, DecisionOptions.Decline)));

        Assert.Equal(ProfileExecutionErrorKind.UnsupportedExecutionShape, exception.Kind);
        Assert.Equal("policy.purchase-decline.result", exception.Path);
        Assert.Equal(before, GameTestSnapshot.Capture(game));
        Assert.Empty(notifications);
    }

    [Fact]
    public void FailingPolicyCapabilityLeavesThePendingMatchUnchanged()
    {
        CapabilityId requested = new("capability.policy-follow-up");
        PurchasePolicyRegistration policy = RequestingPolicy(requested, [requested]);
        PolicyCapabilityRegistration capability = new(
            requested,
            _ => throw new ProfileExecutionException(
                ProfileExecutionErrorKind.InvalidRuntimeState,
                "capability.policy-follow-up",
                "Synthetic policy capability failure."));
        Game game = GameSetup.Create(
            PurchasableProfile(price: 5, fee: 1, startingCredits: 10),
            [new PlayerSetup(1, "One")],
            new ScriptedMatchRandomSource(1),
            ExecutionRegistry(policy, capability));
        PurchaseDecision decision = Assert.IsType<PurchaseDecision>(game.PlayTurn().PendingDecision);
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);
        string before = GameTestSnapshot.Capture(game);

        ProfileExecutionException exception = Assert.Throws<ProfileExecutionException>(() => game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, decision.PlayerId, DecisionOptions.Decline)));

        Assert.Equal(ProfileExecutionErrorKind.InvalidRuntimeState, exception.Kind);
        Assert.Equal(before, GameTestSnapshot.Capture(game));
        Assert.Empty(notifications);
    }

    [Fact]
    public void PendingPurchaseProjectsPrimitivePersistableContinuation()
    {
        Game game = GameSetup.Create(
            PurchasableProfile(price: 5, fee: 1, startingCredits: 10),
            [new PlayerSetup(4, "Persisted")],
            new ScriptedMatchRandomSource(1));

        PurchaseDecision decision = Assert.IsType<PurchaseDecision>(game.PlayTurn().PendingDecision);
        GameProgressState state = GameProgressStateMapper.ToState(game);

        Assert.Equal(GamePhase.AwaitingDecision, state.Phase);
        Assert.Equal(decision.DecisionId, state.PendingDecision!.DecisionId);
        Assert.Equal(decision.PlayerId, state.PendingDecision.PlayerId);
        Assert.Equal(decision.AllowedResponses, state.PendingDecision.AllowedResponses);
        Assert.Equal(decision.SpaceId, state.PendingDecision.SpaceId);
        Assert.Equal(RandomPurpose.TurnDice, state.Continuation!.DicePurpose);
        Assert.Equal([1], state.Continuation.DiceResults);
        Assert.Equal(decision.SpaceId, state.Continuation.SpaceId);
        Assert.Equal(2, state.Continuation.NextCapabilityIndex);

        state.PendingDecision.AllowedResponses.Clear();
        Assert.Equal([DecisionOptions.Accept, DecisionOptions.Decline], decision.AllowedResponses);

        Assert.Equal(GameActionStatus.TurnCompleted, game.SubmitDecision(new DecisionResponse(
            decision.DecisionId,
            decision.PlayerId,
            DecisionOptions.Accept)).Status);
        GameProgressState committed = GameProgressStateMapper.ToState(game);
        Assert.Equal(GamePhase.ReadyForTurn, committed.Phase);
        Assert.Null(committed.PendingDecision);
        Assert.Null(committed.Continuation);
        Assert.Equal(decision.DecisionId, committed.LastConsumedDecisionId);
        Assert.Contains(decision.DecisionId, committed.ConsumedDecisionIds);
        Assert.Equal(5, game.Players[0].Resources[ExecutionProfileFactory.Credits]);
        Assert.Equal(decision.PlayerId, game.Ownership.BySpaceId[decision.SpaceId].OwnerPlayerId);
    }

    [Fact]
    public void TwoGamesKeepRandomnessStateAndNotificationsIsolated()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(spaceCount: 5);
        Game first = GameSetup.Create(profile, [new PlayerSetup(1, "First")], new ScriptedMatchRandomSource(1));
        Game second = GameSetup.Create(profile, [new PlayerSetup(2, "Second")], new ScriptedMatchRandomSource(2));
        List<GameNotification> firstNotifications = [];
        List<GameNotification> secondNotifications = [];
        using IDisposable firstSubscription = first.Notifications.Subscribe(firstNotifications.Add);
        using IDisposable secondSubscription = second.Notifications.Subscribe(secondNotifications.Add);

        first.PlayTurn();

        Assert.Equal(new SpaceId("space.execution-1"), first.CurrentPlayer.CurrentSpaceId);
        Assert.Equal(new SpaceId("space.execution-0"), second.CurrentPlayer.CurrentSpaceId);
        Assert.NotEmpty(firstNotifications);
        Assert.Empty(secondNotifications);

        second.PlayTurn();
        Assert.Equal(new SpaceId("space.execution-2"), second.CurrentPlayer.CurrentSpaceId);
        Assert.NotEmpty(secondNotifications);
    }

    [Fact]
    public void NotificationSubscribersCannotReenterAuthoritativeExecution()
    {
        Game game = GameSetup.Create(
            ExecutionProfileFactory.Create(spaceCount: 3),
            [new PlayerSetup(1, "Observer")],
            new ScriptedMatchRandomSource(1));
        GameActionResult? nested = null;
        using IDisposable subscription = game.Notifications.Subscribe(_ => nested ??= game.PlayTurn());

        GameActionResult outer = game.PlayTurn();

        Assert.Equal(GameActionStatus.TurnCompleted, outer.Status);
        Assert.NotNull(nested);
        Assert.Equal(GameActionStatus.Rejected, nested!.Status);
        Assert.Equal(GameActionRejectionReason.OperationInProgress, nested.RejectionReason);
        Assert.Equal(new SpaceId("space.execution-1"), game.CurrentPlayer.CurrentSpaceId);
    }

    [Fact]
    public void BundledDemoRunsDeterministicallyToTerminalWinner()
    {
        string first = RunDemo();
        string second = RunDemo();
        Assert.Equal(first, second);
    }

    private static string RunDemo()
    {
        ValidatedGameProfile profile = new JsonGameProfileParser().Parse(File.ReadAllBytes(DemoPath));
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(12, "Aster"), new PlayerSetup(4, "Bramble")],
            new MinimumMatchRandomSource());
        int completedTurns = 0;
        while (!game.IsGameOver && completedTurns < 100)
        {
            GameActionResult result = game.PlayTurn();
            if (result.Status == GameActionStatus.DecisionRequired)
            {
                PendingDecision decision = result.PendingDecision!;
                result = game.SubmitDecision(new DecisionResponse(decision.DecisionId, decision.PlayerId, DecisionOptions.Decline));
            }
            Assert.Contains(result.Status, new[] { GameActionStatus.TurnCompleted, GameActionStatus.GameOver });
            completedTurns++;
        }

        Assert.True(game.IsGameOver);
        Assert.Equal(24, completedTurns);
        Assert.Equal(12, game.RoundNumber);
        return $"{game.Winner!.Id}:{string.Join(',', game.Players.SelectMany(player => player.Resources.OrderBy(entry => entry.Key).Select(entry => entry.Value)))}";
    }

    private static ValidatedGameProfile PurchasableProfile(int price, int fee, int startingCredits) =>
        ExecutionProfileFactory.Create(
            spaceCount: 2,
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [1] =
                [
                    new OwnableCapabilityDefinition(),
                    new PurchasableCapabilityDefinition(new ResourceAmount(ExecutionProfileFactory.Credits, price)),
                    new UsageFeeCapabilityDefinition(new ResourceAmount(ExecutionProfileFactory.Credits, fee))
                ]
            },
            startingCredits: startingCredits);

    private static void SetCredits(Monopoly.Core.Models.Player player, int value)
    {
        Dictionary<ResourceId, int> resources = player.Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
        resources[ExecutionProfileFactory.Credits] = value;
        player.ApplyState(resources, player.CurrentSpaceId, player.Position);
    }

    private static PurchasePolicyRegistration RequestingPolicy(
        CapabilityId requested,
        IEnumerable<CapabilityId> declaredRequests) => new(
            PurchaseDeclinePolicyKind.LeaveUnowned,
            (_, _) => ProfilePolicyResult.RequestCapability(requested),
            declaredRequests);

    private static ProfileComponentRegistry ExecutionRegistry(
        PurchasePolicyRegistration purchasePolicy,
        params PolicyCapabilityRegistration[] policyCapabilities) => new(
            [CapabilityKinds.Move, CapabilityKinds.Ownable, CapabilityKinds.Purchasable, CapabilityKinds.UsageFee, CapabilityKinds.Draw],
            [EffectKinds.Move, EffectKinds.ResourceChange],
            [],
            [StartingPlayerPolicyKind.FixedOrder, StartingPlayerPolicyKind.Random, StartingPlayerPolicyKind.HighestRoll],
            [purchasePolicy],
            policyCapabilities,
            [MatchTieBreakPolicy.LowestPlayerId],
            supportsRoundLimitedScore: true);
}
