using System.Text.Json;
using Infrastructure.Profiles;
using Monopoly.Core.Interface;
using Monopoly.Core.Notifications;
using Monopoly.Core.Persistence;
using Monopoly.Core.Presentation;

namespace Monopoly.Tests.ScenarioTests;

public sealed class DemoSetupToWinnerScenarioTests
{
    private const int AsterId = 12;
    private const int BrambleId = 4;
    private static readonly ResourceId Lumen = new("resource.lumen");
    private static readonly ResourceId Renown = new("resource.renown");
    private static readonly DeckId ValeMessages = new("deck.d001");
    private static readonly string DemoPath = Path.Combine(
        AppContext.BaseDirectory,
        "TestData",
        "Profiles",
        "Demo",
        "lantern-vale-v1.json");
    private static readonly string SyntheticProfilesPath = Path.Combine(
        AppContext.BaseDirectory,
        "TestData",
        "Profiles");

    [Fact]
    public void BundledDemoRunsDeterministicallyFromSetupToOneWinner()
    {
        ScenarioResult first = RunScenario();
        ScenarioResult second = RunScenario();

        Assert.Equal(first.SemanticTranscript, second.SemanticTranscript);
    }

    [Fact]
    public void TrackedProfilesProveDifferentTrackAndDeckStructures()
    {
        JsonGameProfileParser parser = new();
        ValidatedGameProfile demo = parser.Parse(File.ReadAllBytes(DemoPath));
        ValidatedGameProfile zeroDecks = parser.Parse(File.ReadAllBytes(
            Path.Combine(SyntheticProfilesPath, "synthetic-zero-decks-v1.json")));
        ValidatedGameProfile multipleDecks = parser.Parse(File.ReadAllBytes(
            Path.Combine(SyntheticProfilesPath, "synthetic-multi-decks-v1.json")));

        Assert.Equal((27, 1), (demo.RuleGraph.Track.Count, demo.RuleGraph.Decks.Count));
        Assert.Equal((1, 0), (zeroDecks.RuleGraph.Track.Count, zeroDecks.RuleGraph.Decks.Count));
        Assert.Equal((4, 2), (multipleDecks.RuleGraph.Track.Count, multipleDecks.RuleGraph.Decks.Count));
        Assert.Equal(3, new[]
        {
            (demo.RuleGraph.Track.Count, demo.RuleGraph.Decks.Count),
            (zeroDecks.RuleGraph.Track.Count, zeroDecks.RuleGraph.Decks.Count),
            (multipleDecks.RuleGraph.Track.Count, multipleDecks.RuleGraph.Decks.Count)
        }.Distinct().Count());
    }

