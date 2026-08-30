namespace Monopoly.Core.Persistence;

using Monopoly.Core.Randomness;

/// <summary>Detached primitive projection of resumable progress for the future Version 2 format.</summary>
public sealed class GameProgressState
{
    public GamePhase Phase { get; set; }
    public PendingDecisionState? PendingDecision { get; set; }
    public TurnContinuationState? Continuation { get; set; }
    public Guid? LastConsumedDecisionId { get; set; }
    public List<Guid> ConsumedDecisionIds { get; set; } = [];
}

public sealed class PendingDecisionState
{
    public Guid DecisionId { get; set; }
    public DecisionKindId Kind { get; set; }
    public int PlayerId { get; set; }
    public List<DecisionOptionId> AllowedResponses { get; set; } = [];
    public SpaceId SpaceId { get; set; }
    public ResourceId ResourceId { get; set; }
    public int ResourceAmount { get; set; }
}

public sealed class TurnContinuationState
{
    public int PlayerId { get; set; }
    public RandomPurpose DicePurpose { get; set; }
    public List<int> DiceResults { get; set; } = [];
    public int DiceSum { get; set; }
    public SpaceId SpaceId { get; set; }
    public int NextCapabilityIndex { get; set; }
}

public static class GameProgressStateMapper
{
    public static GameProgressState ToState(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        return new GameProgressState
        {
            Phase = game.Phase,
            PendingDecision = game.PendingDecision is PurchaseDecision purchase ? MapDecision(purchase) : null,
            Continuation = game.TurnContinuationSnapshot is TurnContinuation continuation ? MapContinuation(continuation) : null,
            LastConsumedDecisionId = game.LastConsumedDecisionId,
            ConsumedDecisionIds = game.ConsumedDecisionIds.OrderBy(id => id).ToList()
        };
    }

    private static PendingDecisionState MapDecision(PurchaseDecision decision) => new()
    {
        DecisionId = decision.DecisionId,
        Kind = decision.Kind,
        PlayerId = decision.PlayerId,
        AllowedResponses = decision.AllowedResponses.ToList(),
        SpaceId = decision.SpaceId,
        ResourceId = decision.Price.ResourceId,
        ResourceAmount = decision.Price.Value
    };

    private static TurnContinuationState MapContinuation(TurnContinuation continuation) => new()
    {
        PlayerId = continuation.PlayerId,
        DicePurpose = continuation.Roll.Purpose,
        DiceResults = continuation.Roll.Results.ToList(),
        DiceSum = continuation.Roll.Sum,
        SpaceId = continuation.SpaceId,
        NextCapabilityIndex = continuation.NextCapabilityIndex
    };
}
