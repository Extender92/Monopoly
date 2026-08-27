using System.Reflection;
using System.Text.Json;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Persistence;

namespace Monopoly.Tests.CoreTests;

public sealed class ResumableGamePhaseTests
{
    [Fact]
    public void InitialProgressContractIsReadOnlyAndReadyForTurn()
    {
        Game game = new GameTestBuilder().Build();

        Assert.Equal(GamePhase.ReadyForTurn, game.Phase);
        Assert.Null(game.PendingDecision);
        AssertNoPublicSetter<Game>(nameof(Game.Phase), nameof(Game.PendingDecision));
        AssertNoPublicSetter<GameActionResult>(
            nameof(GameActionResult.Status),
            nameof(GameActionResult.TurnResult),
            nameof(GameActionResult.PendingDecision),
            nameof(GameActionResult.RejectionReason));
        AssertNoPublicSetter<PendingDecision>(
            nameof(PendingDecision.DecisionId),
            nameof(PendingDecision.PlayerId),
            nameof(PendingDecision.Kind),
            nameof(PendingDecision.AllowedResponses));
        AssertNoPublicSetter<PropertyPurchaseDecision>(
            nameof(PropertyPurchaseDecision.SquarePosition),
            nameof(PropertyPurchaseDecision.Price));
        AssertNoPublicSetter<JailReleaseDecision>(
            nameof(JailReleaseDecision.Fine),
            nameof(JailReleaseDecision.HasGetOutOfJailCard),
            nameof(JailReleaseDecision.TurnsInJail),
            nameof(JailReleaseDecision.MaximumTurnsInJail));
        Assert.Equal(
            [nameof(IPlayerDecisionProvider.ResolveInsufficientFunds)],
            typeof(IPlayerDecisionProvider).GetMethods().Select(method => method.Name));
    }

    [Fact]
    public void PurchaseDecisionPausesAfterMovementWithStableImmutableSnapshot()
    {
        FixedDie firstDie = new(1);
        FixedDie secondDie = new(2);
        Game game = new GameTestBuilder().WithDice(firstDie, secondDie).Build();
        Player player = game.CurrentPlayer;

        GameActionResult required = game.PlayTurn();

        PropertyPurchaseDecision decision = Assert.IsType<PropertyPurchaseDecision>(required.PendingDecision);
        Assert.Equal(GameActionStatus.DecisionRequired, required.Status);
        Assert.Equal(GamePhase.AwaitingDecision, game.Phase);
        Assert.Same(decision, game.PendingDecision);
        Assert.Equal(player.Id, decision.PlayerId);
        Assert.Equal(3, decision.SquarePosition);
        Assert.Equal(game.Board.GetSquareAtPosition(3).Price, decision.Price);
        Assert.Equal([DecisionOption.Purchase, DecisionOption.Decline], decision.AllowedResponses);
        Assert.Throws<NotSupportedException>(() => ((IList<DecisionOption>)decision.AllowedResponses).Add(DecisionOption.LeaveJail));
        Assert.Equal(3, player.Position);
        Assert.Null(game.Board.GetSquareAtPosition(3).Owner);

        GameActionResult rejected = game.PlayTurn();

        Assert.Equal(GameActionStatus.Rejected, rejected.Status);
        Assert.Equal(GameActionRejectionReason.PendingDecisionRequired, rejected.RejectionReason);
        Assert.Same(decision, rejected.PendingDecision);
        Assert.Equal(decision.DecisionId, game.PendingDecision!.DecisionId);
        Assert.Equal(1, firstDie.RollCount);
        Assert.Equal(1, secondDie.RollCount);
    }