    private static ScenarioResult RunScenario()
    {
        ValidatedGameProfile profile = new JsonGameProfileParser().Parse(File.ReadAllBytes(DemoPath));
        TurnStep[] steps = CreateSteps();
        ScriptedMatchRandomSource random = ScriptedMatchRandomSource.ForDice(
            steps.SelectMany(step => new[] { step.FirstDie, step.SecondDie }).ToArray());
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(AsterId, "Aster"), new PlayerSetup(BrambleId, "Bramble")],
            random);
        IGame frontendGame = game;
        List<GameNotification> notifications = [];
        List<string> committedStates = [];
        DecisionIdNormalizer decisionIds = new();
        using IDisposable subscription = frontendGame.Notifications.Subscribe(notifications.Add);

        AssertInitialState(frontendGame, profile);
        AddValidatedState(frontendGame, committedStates, decisionIds);

        for (int index = 0; index < steps.Length; index++)
        {
            TurnStep step = steps[index];
            Assert.Equal(step.ActorPlayerId, frontendGame.CurrentPlayer.Id);
            GameActionResult action = frontendGame.PlayTurn();

            Assert.Equal([step.FirstDie, step.SecondDie], frontendGame.LastDiceRoll!.Results);
            AddValidatedState(frontendGame, committedStates, decisionIds);

            if (step.Response is DecisionOptionId response)
            {
                PurchaseDecision decision = Assert.IsType<PurchaseDecision>(action.PendingDecision);
                Assert.Equal(GameActionStatus.DecisionRequired, action.Status);
                Assert.Same(decision, frontendGame.PendingDecision);
                Assert.Equal(step.ExpectedFinalSpaceId, decision.SpaceId);

                GameStateV2 pendingState = GameStateV2Mapper.Capture(game);
                Assert.Equal(decision.DecisionId, pendingState.PendingDecision!.DecisionId);
                Assert.Equal(decision.PlayerId, pendingState.PendingDecision.PlayerId);
                Assert.Equal(decision.SpaceId, pendingState.PendingDecision.SpaceId);
                Assert.Equal(decision.PlayerId, pendingState.Continuation!.PlayerId);
                Assert.Equal(decision.SpaceId, pendingState.Continuation.SpaceId);
                _ = decisionIds.Normalize(decision.DecisionId);

                action = frontendGame.SubmitDecision(new DecisionResponse(
                    decision.DecisionId,
                    decision.PlayerId,
                    response));
                AddValidatedState(frontendGame, committedStates, decisionIds);

                if (index == 0)
                    AssertDuplicateDecisionIsMutationFree(game, frontendGame, decision, response, notifications);
            }
            else
            {
                Assert.Null(action.PendingDecision);
            }

            GameActionStatus expectedStatus = index == steps.Length - 1
                ? GameActionStatus.GameOver
                : GameActionStatus.TurnCompleted;
            Assert.Equal(expectedStatus, action.Status);
            Assert.Equal(step.ActorPlayerId, action.TurnResult!.Player.Id);
            Assert.Equal(step.ExpectedFinalSpaceId, action.TurnResult.LandedSpace.Id);
            Assert.Equal(step.ExpectedFinalSpaceId, frontendGame.Players.Single(player => player.Id == step.ActorPlayerId).CurrentSpaceId);
            Assert.Equal(index == steps.Length - 1 ? 12 : 1 + ((index + 1) / 2), frontendGame.RoundNumber);
        }

        AssertTerminalState(frontendGame, profile, random, notifications);
        string semanticTranscript = JsonSerializer.Serialize(new
        {
            States = committedStates,
            Notifications = notifications.Select(notification => NotificationSignature(notification, decisionIds)),
            RandomRequests = random.Requests.Select(request =>
                $"{request.Purpose}|{request.MinimumInclusive}|{request.MaximumExclusive}|{request.SequenceIndex}")
        });
        return new ScenarioResult(semanticTranscript);
    }

    private static void AssertInitialState(IGame game, ValidatedGameProfile profile)
    {
        Assert.Equal(new ProfileId("profile.demo-001"), profile.Id);
        Assert.Equal(new ProfileRevision(1), profile.Revision);
        Assert.Equal(
            new ProfileFingerprint("7ba140a86da1a20222f2580b7419ca7e3f52d7a392bcadf9269ed1fe5a456c7d"),
            profile.Fingerprint);
        Assert.Equal(
            [CapabilityKinds.Move],
            profile.RuleGraph.ProfileCapabilities.Entries.Select(capability => capability.Id));
        Assert.Equal(
            [CapabilityKinds.Draw, CapabilityKinds.Ownable, CapabilityKinds.Purchasable, CapabilityKinds.UsageFee],
            profile.RuleGraph.Spaces.SelectMany(space => space.Capabilities.Entries)
                .Select(capability => capability.Id)
                .Distinct()
                .Order());
        Assert.Equal(
            [EffectKinds.Move, EffectKinds.ResourceChange],
            profile.RuleGraph.Decks.SelectMany(deck => deck.Cards)
                .SelectMany(card => card.Effects.Entries)
                .Select(effect => effect.Kind)
                .Distinct()
                .Order());
        Assert.Empty(profile.RuleGraph.Statuses);
        Assert.Equal(GamePhase.ReadyForTurn, game.Phase);
        Assert.Equal(1, game.RoundNumber);
        Assert.Equal(AsterId, game.CurrentPlayer.Id);
        Assert.Null(game.LastDiceRoll);
        Assert.Null(game.PendingDecision);
        Assert.Null(game.Winner);
        Assert.All(game.Players, player =>
        {
            Assert.Equal(new SpaceId("space.s001"), player.CurrentSpaceId);
            Assert.Equal(120, player.Resources[Lumen]);
            Assert.Equal(0, player.Resources[Renown]);
        });
        Assert.Equal(
            Enumerable.Range(2, 8).Append(1).Select(index => new CardId($"card.c{index:000}")),
            game.Decks.Resolve(ValeMessages).Cards.Select(card => card.Id));
    }

    private static void AssertDuplicateDecisionIsMutationFree(
        Game game,
        IGame frontendGame,
        PurchaseDecision decision,
        DecisionOptionId response,
        IReadOnlyCollection<GameNotification> notifications)
    {
        string before = JsonSerializer.Serialize(GameStateV2Mapper.Capture(game));
        int notificationCount = notifications.Count;

        GameActionResult duplicate = frontendGame.SubmitDecision(new DecisionResponse(
            decision.DecisionId,
            decision.PlayerId,
            response));

        Assert.Equal(GameActionStatus.Rejected, duplicate.Status);
        Assert.Equal(GameActionRejectionReason.DuplicateDecision, duplicate.RejectionReason);
        Assert.Equal(before, JsonSerializer.Serialize(GameStateV2Mapper.Capture(game)));
        Assert.Equal(notificationCount, notifications.Count);
    }

    private static void AddValidatedState(
        IGame game,
        ICollection<string> committedStates,
        DecisionIdNormalizer decisionIds)
    {
        Game concreteGame = Assert.IsType<Game>(game);
        GameStateV2 state = GameStateV2Mapper.Capture(concreteGame);

        Assert.Equal(game.Profile.Id, state.ProfileId);
        Assert.Equal(game.Profile.Revision, state.ProfileRevision);
        Assert.Equal(game.Profile.Fingerprint, state.ProfileFingerprint);
        Assert.Equal(game.CurrentPlayer.Id, state.CurrentPlayerId);
        Assert.Equal(AsterId, state.RoundAnchorPlayerId);
        Assert.Equal(game.RoundNumber, state.RoundNumber);
        Assert.Equal(game.Phase, state.Phase);
        Assert.Equal(game.Winner?.Id, state.WinnerPlayerId);
        Assert.Equal(
            game.LastDiceRoll?.Results,
            state.LastDiceRoll?.Results);
        Assert.Equal(
            game.Players.Select(player => (player.Id, player.Name, player.CurrentSpaceId)),
            state.Players.Select(player => (player.PlayerId, player.Name, player.SpaceId)));
        Assert.Equal(
            game.Players.Select(player => player.Resources.OrderBy(resource => resource.Key).ToArray()),
            state.Players.Select(player => player.Resources.Select(resource =>
                new KeyValuePair<ResourceId, int>(resource.ResourceId, resource.Value)).ToArray()));
        Assert.Equal(
            game.Decks.Entries.Select(deck => (deck.Id, Cards: string.Join(',', deck.Cards.Select(card => card.Id)))),
            state.Decks.Select(deck => (deck.DeckId, Cards: string.Join(',', deck.CardIds))));
        Assert.Equal(
            game.Ownership.Entries.Select(entry => (entry.SpaceId, entry.OwnerPlayerId)),
            state.ModuleState.Ownership.Select(entry => (entry.SpaceId, entry.OwnerPlayerId)));
        Assert.Equal(game.Statuses.Count, state.ModuleState.Statuses.Count);

        if (game.PendingDecision is PurchaseDecision purchase)
        {
            Assert.NotNull(state.PendingDecision);
            Assert.NotNull(state.Continuation);
            Assert.Equal(purchase.DecisionId, state.PendingDecision!.DecisionId);
            Assert.Equal(purchase.Kind, state.PendingDecision.Kind);
            Assert.Equal(purchase.PlayerId, state.PendingDecision.PlayerId);
            Assert.Equal(purchase.AllowedResponses, state.PendingDecision.AllowedResponses);
            Assert.Equal(purchase.SpaceId, state.PendingDecision.SpaceId);
            Assert.Equal(purchase.Price.ResourceId, state.PendingDecision.ResourceId);
            Assert.Equal(purchase.Price.Value, state.PendingDecision.ResourceAmount);
            Assert.Equal(purchase.PlayerId, state.Continuation!.PlayerId);
            Assert.Equal(purchase.SpaceId, state.Continuation.SpaceId);
            _ = decisionIds.Normalize(purchase.DecisionId);
        }
        else
        {
            Assert.Null(state.PendingDecision);
            Assert.Null(state.Continuation);
        }

        committedStates.Add(StateSignature(state, decisionIds));
    }

    private static void AssertTerminalState(
        IGame game,
        ValidatedGameProfile profile,
        ScriptedMatchRandomSource random,
        IReadOnlyList<GameNotification> notifications)
    {
        Assert.Equal(GamePhase.GameOver, game.Phase);
        Assert.True(game.IsGameOver);
        Assert.Equal(12, game.RoundNumber);
        Assert.Equal(BrambleId, game.CurrentPlayer.Id);
        Assert.Equal(BrambleId, game.Winner!.Id);
        Assert.Null(game.PendingDecision);
        Assert.Equal([1, 1], game.LastDiceRoll!.Results);

        Player aster = game.Players.Single(player => player.Id == AsterId);
        Player bramble = game.Players.Single(player => player.Id == BrambleId);
        Assert.Equal(new SpaceId("space.s010"), aster.CurrentSpaceId);
        Assert.Equal(113, aster.Resources[Lumen]);
        Assert.Equal(0, aster.Resources[Renown]);
        Assert.Equal(new SpaceId("space.s013"), bramble.CurrentSpaceId);
        Assert.Equal(146, bramble.Resources[Lumen]);
        Assert.Equal(6, bramble.Resources[Renown]);

        Assert.Equal(AsterId, game.Ownership.BySpaceId[new SpaceId("space.s004")].OwnerPlayerId);
        Assert.All(
            game.Ownership.Entries.Where(entry => entry.SpaceId != new SpaceId("space.s004")),
            entry => Assert.Null(entry.OwnerPlayerId));
        Assert.Equal(
            Enumerable.Range(3, 7).Concat([1, 2]).Select(index => new CardId($"card.c{index:000}")),
            game.Decks.Resolve(ValeMessages).Cards.Select(card => card.Id));

        Assert.Equal(8, random.Requests.Count(request => request.Purpose == RandomPurpose.DeckShuffle));
        Assert.Equal(48, random.Requests.Count(request => request.Purpose == RandomPurpose.TurnDice));
        Assert.DoesNotContain(random.Requests, request =>
            request.Purpose is RandomPurpose.SetupDice or RandomPurpose.SetupStartingPlayer);
        Assert.Equal(
            Enumerable.Range(0, 8),
            random.Requests.Where(request => request.Purpose == RandomPurpose.DeckShuffle)
                .Select(request => request.SequenceIndex));
        Assert.All(
            random.Requests.Where(request => request.Purpose == RandomPurpose.TurnDice),
            request =>
            {
                Assert.Equal(1, request.MinimumInclusive);
                Assert.Equal(9, request.MaximumExclusive);
                Assert.InRange(request.SequenceIndex, 0, 1);
            });

        Assert.Equal(
            [2, 3, 4, 5, 6, 7, 8, 9, 1, 2],
            notifications.OfType<CardDrawnNotification>()
                .Select(notification => int.Parse(notification.Card.Id.Value[^3..])));
        Assert.Equal(28, notifications.Count(notification => notification is PlayerMovedNotification));
        Assert.Equal(14, notifications.Count(notification => notification is ResourceChangedNotification));
        Assert.Single(notifications.OfType<OwnershipChangedNotification>());
        Assert.Equal(9, notifications.Count(notification => notification is DecisionResolvedNotification));
        Assert.Equal(10, notifications.Count(notification => notification is CardDrawnNotification));
        Assert.Equal(23, notifications.Count(notification => notification is TurnAdvancedNotification));
        MatchEndedNotification ended = Assert.Single(notifications.OfType<MatchEndedNotification>());
        Assert.Equal(BrambleId, ended.WinnerPlayerId);
        Assert.Equal(12, ended.RoundNumber);
        Assert.Equal(Renown, ended.ScoreResourceId);
        Assert.Equal(profile.PresentationToken, ended.PresentationToken);

        AssertNotificationOrdering(notifications);
    }

    private static void AssertNotificationOrdering(IReadOnlyList<GameNotification> notifications)
    {
        Assert.Equal(
            [
                (AsterId, Lumen, 120, 89),
                (BrambleId, Lumen, 120, 113),
                (AsterId, Lumen, 89, 96),
                (BrambleId, Lumen, 113, 123),
                (AsterId, Lumen, 96, 89),
                (AsterId, Lumen, 89, 101),
                (BrambleId, Renown, 0, 2),
                (BrambleId, Lumen, 123, 128),
                (BrambleId, Lumen, 128, 124),
                (BrambleId, Renown, 2, 3),
                (BrambleId, Lumen, 124, 136),
                (BrambleId, Renown, 3, 6),
                (AsterId, Lumen, 101, 113),
                (BrambleId, Lumen, 136, 146)
            ],
            notifications.OfType<ResourceChangedNotification>().Select(notification =>
                (notification.PlayerId, notification.ResourceId, notification.PreviousValue, notification.CurrentValue)));

        OwnershipChangedNotification ownership = Assert.Single(notifications.OfType<OwnershipChangedNotification>());
        Assert.Equal(new SpaceId("space.s004"), ownership.SpaceId);
        Assert.Null(ownership.PreviousOwnerPlayerId);
        Assert.Equal(AsterId, ownership.CurrentOwnerPlayerId);
        Assert.Equal(new PresentationToken("space.glasswing-atelier"), ownership.PresentationToken);
        DecisionResolvedNotification accepted = notifications.OfType<DecisionResolvedNotification>()
            .Single(notification => notification.Response == DecisionOptions.Accept);
        Assert.Equal(AsterId, accepted.PlayerId);
        Assert.Equal(DecisionKinds.Purchase, accepted.DecisionKind);
        Assert.Equal(
            [
                (AsterId, DecisionOptions.Accept),
                (AsterId, DecisionOptions.Decline),
                (BrambleId, DecisionOptions.Decline),
                (AsterId, DecisionOptions.Decline),
                (AsterId, DecisionOptions.Decline),
                (AsterId, DecisionOptions.Decline),
                (BrambleId, DecisionOptions.Decline),
                (AsterId, DecisionOptions.Decline),
                (BrambleId, DecisionOptions.Decline)
            ],
            notifications.OfType<DecisionResolvedNotification>()
                .Select(notification => (notification.PlayerId, notification.Response)));
        int ownershipIndex = FindNotificationIndex(notifications, ownership);
        int acceptedIndex = FindNotificationIndex(notifications, accepted);
        Assert.IsType<ResourceChangedNotification>(notifications[ownershipIndex - 1]);
        Assert.Equal(ownershipIndex + 1, acceptedIndex);
        Assert.IsType<TurnAdvancedNotification>(notifications[acceptedIndex + 1]);

        CardDrawnNotification relativeMove = notifications.OfType<CardDrawnNotification>()
            .Single(notification => notification.Card.Id == new CardId("card.c004"));
        int relativeIndex = FindNotificationIndex(notifications, relativeMove);
        PlayerMovedNotification resolvedMove = Assert.IsType<PlayerMovedNotification>(notifications[relativeIndex + 1]);
        Assert.Equal(new SpaceId("space.s015"), resolvedMove.ToSpaceId);

        CardDrawnNotification resourceCard = notifications.OfType<CardDrawnNotification>()
            .Single(notification => notification.Card.Id == new CardId("card.c006"));
        int resourceCardIndex = FindNotificationIndex(notifications, resourceCard);
        Assert.IsType<ResourceChangedNotification>(notifications[resourceCardIndex + 1]);
        Assert.IsType<ResourceChangedNotification>(notifications[resourceCardIndex + 2]);
        Assert.IsType<TurnAdvancedNotification>(notifications[resourceCardIndex + 3]);

        Assert.Equal(
            Enumerable.Range(0, 23).Select(index =>
                (CurrentPlayerId: index % 2 == 0 ? BrambleId : AsterId, RoundNumber: 1 + ((index + 1) / 2))),
            notifications.OfType<TurnAdvancedNotification>()
                .Select(notification => (notification.CurrentPlayerId, notification.RoundNumber)));

        PlayerMovedNotification[] movements = notifications.OfType<PlayerMovedNotification>().ToArray();
        Assert.Contains(movements, notification =>
            notification.PlayerId == AsterId &&
            notification.FromSpaceId == new SpaceId("space.s016") &&
            notification.ToSpaceId == new SpaceId("space.s004") &&
            notification.OriginPasses == 1);
        Assert.Contains(movements, notification =>
            notification.PlayerId == AsterId &&
            notification.FromSpaceId == new SpaceId("space.s007") &&
            notification.ToSpaceId == new SpaceId("space.s005") &&
            notification.OriginPasses == 0);
        Assert.Contains(movements, notification =>
            notification.PlayerId == AsterId &&
            notification.FromSpaceId == new SpaceId("space.s007") &&
            notification.ToSpaceId == new SpaceId("space.s024") &&
            notification.OriginPasses == 0);

        Assert.IsType<MatchEndedNotification>(notifications[^1]);
    }

    private static int FindNotificationIndex(
        IReadOnlyList<GameNotification> notifications,
        GameNotification expected)
    {
        for (int index = 0; index < notifications.Count; index++)
        {
            if (ReferenceEquals(notifications[index], expected))
                return index;
        }

        throw new Xunit.Sdk.XunitException("The expected notification was not published.");
    }

    private static string StateSignature(GameStateV2 state, DecisionIdNormalizer decisionIds) =>
        JsonSerializer.Serialize(new
        {
            state.FormatVersion,
            ProfileId = state.ProfileId.Value,
            Revision = state.ProfileRevision.Value,
            Fingerprint = state.ProfileFingerprint.Value,
            Players = state.Players.Select(player => new
            {
                player.PlayerId,
                player.Name,
                SpaceId = player.SpaceId.Value,
                Resources = player.Resources.Select(resource => new
                {
                    ResourceId = resource.ResourceId.Value,
                    resource.Value
                })
            }),
            state.CurrentPlayerId,
            state.RoundAnchorPlayerId,
            state.RoundNumber,
            state.Phase,
            LastDiceRoll = state.LastDiceRoll is null
                ? null
                : new { state.LastDiceRoll.Purpose, state.LastDiceRoll.Results },
            state.WinnerPlayerId,
            Decks = state.Decks.Select(deck => new
            {
                DeckId = deck.DeckId.Value,
                Cards = deck.CardIds.Select(card => card.Value)
            }),
            Ownership = state.ModuleState.Ownership.Select(entry => new
            {
                SpaceId = entry.SpaceId.Value,
                entry.OwnerPlayerId
            }),
            Statuses = state.ModuleState.Statuses.Select(entry => new
            {
                entry.PlayerId,
                StatusId = entry.StatusId.Value,
                entry.Value
            }),
            PendingDecision = state.PendingDecision is null
                ? null
                : new
                {
                    DecisionId = decisionIds.Normalize(state.PendingDecision.DecisionId),
                    Kind = state.PendingDecision.Kind.Value,
                    state.PendingDecision.PlayerId,
                    Responses = state.PendingDecision.AllowedResponses.Select(response => response.Value),
                    SpaceId = state.PendingDecision.SpaceId.Value,
                    ResourceId = state.PendingDecision.ResourceId.Value,
                    state.PendingDecision.ResourceAmount
                },
            state.Continuation,
            ConsumedDecisionIds = state.ConsumedDecisionIds
                .Select(decisionIds.Normalize)
                .Order(StringComparer.Ordinal),
            LastConsumedDecisionId = state.LastConsumedDecisionId is Guid consumed
                ? decisionIds.Normalize(consumed)
                : null
        });

    private static string NotificationSignature(GameNotification notification, DecisionIdNormalizer decisionIds) =>
        notification switch
        {
            PlayerMovedNotification moved =>
                $"move|{moved.PlayerId}|{moved.FromSpaceId}|{moved.ToSpaceId}|{moved.OriginPasses}|{moved.PresentationToken}",
            ResourceChangedNotification resource =>
                $"resource|{resource.PlayerId}|{resource.ResourceId}|{resource.PreviousValue}|{resource.CurrentValue}|{resource.PresentationToken}",
            OwnershipChangedNotification ownership =>
                $"ownership|{ownership.SpaceId}|{ownership.PreviousOwnerPlayerId}|{ownership.CurrentOwnerPlayerId}|{ownership.PresentationToken}",
            DecisionResolvedNotification decision =>
                $"decision|{decisionIds.Normalize(decision.DecisionId)}|{decision.PlayerId}|{decision.DecisionKind}|{decision.Response}|{decision.PresentationToken}",
            CardDrawnNotification card =>
                $"card|{card.DeckId}|{card.Card.Id}|{card.Card.PresentationToken}|{card.PresentationToken}",
            TurnAdvancedNotification turn =>
                $"turn|{turn.CurrentPlayerId}|{turn.RoundNumber}|{turn.PresentationToken}",
            MatchEndedNotification ended =>
                $"winner|{ended.WinnerPlayerId}|{ended.RoundNumber}|{ended.ScoreResourceId}|{ended.PresentationToken}",
            LogAddedNotification log =>
                $"log|{log.Log.Info}|{log.PresentationToken}",
            _ => throw new InvalidOperationException($"Unexpected notification type '{notification.GetType().Name}'.")
        };

    private static TurnStep[] CreateSteps() =>
    [
        new(AsterId, 1, 2, "space.s004", DecisionOptions.Accept),
        new(BrambleId, 1, 2, "space.s004"),
        new(AsterId, 1, 1, "space.s006", DecisionOptions.Decline),
        new(BrambleId, 1, 2, "space.s007"),
        new(AsterId, 1, 5, "space.s012"),
        new(BrambleId, 1, 4, "space.s015", DecisionOptions.Decline),
        new(AsterId, 1, 3, "space.s004"),
        new(BrambleId, 1, 5, "space.s021"),
        new(AsterId, 1, 2, "space.s005"),
        new(BrambleId, 1, 4, "space.s026"),
        new(AsterId, 1, 1, "space.s024"),
        new(BrambleId, 1, 1, "space.s001"),
        new(AsterId, 1, 2, "space.s027"),
        new(BrambleId, 1, 1, "space.s003"),
        new(AsterId, 1, 1, "space.s002", DecisionOptions.Decline),
        new(BrambleId, 1, 1, "space.s005"),
        new(AsterId, 1, 1, "space.s004"),
        new(BrambleId, 1, 1, "space.s007"),
        new(AsterId, 1, 1, "space.s006", DecisionOptions.Decline),
        new(BrambleId, 1, 1, "space.s009"),
        new(AsterId, 1, 1, "space.s008", DecisionOptions.Decline),
        new(BrambleId, 1, 1, "space.s011", DecisionOptions.Decline),
        new(AsterId, 1, 1, "space.s010", DecisionOptions.Decline),
        new(BrambleId, 1, 1, "space.s013", DecisionOptions.Decline)
    ];

    private sealed record TurnStep(
        int ActorPlayerId,
        int FirstDie,
        int SecondDie,
        SpaceId ExpectedFinalSpaceId,
        DecisionOptionId? Response = null)
    {
        internal TurnStep(
            int actorPlayerId,
            int firstDie,
            int secondDie,
            string expectedFinalSpaceId,
            DecisionOptionId? response = null)
            : this(actorPlayerId, firstDie, secondDie, new SpaceId(expectedFinalSpaceId), response)
        {
        }
    }

    private sealed record ScenarioResult(string SemanticTranscript);

    private sealed class DecisionIdNormalizer
    {
        private readonly Dictionary<Guid, string> _ids = [];

        internal string Normalize(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("A decision ID cannot be empty.", nameof(id));
            if (!_ids.TryGetValue(id, out string? normalized))
            {
                normalized = $"decision-{_ids.Count + 1}";
                _ids.Add(id, normalized);
            }
            return normalized;
        }
    }
}
