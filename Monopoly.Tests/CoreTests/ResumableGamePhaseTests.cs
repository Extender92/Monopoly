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
        AssertNoPublicSetter<PurchaseDecision>(
            nameof(PurchaseDecision.SpaceId),
            nameof(PurchaseDecision.Price));
        AssertNoPublicSetter<StatusDecision>(
            nameof(StatusDecision.StatusId),
            nameof(StatusDecision.Cost),
            nameof(StatusDecision.HasAlternative),
            nameof(StatusDecision.CurrentValue),
            nameof(StatusDecision.MaximumValue));
        Assert.Equal(
            [nameof(IPlayerDecisionProvider.ResolveInsufficientFunds)],
            typeof(IPlayerDecisionProvider).GetMethods().Select(method => method.Name));
    }

    [Fact]
    public void PurchaseDecisionPausesAfterMovementWithStableImmutableSnapshot()
    {
        ScriptedMatchRandomSource randomSource = new(1, 2);
        Game game = new GameTestBuilder().WithRandomSource(randomSource).Build();
        Player player = game.CurrentPlayer;

        GameActionResult required = game.PlayTurn();

        PurchaseDecision decision = Assert.IsType<PurchaseDecision>(required.PendingDecision);
        Assert.Equal(GameActionStatus.DecisionRequired, required.Status);
        Assert.Equal(GamePhase.AwaitingDecision, game.Phase);
        Assert.Same(decision, game.PendingDecision);
        Assert.Equal(player.Id, decision.PlayerId);
        Assert.Equal(game.Board.GetSquareAtPosition(3).Id, decision.SpaceId);
        Assert.Equal(game.Board.GetSquareAtPosition(3).Price, decision.Price.Value);
        Assert.Equal([DecisionOptions.Accept, DecisionOptions.Decline], decision.AllowedResponses);
        Assert.Throws<NotSupportedException>(() => ((IList<DecisionOptionId>)decision.AllowedResponses).Add(DecisionOptions.Resolve));
        Assert.Equal(3, player.Position);
        Assert.Null(game.Board.GetSquareAtPosition(3).Owner);

        GameActionResult rejected = game.PlayTurn();

        Assert.Equal(GameActionStatus.Rejected, rejected.Status);
        Assert.Equal(GameActionRejectionReason.PendingDecisionRequired, rejected.RejectionReason);
        Assert.Same(decision, rejected.PendingDecision);
        Assert.Equal(decision.DecisionId, game.PendingDecision!.DecisionId);
        Assert.Equal(2, randomSource.Requests.Count(request => request.Purpose == RandomPurpose.TurnDice));
    }

    [Fact]
    public void PurchasingResumesWithoutRepeatingMovementAndRotatesExactlyOnce()
    {
        ScriptedMatchRandomSource randomSource = new(1, 2);
        Game game = new GameTestBuilder().WithRandomSource(randomSource).Build();
        Player buyer = game.CurrentPlayer;
        Player nextPlayer = game.Players[1];
        int originalMoney = buyer.Money;
        PurchaseDecision decision = RequirePurchase(game);

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, DecisionOptions.Accept));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.NotNull(completed.TurnResult);
        Assert.Equal(GamePhase.ReadyForTurn, game.Phase);
        Assert.Null(game.PendingDecision);
        Assert.Same(buyer, game.Board.GetSquare(decision.SpaceId).Owner);
        Assert.Equal(originalMoney - decision.Price.Value, buyer.Money);
        Assert.Same(nextPlayer, game.CurrentPlayer);
        Assert.Equal(2, randomSource.Requests.Count(request => request.Purpose == RandomPurpose.TurnDice));
    }

    [Fact]
    public void DecliningPurchaseLeavesSquareAndMoneyUnchangedThenCompletesTurn()
    {
        Game game = new GameTestBuilder().WithRandomValues(1, 2).Build();
        Player player = game.CurrentPlayer;
        int money = player.Money;
        PurchaseDecision decision = RequirePurchase(game);

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, DecisionOptions.Decline));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.Null(game.Board.GetSquare(decision.SpaceId).Owner);
        Assert.Equal(money, player.Money);
    }

    [Fact]
    public void PurchaseCanUseOnlyRemainingSynchronousFundsCallbackAfterAcceptance()
    {
        MortgageFundsProvider decisions = new(squarePosition: 5);
        Game game = new GameTestBuilder()
            .WithPlayer(0, money: 10)
            .WithSquare(5, ownerId: 0)
            .WithRandomValues(1, 2)
            .WithDecisions(decisions)
            .Build();
        PurchaseDecision decision = RequirePurchase(game);

        Assert.Equal(0, decisions.RequestCount);

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, DecisionOptions.Accept));

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
            .WithPlayer(0, money: 10)
            .WithSquare(5, ownerId: 0)
            .WithRandomValues(1, 2)
            .WithDecisions(decisions)
            .Build();
        Player player = game.CurrentPlayer;
        PurchaseDecision decision = RequirePurchase(game);

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, DecisionOptions.Accept));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.Equal(1, decisions.RequestCount);
        Assert.False(player.IsBankrupt);
        Assert.Null(game.Board.GetSquare(decision.SpaceId).Owner);
    }

    [Fact]
    public void JailDecisionIsCreatedBeforeDiceMoneyCardsOrStatusMutate()
    {
        ScriptedMatchRandomSource randomSource = new(1, 4);
        Game game = new GameTestBuilder(new GameRules(2, 2, 6, jailFine: 75))
            .WithPlayer(0, money: 100, jailCards: 1)
            .WithPlayerInJail(0, turnsInJail: 1)
            .WithRandomSource(randomSource)
            .Build();
        Player player = game.CurrentPlayer;

        GameActionResult required = game.PlayTurn();

        StatusDecision decision = Assert.IsType<StatusDecision>(required.PendingDecision);
        Assert.Equal(LegacyStatusIds.Detained, decision.StatusId);
        Assert.Equal(75, decision.Cost.Value);
        Assert.True(decision.HasAlternative);
        Assert.Equal(1, decision.CurrentValue);
        Assert.Equal(3, decision.MaximumValue);
        Assert.Equal([DecisionOptions.Resolve, DecisionOptions.Roll], decision.AllowedResponses);
        Assert.Equal(100, player.Money);
        Assert.Equal(1, player.NumberOfGetOutOFJailCards);
        Assert.Equal(1, game.TheJail.GetJailInfo(player).TurnsInJail);
        Assert.Empty(randomSource.Requests);
    }

    [Fact]
    public void LeaveJailUsesConfiguredFineThenCompletesExistingJailFlow()
    {
        Game game = new GameTestBuilder(new GameRules(2, 2, 6, jailFine: 17))
            .WithPlayer(0, money: 100)
            .WithPlayerInJail(0)
            .WithRandomValues(1, 4)
            .Build();
        Player player = game.CurrentPlayer;
        StatusDecision decision = Assert.IsType<StatusDecision>(game.PlayTurn().PendingDecision);

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, DecisionOptions.Resolve));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.Equal(83, player.Money);
        Assert.False(game.TheJail.IsPlayerInJail(player));
        Assert.Equal([1, 4], completed.TurnResult!.DiceResults);
        StatusTransition transition = Assert.Single(completed.TurnResult.StatusTransitions);
        Assert.Equal(player.Id, transition.PlayerId);
        Assert.Equal(LegacyStatusIds.Detained, transition.StatusId);
        Assert.Equal(StatusTransitionKind.Removed, transition.Kind);
    }

    [Fact]
    public void LeaveJailUsesHeldCardBeforeConfiguredFine()
    {
        Game game = new GameTestBuilder(new GameRules(2, 2, 6, jailFine: 17))
            .WithPlayer(0, money: 100, jailCards: 1)
            .WithPlayerInJail(0)
            .WithRandomValues(1, 4)
            .Build();
        Player player = game.CurrentPlayer;
        StatusDecision decision = Assert.IsType<StatusDecision>(game.PlayTurn().PendingDecision);

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(decision.DecisionId, DecisionOptions.Resolve));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.Equal(100, player.Money);
        Assert.Equal(0, player.NumberOfGetOutOFJailCards);
        Assert.False(game.TheJail.IsPlayerInJail(player));
    }

    [Fact]
    public void JailDoubleCanContinueToNewPurchaseDecisionWithoutRerolling()
    {
        ScriptedMatchRandomSource randomSource = new(1, 1);
        Game game = new GameTestBuilder()
            .WithPlayerInJail(0)
            .WithRandomSource(randomSource)
            .Build();
        Player player = game.CurrentPlayer;
        Player nextPlayer = game.Players[1];
        StatusDecision jailDecision = Assert.IsType<StatusDecision>(game.PlayTurn().PendingDecision);

        GameActionResult purchaseRequired = game.SubmitDecision(
            new DecisionResponse(jailDecision.DecisionId, DecisionOptions.Roll));

        PurchaseDecision purchase = Assert.IsType<PurchaseDecision>(purchaseRequired.PendingDecision);
        Assert.Equal(game.Board.GetSquareAtPosition(12).Id, purchase.SpaceId);
        Assert.NotEqual(jailDecision.DecisionId, purchase.DecisionId);
        Assert.False(game.TheJail.IsPlayerInJail(player));
        Assert.Equal(12, player.Position);
        Assert.Same(player, game.CurrentPlayer);
        Assert.Equal(2, randomSource.Requests.Count(request => request.Purpose == RandomPurpose.DetentionDice));

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(purchase.DecisionId, DecisionOptions.Decline));

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.True(completed.TurnResult!.WasReleasedFromJailByDouble);
        StatusTransition transition = Assert.Single(completed.TurnResult.StatusTransitions);
        Assert.Equal(player.Id, transition.PlayerId);
        Assert.Equal(LegacyStatusIds.Detained, transition.StatusId);
        Assert.Equal(StatusTransitionKind.Removed, transition.Kind);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<StatusTransition>)completed.TurnResult.StatusTransitions).Clear());
        Assert.Same(nextPlayer, game.CurrentPlayer);
        Assert.Equal(2, randomSource.Requests.Count(request => request.Purpose == RandomPurpose.DetentionDice));
    }

    [Fact]
    public void InvalidResponsesAndPlayTurnRejectionAreAtomic()
    {
        Game game = new GameTestBuilder().WithRandomValues(1, 2).Build();
        PurchaseDecision decision = RequirePurchase(game);

        AssertRejectedWithoutMutation(game, null, GameActionRejectionReason.MalformedResponse);
        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(Guid.Empty, DecisionOptions.Accept),
            GameActionRejectionReason.MalformedResponse);
        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(decision.DecisionId, default),
            GameActionRejectionReason.MalformedResponse);
        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(Guid.NewGuid(), DecisionOptions.Accept),
            GameActionRejectionReason.StaleDecision);
        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(decision.DecisionId, DecisionOptions.Resolve),
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
            .WithRandomValues(1, 1)
            .Build();
        StatusDecision jail = Assert.IsType<StatusDecision>(game.PlayTurn().PendingDecision);
        PurchaseDecision purchase = Assert.IsType<PurchaseDecision>(game.SubmitDecision(
            new DecisionResponse(jail.DecisionId, DecisionOptions.Roll)).PendingDecision);

        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(jail.DecisionId, DecisionOptions.Roll),
            GameActionRejectionReason.DuplicateDecision);

        GameActionResult completed = game.SubmitDecision(
            new DecisionResponse(purchase.DecisionId, DecisionOptions.Decline));
        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);

        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(purchase.DecisionId, DecisionOptions.Decline),
            GameActionRejectionReason.DuplicateDecision);
        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(jail.DecisionId, DecisionOptions.Roll),
            GameActionRejectionReason.StaleDecision);
        AssertRejectedWithoutMutation(
            game,
            new DecisionResponse(Guid.NewGuid(), DecisionOptions.Decline),
            GameActionRejectionReason.NoPendingDecision);
    }

    [Fact]
    public void ProgressProjectionIsDetachedPrimitiveOnlyData()
    {
        Game game = new GameTestBuilder().WithRandomValues(1, 2).Build();
        PurchaseDecision decision = RequirePurchase(game);

        GameProgressState progress = GameProgressStateMapper.ToState(game);

        Assert.Equal(GamePhase.AwaitingDecision, progress.Phase);
        Assert.Equal(decision.DecisionId, progress.PendingDecision!.DecisionId);
        Assert.Equal(decision.SpaceId, progress.PendingDecision.SpaceId);
        Assert.Equal([DecisionOptions.Accept, DecisionOptions.Decline], progress.PendingDecision.AllowedResponses);
        Assert.Equal(TurnContinuationKindState.StandardLanding, progress.Continuation!.Kind);
        Assert.Equal(RandomPurpose.TurnDice, progress.Continuation.DicePurpose);
        Assert.Equal([1, 2], progress.Continuation.DiceResults);
        Assert.DoesNotContain(nameof(IPlayerDecisionProvider), JsonSerializer.Serialize(progress));
        Assert.DoesNotContain(typeof(Player).FullName!, JsonSerializer.Serialize(progress));

        progress.PendingDecision.AllowedResponses.Clear();
        progress.Continuation.DiceResults.Clear();

        Assert.Equal(2, game.PendingDecision!.AllowedResponses.Count);
        Assert.Equal([1, 2], GameProgressStateMapper.ToState(game).Continuation!.DiceResults);
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
                    propertyType.IsPrimitive || propertyType.IsEnum || propertyType == typeof(Guid) ||
                    propertyType == typeof(DecisionKindId) || propertyType == typeof(DecisionOptionId) ||
                    propertyType == typeof(SpaceId) || propertyType == typeof(ResourceId) ||
                    propertyType == typeof(StatusId) || dtoTypes.Contains(propertyType),
                    $"{dtoType.Name}.{property.Name} exposes {property.PropertyType}.");
                Assert.False(typeof(Delegate).IsAssignableFrom(propertyType));
                Assert.False(propertyType.Namespace?.StartsWith("Monopoly.Console", StringComparison.Ordinal) ?? false);
                Assert.False(propertyType.Namespace?.StartsWith("Monopoly.Core.Models", StringComparison.Ordinal) ?? false);
            }
        }
    }

    [Fact]
    public void FinalBankruptcyReturnsGameOverStatusAndTransitionsPhase()
    {
        Game game = new GameTestBuilder()
            .WithPlayer(0, money: 0)
            .WithSquare(3, ownerId: 1)
            .WithRandomValues(1, 2)
            .Build();
        Player survivor = game.Players[1];

        GameActionResult result = game.PlayTurn();

        Assert.Equal(GameActionStatus.GameOver, result.Status);
        Assert.True(result.TurnResult!.GameOver);
        Assert.Same(survivor, result.TurnResult.Winner);
        Assert.Equal(GamePhase.GameOver, game.Phase);
        Assert.Null(game.PendingDecision);
    }

    private static PurchaseDecision RequirePurchase(Game game)
    {
        GameActionResult result = game.PlayTurn();
        Assert.Equal(GameActionStatus.DecisionRequired, result.Status);
        return Assert.IsType<PurchaseDecision>(result.PendingDecision);
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
        Dice = game.LastDiceRoll?.Results,
        Logs = game.Logs.LogList.Select(log => new { log.Id, log.Info }),
        Decks = game.Decks.Entries.Select(deck => new
        {
            deck.Id,
            Cards = deck.Cards.Select(card => card.Id)
        }),
        Progress = GameProgressStateMapper.ToState(game)
    });

    private static void AssertNoPublicSetter<T>(params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            PropertyInfo property = typeof(T).GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
            Assert.NotNull(property);
            Assert.False(property.SetMethod?.IsPublic ?? false, $"{typeof(T).Name}.{propertyName} has a public setter.");
        }
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
