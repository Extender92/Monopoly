namespace Monopoly.Core.Persistence;

/// <summary>
/// Detached, storage-neutral projection of resumable match progress. Version 1 deliberately
/// does not include this data; it is the primitive-only handoff for a future save format.
/// </summary>
public sealed class GameProgressState
{
    public GamePhase Phase { get; set; }
    public PendingDecisionState? PendingDecision { get; set; }
    public TurnContinuationState? Continuation { get; set; }
    public Guid? LastConsumedDecisionId { get; set; }
    public List<Guid> ConsumedDecisionIds { get; set; } = new();
}

public sealed class PendingDecisionState
{
    public Guid DecisionId { get; set; }
    public DecisionKind Kind { get; set; }
    public int PlayerId { get; set; }
    public List<DecisionOption> AllowedResponses { get; set; } = new();
    public int? SquarePosition { get; set; }
    public int? Price { get; set; }
    public int? JailFine { get; set; }
    public bool? HasGetOutOfJailCard { get; set; }
    public int? TurnsInJail { get; set; }
    public int? MaximumTurnsInJail { get; set; }
}

public sealed class TurnContinuationState
{
    public TurnContinuationKindState Kind { get; set; }
    public int PlayerId { get; set; }
    public List<int> DiceResults { get; set; } = new();
    public int DiceSum { get; set; }
    public int LandedSquarePosition { get; set; }
    public bool WasDouble { get; set; }
    public bool WasReleasedFromJailByDouble { get; set; }
}

public enum TurnContinuationKindState
{
    StandardLanding,
    JailDoubleLanding
}

public static class GameProgressStateMapper
{
    public static GameProgressState ToState(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return new GameProgressState
        {
            Phase = game.Phase,
            PendingDecision = game.PendingDecision is null ? null : MapDecision(game.PendingDecision),
            Continuation = game.TurnContinuationSnapshot is null ? null : MapContinuation(game.TurnContinuationSnapshot),
            LastConsumedDecisionId = game.LastConsumedDecisionId,
            ConsumedDecisionIds = game.ConsumedDecisionIds.OrderBy(id => id).ToList()
        };
    }

    private static PendingDecisionState MapDecision(PendingDecision decision)
    {
        PendingDecisionState state = new()
        {
            DecisionId = decision.DecisionId,
            Kind = decision.Kind,
            PlayerId = decision.PlayerId,
            AllowedResponses = decision.AllowedResponses.ToList()
        };

        switch (decision)
        {
            case PropertyPurchaseDecision purchase:
                state.SquarePosition = purchase.SquarePosition;
                state.Price = purchase.Price;
                break;
            case JailReleaseDecision jail:
                state.JailFine = jail.Fine;
                state.HasGetOutOfJailCard = jail.HasGetOutOfJailCard;
                state.TurnsInJail = jail.TurnsInJail;
                state.MaximumTurnsInJail = jail.MaximumTurnsInJail;
                break;
        }

        return state;
    }

    private static TurnContinuationState MapContinuation(TurnContinuation continuation) => new()
    {
        Kind = continuation.Kind == TurnContinuationKind.StandardLanding
            ? TurnContinuationKindState.StandardLanding
            : TurnContinuationKindState.JailDoubleLanding,
        PlayerId = continuation.PlayerId,
        DiceResults = continuation.DiceResults.ToList(),
        DiceSum = continuation.DiceSum,
        LandedSquarePosition = continuation.LandedSquarePosition,
        WasDouble = continuation.WasDouble,
        WasReleasedFromJailByDouble = continuation.WasReleasedFromJailByDouble
    };
}