    [Fact]
    public void PurchasingResumesWithoutRepeatingMovementAndRotatesExactlyOnce()
    {
        FixedDie firstDie = new(1);
        FixedDie secondDie = new(2);
        Game game = new GameTestBuilder().WithDice(firstDie, secondDie).Build();
        Player buyer = game.CurrentPlayer;
        Player nextPlayer = game.Players[1];
        int originalMoney = buyer.Money;
        PropertyPurchaseDecision decision = RequirePurchase(game);

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, DecisionOption.Purchase));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.NotNull(completed.TurnResult);
        Assert.Equal(GamePhase.ReadyForTurn, game.Phase);
        Assert.Null(game.PendingDecision);
        Assert.Same(buyer, game.Board.GetSquareAtPosition(decision.SquarePosition).Owner);
        Assert.Equal(originalMoney - decision.Price, buyer.Money);
        Assert.Same(nextPlayer, game.CurrentPlayer);
        Assert.Equal(1, firstDie.RollCount);
        Assert.Equal(1, secondDie.RollCount);
    }

    [Fact]
    public void DecliningPurchaseLeavesSquareAndMoneyUnchangedThenCompletesTurn()
    {
        Game game = new GameTestBuilder().WithDice(new FixedDie(1), new FixedDie(2)).Build();
        Player player = game.CurrentPlayer;
        int money = player.Money;
        PropertyPurchaseDecision decision = RequirePurchase(game);

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, DecisionOption.Decline));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.Null(game.Board.GetSquareAtPosition(decision.SquarePosition).Owner);
        Assert.Equal(money, player.Money);
    }

    [Fact]
    public void PurchaseCanUseOnlyRemainingSynchronousFundsCallbackAfterAcceptance()
    {
        MortgageFundsProvider decisions = new(squarePosition: 5);
        Game game = new GameTestBuilder()
            .WithPlayer(0, money: 0)
            .WithSquare(5, ownerId: 0)
            .WithDice(new FixedDie(1), new FixedDie(2))
            .WithDecisions(decisions)
            .Build();
        PropertyPurchaseDecision decision = RequirePurchase(game);

        Assert.Equal(0, decisions.RequestCount);

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, DecisionOption.Purchase));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.Equal(1, decisions.RequestCount);
        Assert.Same(game.Players.Single(player => player.Id == 0), game.Board.GetSquareAtPosition(3).Owner);
        Assert.True(game.Board.GetSquareAtPosition(5).IsMortgage);
    }

    [Fact]
    public void FailedOptionalPurchaseFinancingDoesNotBankruptPlayer()
    {
        CountingFundsProvider decisions = new();
        Game game = new GameTestBuilder()
            .WithPlayer(0, money: 0)
            .WithSquare(5, ownerId: 0)
            .WithDice(new FixedDie(1), new FixedDie(2))
            .WithDecisions(decisions)
            .Build();
        Player player = game.CurrentPlayer;
        PropertyPurchaseDecision decision = RequirePurchase(game);

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, DecisionOption.Purchase));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.Equal(1, decisions.RequestCount);
        Assert.False(player.IsBankrupt);
        Assert.Null(game.Board.GetSquareAtPosition(decision.SquarePosition).Owner);
    }

    [Fact]
    public void JailDecisionIsCreatedBeforeDiceMoneyCardsOrStatusMutate()
    {
        FixedDie firstDie = new(1);
        FixedDie secondDie = new(4);
        Game game = new GameTestBuilder(new GameRules(2, 2, 6, jailFine: 75))
            .WithPlayer(0, money: 100, jailCards: 1)
            .WithPlayerInJail(0, turnsInJail: 1)
            .WithDice(firstDie, secondDie)
            .Build();
        Player player = game.CurrentPlayer;

        GameActionResult required = game.PlayTurn();

        JailReleaseDecision decision = Assert.IsType<JailReleaseDecision>(required.PendingDecision);
        Assert.Equal(75, decision.Fine);
        Assert.True(decision.HasGetOutOfJailCard);
        Assert.Equal(1, decision.TurnsInJail);
        Assert.Equal(3, decision.MaximumTurnsInJail);
        Assert.Equal([DecisionOption.LeaveJail, DecisionOption.RollForDoubles], decision.AllowedResponses);
        Assert.Equal(100, player.Money);
        Assert.Equal(1, player.NumberOfGetOutOFJailCards);
        Assert.Equal(1, game.TheJail.GetJailInfo(player).TurnsInJail);
        Assert.Equal(0, firstDie.RollCount);
        Assert.Equal(0, secondDie.RollCount);
    }

    [Fact]
    public void LeaveJailUsesConfiguredFineThenCompletesExistingJailFlow()
    {
        Game game = new GameTestBuilder(new GameRules(2, 2, 6, jailFine: 17))
            .WithPlayer(0, money: 100)
            .WithPlayerInJail(0)
            .WithDice(new FixedDie(1), new FixedDie(4))
            .Build();
        Player player = game.CurrentPlayer;
        JailReleaseDecision decision = Assert.IsType<JailReleaseDecision>(game.PlayTurn().PendingDecision);

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, DecisionOption.LeaveJail));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.Equal(83, player.Money);
        Assert.False(game.TheJail.IsPlayerInJail(player));
        Assert.Equal([1, 4], completed.TurnResult!.DiceResults);
    }

    [Fact]
    public void LeaveJailUsesHeldCardBeforeConfiguredFine()
    {
        Game game = new GameTestBuilder(new GameRules(2, 2, 6, jailFine: 17))
            .WithPlayer(0, money: 100, jailCards: 1)
            .WithPlayerInJail(0)
            .WithDice(new FixedDie(1), new FixedDie(4))
            .Build();
        Player player = game.CurrentPlayer;
        JailReleaseDecision decision = Assert.IsType<JailReleaseDecision>(game.PlayTurn().PendingDecision);

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, DecisionOption.LeaveJail));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.Equal(100, player.Money);
        Assert.Equal(0, player.NumberOfGetOutOFJailCards);
        Assert.False(game.TheJail.IsPlayerInJail(player));
    }

    [Fact]
    public void JailDoubleCanContinueToNewPurchaseDecisionWithoutRerolling()
    {
        FixedDie firstDie = new(1);
        FixedDie secondDie = new(1);
        Game game = new GameTestBuilder()
            .WithPlayerInJail(0)
            .WithDice(firstDie, secondDie)
            .Build();
        Player player = game.CurrentPlayer;
        Player nextPlayer = game.Players[1];
        JailReleaseDecision jailDecision = Assert.IsType<JailReleaseDecision>(game.PlayTurn().PendingDecision);

        GameActionResult purchaseRequired = game.SubmitDecision(
            new DecisionResponse(jailDecision.DecisionId, DecisionOption.RollForDoubles));

        PropertyPurchaseDecision purchase = Assert.IsType<PropertyPurchaseDecision>(purchaseRequired.PendingDecision);
        Assert.Equal(12, purchase.SquarePosition);
        Assert.NotEqual(jailDecision.DecisionId, purchase.DecisionId);
        Assert.False(game.TheJail.IsPlayerInJail(player));
        Assert.Equal(12, player.Position);
        Assert.Same(player, game.CurrentPlayer);
        Assert.Equal(1, firstDie.RollCount);
        Assert.Equal(1, secondDie.RollCount);

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(purchase.DecisionId, DecisionOption.Decline));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.True(completed.TurnResult!.WasReleasedFromJailByDouble);
        Assert.Same(nextPlayer, game.CurrentPlayer);
        Assert.Equal(1, firstDie.RollCount);
        Assert.Equal(1, secondDie.RollCount);
    }

    [Fact]
    public void InvalidResponsesAndPlayTurnRejectionAreAtomic()
    {
        Game game = new GameTestBuilder().WithDice(new FixedDie(1), new FixedDie(2)).Build();
        PropertyPurchaseDecision decision = RequirePurchase(game);

        AssertRejectedWithoutMutation(game, null, GameActionRejectionReason.MalformedResponse);
        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(Guid.Empty, DecisionOption.Purchase),
            GameActionRejectionReason.MalformedResponse);
        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(decision.DecisionId, (DecisionOption)999),
            GameActionRejectionReason.MalformedResponse);
        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(Guid.NewGuid(), DecisionOption.Purchase),
            GameActionRejectionReason.StaleDecision);
        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(decision.DecisionId, DecisionOption.LeaveJail),
            GameActionRejectionReason.ResponseNotAllowed);

        string before = CaptureSnapshot(game);
        GameActionResult playRejected = game.PlayTurn();
        Assert.Equal(GameActionRejectionReason.PendingDecisionRequired, playRejected.RejectionReason);
        Assert.Equal(before, CaptureSnapshot(game));
    }

    [Fact]
    public void UsedAndOlderDecisionIdsHaveDistinctTypedRejections()
    {
        Game game = new GameTestBuilder()
            .WithPlayerInJail(0)
            .WithDice(new FixedDie(1), new FixedDie(1))
            .Build();
        JailReleaseDecision jail = Assert.IsType<JailReleaseDecision>(game.PlayTurn().PendingDecision);
        PropertyPurchaseDecision purchase = Assert.IsType<PropertyPurchaseDecision>(game.SubmitDecision(
            new DecisionResponse(jail.DecisionId, DecisionOption.RollForDoubles)).PendingDecision);

        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(jail.DecisionId, DecisionOption.RollForDoubles),
            GameActionRejectionReason.DuplicateDecision);

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(purchase.DecisionId, DecisionOption.Decline));
        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);

        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(purchase.DecisionId, DecisionOption.Decline),
            GameActionRejectionReason.DuplicateDecision);
        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(jail.DecisionId, DecisionOption.RollForDoubles),
            GameActionRejectionReason.StaleDecision);
        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(Guid.NewGuid(), DecisionOption.Decline),
            GameActionRejectionReason.NoPendingDecision);
    }

    [Fact]
    public void ProgressProjectionIsDetachedPrimitiveOnlyDataAndNotPartOfVersionOne()
    {
        Game game = new GameTestBuilder().WithDice(new FixedDie(1), new FixedDie(2)).Build();
        PropertyPurchaseDecision decision = RequirePurchase(game);

        GameProgressState progress = GameProgressStateMapper.ToState(game);

        Assert.Equal(GamePhase.AwaitingDecision, progress.Phase);
        Assert.Equal(decision.DecisionId, progress.PendingDecision!.DecisionId);
        Assert.Equal(3, progress.PendingDecision.SquarePosition);
        Assert.Equal([DecisionOption.Purchase, DecisionOption.Decline], progress.PendingDecision.AllowedResponses);
        Assert.Equal(TurnContinuationKindState.StandardLanding, progress.Continuation!.Kind);
        Assert.Equal([1, 2], progress.Continuation.DiceResults);
        Assert.DoesNotContain(nameof(IPlayerDecisionProvider), JsonSerializer.Serialize(progress));
        Assert.DoesNotContain(typeof(Player).FullName!, JsonSerializer.Serialize(progress));

        progress.PendingDecision.AllowedResponses.Clear();
        progress.Continuation.DiceResults.Clear();

        Assert.Equal(2, game.PendingDecision!.AllowedResponses.Count);
        Assert.Equal([1, 2], GameProgressStateMapper.ToState(game).Continuation!.DiceResults);
        Assert.DoesNotContain(
            typeof(GameStateV1).GetProperties(),
            property => property.PropertyType == typeof(GameProgressState) ||
                        property.Name.Contains("Decision", StringComparison.Ordinal) ||
                        property.Name.Contains("Continuation", StringComparison.Ordinal));
        Assert.Throws<GameStateValidationException>(() => GameStateV1Mapper.ToState(game));
    }

    [Fact]
    public void ProgressDtoGraphContainsOnlyDtosPrimitivesAndEnums()
    {
        HashSet<Type> dtoTypes =
        [
            typeof(GameProgressState),
            typeof(PendingDecisionState),
            typeof(TurnContinuationState)
        ];

        foreach (Type dtoType in dtoTypes)
        {
            foreach (PropertyInfo property in dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                    propertyType = propertyType.GetGenericArguments()[0];

                Assert.True(
                    propertyType.IsPrimitive || propertyType.IsEnum || propertyType == typeof(Guid) || dtoTypes.Contains(propertyType),
                    $"{dtoType.Name}.{property.Name} exposes {property.PropertyType}.");
                Assert.False(typeof(Delegate).IsAssignableFrom(propertyType));
                Assert.False(propertyType.Namespace?.StartsWith("Monopoly.Console", StringComparison.Ordinal) ?? false);
                Assert.False(propertyType.Namespace?.StartsWith("Monopoly.Core.Models", StringComparison.Ordinal) ?? false);
            }
        }
    }

    [Fact]
    public void VersionOneReconstructionAlwaysStartsWithoutPendingProgress()
    {
        Game source = new GameTestBuilder().WithTurn(4, consecutiveDoubles: 1).Build();
        GameStateV1 state = GameStateV1Mapper.ToState(source);

        Game loaded = GameStateV1Mapper.FromState(state);

        Assert.Equal(GamePhase.ReadyForTurn, loaded.Phase);
        Assert.Null(loaded.PendingDecision);
        Assert.Null(GameProgressStateMapper.ToState(loaded).Continuation);

        state.Players.RemoveAll(player => player.Id != state.CurrentPlayerId);
        Game loadedCompletedMatch = GameStateV1Mapper.FromState(state);
        Assert.True(loadedCompletedMatch.IsGameOver);
        Assert.Equal(GamePhase.ReadyForTurn, loadedCompletedMatch.Phase);
        Assert.Equal(GameActionStatus.GameOver, loadedCompletedMatch.PlayTurn().Status);
        Assert.Equal(GamePhase.GameOver, loadedCompletedMatch.Phase);
    }

    [Fact]
    public void FinalBankruptcyReturnsGameOverStatusAndTransitionsPhase()
    {
        Game game = new GameTestBuilder()
            .WithPlayer(0, money: 0)
            .WithSquare(3, ownerId: 1)
            .WithDice(new FixedDie(1), new FixedDie(2))
            .Build();
        Player survivor = game.Players[1];

        GameActionResult result = game.PlayTurn();

        Assert.Equal(GameActionStatus.GameOver, result.Status);
        Assert.True(result.TurnResult!.GameOver);
        Assert.Same(survivor, result.TurnResult.Winner);
        Assert.Equal(GamePhase.GameOver, game.Phase);
        Assert.Null(game.PendingDecision);
    }

    private static PropertyPurchaseDecision RequirePurchase(Game game)
    {
        GameActionResult result = game.PlayTurn();
        Assert.Equal(GameActionStatus.DecisionRequired, result.Status);
        return Assert.IsType<PropertyPurchaseDecision>(result.PendingDecision);
    }

    private static void AssertRejectedWithoutMutation(
        Game game,
        DecisionResponse? response,
        GameActionRejectionReason expectedReason)
    {
        string before = CaptureSnapshot(game);

        GameActionResult rejected = game.SubmitDecision(response);

        Assert.Equal(GameActionStatus.Rejected, rejected.Status);
        Assert.Equal(expectedReason, rejected.RejectionReason);
        Assert.Equal(before, CaptureSnapshot(game));
    }

    private static string CaptureSnapshot(Game game) => JsonSerializer.Serialize(new
    {
        Players = game.Players.Select(player => new
        {
            player.Id,
            player.Name,
            player.Money,
            player.Position,
            player.NumberOfGetOutOFJailCards,
            player.IsBankrupt
        }),
        Squares = game.Board.Squares.Select(square => new
        {
            square.Position,
            OwnerId = square.Owner?.Id,
            square.IsMortgage,
            Houses = square is PropertySquare property ? property.Houses : 0
        }),
        Jail = game.TheJail.PlayersInJail.Select(entry => new { PlayerId = entry.Key.Id, entry.Value.TurnsInJail }),
        game.CurrentPlayer.Id,
        game.CurrentTurn,
        game.ConsecutiveDoubles,
        game.Fines,
        WinnerId = game.Winner?.Id,
        Dice = game.Dice.Select(die => die.GetDieResult()),
        Logs = game.Logs.LogList.Select(log => new { log.Id, log.Info }),
        Chance = game.FortuneCard.GetChanceDeckOrder(),
        CommunityChest = game.FortuneCard.GetCommunityChestDeckOrder(),
        Progress = GameProgressStateMapper.ToState(game)
    });

    private static void AssertNoPublicSetter<T>(params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            PropertyInfo property = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!;
            Assert.NotNull(property);
            Assert.False(property.SetMethod?.IsPublic ?? false, $"{typeof(T).Name}.{propertyName} has a public setter.");
        }
    }

    private sealed class FixedDie : IDie
    {
        private readonly Queue<int> _values;
        private int _result;

        internal FixedDie(params int[] values)
        {
            _values = new Queue<int>(values);
            _result = _values.Peek();
        }

        public int RollCount { get; private set; }
        public int GetDieResult() => _result;
        public int GetDieType() => 6;

        public void Roll()
        {
            RollCount++;
            if (_values.Count > 0) _result = _values.Dequeue();
        }

        public void ScrambleDie() => _result = -1;
    }

    private class CountingFundsProvider : IPlayerDecisionProvider
    {
        public int RequestCount { get; private set; }

        public virtual bool ResolveInsufficientFunds(Game game, Player player, int amount)
        {
            RequestCount++;
            return false;
        }

        protected void CountRequest() => RequestCount++;
    }

    private sealed class MortgageFundsProvider(int squarePosition) : CountingFundsProvider
    {
        public override bool ResolveInsufficientFunds(Game game, Player player, int amount)
        {
            CountRequest();
            return game.TryMortgageProperty(player, game.Board.GetSquareAtPosition(squarePosition));
        }
    }
}
