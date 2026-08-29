namespace Monopoly.Core.Persistence;

using Monopoly.Core.Randomness;

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
    public DecisionKindId Kind { get; set; }
    public int PlayerId { get; set; }
    public List<DecisionOptionId> AllowedResponses { get; set; } = new();
    public SpaceId? SpaceId { get; set; }
    public ResourceId? ResourceId { get; set; }
    public int? ResourceAmount { get; set; }
    public StatusId? StatusId { get; set; }
    public bool? HasAlternative { get; set; }
    public int? StatusValue { get; set; }
    public int? MaximumStatusValue { get; set; }
}

public sealed class TurnContinuationState
{
    public TurnContinuationKindState Kind { get; set; }
    public int PlayerId { get; set; }
    public RandomPurpose DicePurpose { get; set; }
    public List<int> DiceResults { get; set; } = new();
    public int DiceSum { get; set; }
    public SpaceId LandedSpaceId { get; set; }
    public bool WasDouble { get; set; }
    public StatusId? ReleasedStatusId { get; set; }
}

public enum TurnContinuationKindState
{
    StandardLanding,
    StatusRollLanding
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
            Continuation = game.TurnContinuationSnapshot is null ? null : MapContinuation(game, game.TurnContinuationSnapshot),
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
            case PurchaseDecision purchase:
                state.SpaceId = purchase.SpaceId;
                state.ResourceId = purchase.Price.ResourceId;
                state.ResourceAmount = purchase.Price.Value;
                break;
            case StatusDecision status:
                state.StatusId = status.StatusId;
                state.ResourceId = status.Cost.ResourceId;
                state.ResourceAmount = status.Cost.Value;
                state.HasAlternative = status.HasAlternative;
                state.StatusValue = status.CurrentValue;
                state.MaximumStatusValue = status.MaximumValue;
                break;
        }

        return state;
    }

    private static TurnContinuationState MapContinuation(Game game, TurnContinuation continuation) => new()
    {
        Kind = continuation.Kind == TurnContinuationKind.StandardLanding
            ? TurnContinuationKindState.StandardLanding
            : TurnContinuationKindState.StatusRollLanding,
        PlayerId = continuation.PlayerId,
        DicePurpose = continuation.Roll.Purpose,
        DiceResults = continuation.Roll.Results.ToList(),
        DiceSum = continuation.Roll.Sum,
        LandedSpaceId = game.Board.Track.GetSpaceIdAt(continuation.LandedSquarePosition),
        WasDouble = continuation.Roll.IsDouble,
        ReleasedStatusId = continuation.WasReleasedFromJailByDouble ? LegacyStatusIds.Detained : null
    };
}
