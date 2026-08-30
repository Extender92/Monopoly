using System.Text.Json;
using Monopoly.Core.Models.FortuneCard;
using Monopoly.Core.Notifications;
using Monopoly.Core.Persistence;

namespace Monopoly.Tests.CoreTests;

public sealed class MatchRandomnessTests
{
    [Fact]
    public void RandomRequestsValidateTheirContractAndReserveSetupPurposes()
    {
        RandomRequest startingPlayer = new(RandomPurpose.SetupStartingPlayer, 0, 2, 0);
        RandomRequest setupDice = new(RandomPurpose.SetupDice, 1, 7, 1);

        Assert.Equal(RandomPurpose.SetupStartingPlayer, startingPlayer.Purpose);
        Assert.Equal(RandomPurpose.SetupDice, setupDice.Purpose);
        Assert.Throws<ArgumentOutOfRangeException>(() => new RandomRequest((RandomPurpose)999, 0, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RandomRequest(RandomPurpose.TurnDice, 1, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RandomRequest(RandomPurpose.TurnDice, 1, 7, -1));

        MinimumMatchRandomSource source = new();
        MatchRandomizer randomizer = new(source);
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.NextInt(default));
        Assert.Empty(source.Requests);
    }

    [Fact]
    public void SystemSourceValidatesRequestsAndReturnsValuesInsideTheRequestedRange()
    {
        SystemMatchRandomSource source = new();
        RandomRequest request = new(RandomPurpose.DeckShuffle, -4, 5, 0);

        int[] values = Enumerable.Range(0, 100).Select(_ => source.NextInt(request)).ToArray();

        Assert.All(values, value => Assert.InRange(value, -4, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.NextInt(default));
    }

    [Fact]
    public void RandomizerReturnsTypedErrorsForExhaustedOutOfRangeAndFailedSources()
    {
        RandomRequest request = new(RandomPurpose.TurnDice, 1, 7, 0);

        RandomSourceException exhausted = Assert.Throws<RandomSourceException>(() =>
            new MatchRandomizer(new ScriptedMatchRandomSource()).NextInt(request));
        RandomSourceException outOfRange = Assert.Throws<RandomSourceException>(() =>
            new MatchRandomizer(new ConstantRandomSource(7)).NextInt(request));
        RandomSourceException failed = Assert.Throws<RandomSourceException>(() =>
            new MatchRandomizer(new FailingRandomSource()).NextInt(request));

        Assert.Equal(RandomSourceErrorKind.Exhausted, exhausted.Kind);
        Assert.Equal(RandomSourceErrorKind.OutOfRange, outOfRange.Kind);
        Assert.Equal(7, outOfRange.ReturnedValue);
        Assert.Equal(RandomSourceErrorKind.SourceFailure, failed.Kind);
        Assert.IsType<InvalidOperationException>(failed.InnerException);
        Assert.Equal(request, failed.Request);
    }

    [Fact]
    public void FailedMultiDieRollLeavesMatchStateLogsAndNotificationsUnchanged()
    {
        ScriptedMatchRandomSource source = new(2, 7);
        Game game = new GameTestBuilder().WithRandomSource(source).Build();
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);
        string before = SerializeState(game);

        RandomSourceException exception = Assert.Throws<RandomSourceException>(() => game.PlayTurn());

        Assert.Equal(RandomSourceErrorKind.OutOfRange, exception.Kind);
        Assert.Equal(RandomPurpose.TurnDice, exception.Request.Purpose);
        Assert.Equal(1, exception.Request.MinimumInclusive);
        Assert.Equal(7, exception.Request.MaximumExclusive);
        Assert.Equal(1, exception.Request.SequenceIndex);
        Assert.Equal(7, exception.ReturnedValue);
        Assert.Equal(before, SerializeState(game));
        Assert.Equal(GamePhase.ReadyForTurn, game.Phase);
        Assert.Null(game.PendingDecision);
        Assert.Null(game.LastDiceRoll);
        Assert.Empty(game.Logs.LogList);
        Assert.Empty(notifications);
    }

    [Fact]
    public void ExhaustedDetentionRollDoesNotConsumeThePendingDecision()
    {
        ScriptedMatchRandomSource source = new(1);
        Game game = new GameTestBuilder()
            .WithPlayerInJail(0)
            .WithRandomSource(source)
            .Build();
        StatusDecision decision = Assert.IsType<StatusDecision>(game.PlayTurn().PendingDecision);
        string before = JsonSerializer.Serialize(GameProgressStateMapper.ToState(game));

        RandomSourceException exception = Assert.Throws<RandomSourceException>(() => game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, DecisionOptions.Roll)));

