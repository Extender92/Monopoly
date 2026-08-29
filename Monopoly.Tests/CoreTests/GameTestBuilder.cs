using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Persistence;

namespace Monopoly.Tests.CoreTests;

internal sealed class GameTestBuilder
{
    private readonly GameStateV1 _state;
    private IPlayerDecisionProvider? _decisions;
    private IMatchRandomSource _randomSource = new MinimumMatchRandomSource();

    internal GameTestBuilder(int playerCount = 2)
        : this(new GameRules(playerCount, 2, 6))
    {
    }

    internal GameTestBuilder(GameRules rules)
    {
        Game baseline = CoreGameSetup.Setup(rules, randomSource: new MinimumMatchRandomSource());
        _state = GameStateV1Mapper.ToState(baseline);
    }

    internal GameTestBuilder WithCurrentPlayer(int playerId)
    {
        _state.CurrentPlayerId = playerId;
        return this;
    }

    internal GameTestBuilder WithPlayer(
        int playerId,
        string? name = null,
        int? money = null,
        int? position = null,
        int? jailCards = null,
        bool? isBankrupt = null)
    {
        PlayerState player = _state.Players.Single(candidate => candidate.Id == playerId);
        if (name is not null) player.Name = name;
        if (money is int savedMoney) player.Money = savedMoney;
        if (position is int savedPosition) player.Position = savedPosition;
        if (jailCards is int savedCards) player.NumberOfGetOutOfJailCards = savedCards;
        if (isBankrupt is bool savedBankruptcy) player.IsBankrupt = savedBankruptcy;
        return this;
    }

    internal GameTestBuilder WithTurn(int currentTurn, int consecutiveDoubles = 0, int fines = 0)
    {
        _state.CurrentTurn = currentTurn;
        _state.ConsecutiveDoubles = consecutiveDoubles;
        _state.Fines = fines;
        return this;
    }

    internal GameTestBuilder WithSquare(
        int position,
        int? ownerId,
        int houses = 0,
        bool isMortgage = false)
    {
        _state.Squares.RemoveAll(square => square.Position == position);
        if (ownerId is not null || houses != 0 || isMortgage)
        {
            _state.Squares.Add(new SquareState
            {
                Position = position,
                OwnerId = ownerId,
                Houses = houses,
                IsMortgage = isMortgage
            });
        }
        return this;
    }

    internal GameTestBuilder WithPlayerInJail(int playerId, int turnsInJail = 0)
    {
        GameRules rules = _state.Rules.ToGameRules();
        int jailPosition = new GameBoard(rules).Squares.OfType<JailSquare>().Single().Position;
        WithPlayer(playerId, position: jailPosition);
        _state.Jail.RemoveAll(jail => jail.PlayerId == playerId);
        _state.Jail.Add(new JailState { PlayerId = playerId, TurnsInJail = turnsInJail });
        return this;
    }

    internal GameTestBuilder WithRandomValues(params int[] values)
    {
        _randomSource = new ScriptedMatchRandomSource(values);
        return this;
    }

    internal GameTestBuilder WithRandomSource(IMatchRandomSource randomSource)
    {
        _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
        return this;
    }

    internal GameTestBuilder WithChanceCardFirst(int cardIndex)
    {
        string cardKey = cardIndex.ToString();
        if (!_state.ChanceDeck.Remove(cardKey))
            throw new ArgumentOutOfRangeException(nameof(cardIndex));

        _state.ChanceDeck.Insert(0, cardKey);
        return this;
    }

    internal GameTestBuilder WithDecisions(IPlayerDecisionProvider decisions)
    {
        _decisions = decisions;
        return this;
    }

    internal Game Build() => GameStateV1Mapper.FromState(_state, _decisions, _randomSource);
}

internal static class GameTestActions
{
    internal static TurnResult PlayTurnToCompletion(
        this Game game,
        Func<PendingDecision, DecisionOption>? chooseResponse = null)
    {
        GameActionResult result = game.PlayTurn();
        while (result.Status == GameActionStatus.DecisionRequired)
        {
            PendingDecision decision = result.PendingDecision
                ?? throw new InvalidOperationException("A required decision did not contain a snapshot.");
            DecisionOption response = chooseResponse?.Invoke(decision) ?? decision switch
            {
                PropertyPurchaseDecision => DecisionOption.Decline,
                JailReleaseDecision => DecisionOption.RollForDoubles,
                _ => throw new InvalidOperationException("Unknown test decision type.")
            };
            result = game.SubmitDecision(new DecisionResponse(decision.DecisionId, response));
        }

        return result.TurnResult
            ?? throw new InvalidOperationException($"The test turn did not complete: {result.Status} ({result.RejectionReason}).");
    }
}