        Assert.Equal(RandomSourceErrorKind.Exhausted, exception.Kind);
        Assert.Equal(RandomPurpose.DetentionDice, exception.Request.Purpose);
        Assert.Equal(1, exception.Request.SequenceIndex);
        Assert.Equal(before, JsonSerializer.Serialize(GameProgressStateMapper.ToState(game)));
        Assert.Same(decision, game.PendingDecision);
        Assert.Equal(GamePhase.AwaitingDecision, game.Phase);
        Assert.Null(game.LastDiceRoll);
        Assert.Empty(game.Logs.LogList);
        Assert.Equal(
            [RandomPurpose.DetentionDice, RandomPurpose.DetentionDice],
            source.Requests.Select(request => request.Purpose));
    }

    [Fact]
    public void ConcurrentMatchesUseOnlyTheirOwnInjectedSources()
    {
        ScriptedMatchRandomSource firstSource = new(1, 2);
        ScriptedMatchRandomSource secondSource = new(4, 5);
        Game first = new GameTestBuilder().WithRandomSource(firstSource).Build();
        Game second = new GameTestBuilder().WithRandomSource(secondSource).Build();

        TurnResult firstResult = first.PlayTurnToCompletion();
        TurnResult secondResult = second.PlayTurnToCompletion();

        Assert.Equal([1, 2], firstResult.Roll!.Results);
        Assert.Equal([4, 5], secondResult.Roll!.Results);
        Assert.Equal([1, 2], first.LastDiceRoll!.Results);
        Assert.Equal([4, 5], second.LastDiceRoll!.Results);
        Assert.Equal(2, firstSource.Requests.Count);
        Assert.Equal(2, secondSource.Requests.Count);
        Assert.Equal(0, firstSource.RemainingCount);
        Assert.Equal(0, secondSource.RemainingCount);
    }

    [Fact]
    public void IdenticalScriptsAndDecisionsProduceEquivalentSemanticOutcomes()
    {
        ScenarioSnapshot first = RunScenario();
        ScenarioSnapshot second = RunScenario();

        Assert.Equal(first, second);
    }

    [Fact]
    public void DedicatedRuleRollUsesItsOwnPurposeWithoutReplacingTheTurnResult()
    {
        ScriptedMatchRandomSource source = new(1, 2, 4, 5);
        Game game = new GameTestBuilder().WithRandomSource(source).Build();

        TurnResult result = game.PlayTurnToCompletion();
        DiceRoll dedicated = game.Handler.RollDice(game.CurrentPlayer, RandomPurpose.DedicatedRuleDice);

        Assert.Equal(RandomPurpose.TurnDice, result.Roll!.Purpose);
        Assert.Equal([1, 2], result.Roll.Results);
        Assert.Equal(3, result.Roll.Sum);
        Assert.Equal(3, result.LandedSpace!.Index);
        Assert.Equal(RandomPurpose.DedicatedRuleDice, game.LastDiceRoll!.Purpose);
        Assert.Equal([4, 5], dedicated.Results);
        Assert.Equal([1, 2], result.Roll.Results);
        Assert.Equal(
            [RandomPurpose.TurnDice, RandomPurpose.TurnDice,
                RandomPurpose.DedicatedRuleDice, RandomPurpose.DedicatedRuleDice],
            source.Requests.Select(request => request.Purpose));
        Assert.Equal([0, 1, 0, 1], source.Requests.Select(request => request.SequenceIndex));
    }

    [Fact]
    public void NonDicePurposeIsRejectedBeforeTheSourceIsInvoked()
    {
        MinimumMatchRandomSource source = new();
        Game game = new GameTestBuilder().WithRandomSource(source).Build();

        Assert.Throws<ArgumentException>(() =>
            game.Handler.RollDice(game.CurrentPlayer, RandomPurpose.DeckShuffle));

        Assert.Empty(source.Requests);
        Assert.Null(game.LastDiceRoll);
        Assert.Empty(game.Logs.LogList);
    }

    private static ScenarioSnapshot RunScenario()
    {
        ScriptedMatchRandomSource source = ScriptedMatchRandomSource.ForDice(1, 2);
        Game game = new GameTestBuilder().WithRandomSource(source).Build();
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);

        PurchaseDecision decision = Assert.IsType<PurchaseDecision>(game.PlayTurn().PendingDecision);
        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, DecisionOptions.Decline));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        return new ScenarioSnapshot(
            GameTestSnapshot.CaptureAuthoritative(game),
            string.Join(',', completed.TurnResult!.Roll!.Results),
            string.Join(',', game.Decks.Entries.SelectMany(deck => deck.Cards).Select(card => card.Id.Value)),
            string.Join(',', notifications.Select(notification =>
                $"{notification.GetType().Name}:{notification.PresentationToken.Value}")));
    }

    private static string SerializeState(Game game) =>
        GameTestSnapshot.Capture(game);

    private sealed record ScenarioSnapshot(
        string State,
        string Roll,
        string DeckOrder,
        string Notifications);

    private sealed class ConstantRandomSource(int value) : IMatchRandomSource
    {
        public int NextInt(RandomRequest request) => value;
    }

    private sealed class FailingRandomSource : IMatchRandomSource
    {
        public int NextInt(RandomRequest request) => throw new InvalidOperationException("source failure");
    }
}
