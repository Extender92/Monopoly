using System.Collections.ObjectModel;
using Monopoly.Core.Presentation;

namespace Monopoly.Core;

public enum GamePhase
{
    ReadyForTurn,
    AwaitingDecision,
    GameOver
}

public enum GameActionStatus
{
    TurnCompleted,
    DecisionRequired,
    GameOver,
    Rejected
}

public static class DecisionKinds
{
    public static DecisionKindId Purchase { get; } = new("purchase");
    public static DecisionKindId Status { get; } = new("status");
}

public static class DecisionOptions
{
    public static DecisionOptionId Accept { get; } = new("accept");
    public static DecisionOptionId Decline { get; } = new("decline");
    public static DecisionOptionId Resolve { get; } = new("resolve");
    public static DecisionOptionId Roll { get; } = new("roll");
}

public enum GameActionRejectionReason
{
    PendingDecisionRequired,
    NoPendingDecision,
    MalformedResponse,
    StaleDecision,
    DuplicateDecision,
    ResponseNotAllowed,
    OperationInProgress,
    CapabilityExecutionUnavailable
}

public sealed record DecisionResponse(Guid DecisionId, DecisionOptionId Response);

public abstract class PendingDecision
{
    private readonly ReadOnlyCollection<DecisionOptionId> _allowedResponses;

    protected PendingDecision(
        Guid decisionId,
        int playerId,
        DecisionKindId kind,
        PresentationToken presentationToken,
        IEnumerable<DecisionOptionId> allowedResponses)
    {
        if (decisionId == Guid.Empty) throw new ArgumentException("A decision ID cannot be empty.", nameof(decisionId));
        if (playerId < 0) throw new ArgumentOutOfRangeException(nameof(playerId));
        ArgumentNullException.ThrowIfNull(allowedResponses);

        DecisionOptionId[] responses = allowedResponses.ToArray();
        if (!kind.IsValid) throw new ArgumentException("The decision kind is invalid.", nameof(kind));
        if (responses.Length == 0 || responses.Any(response => !response.IsValid) || responses.Distinct().Count() != responses.Length)
            throw new ArgumentException("Allowed responses must contain unique, defined values.", nameof(allowedResponses));

        DecisionId = decisionId;
        PlayerId = playerId;
        Kind = kind;
        if (!presentationToken.IsValid) throw new ArgumentException("The decision presentation token is invalid.", nameof(presentationToken));
        PresentationToken = presentationToken;
        _allowedResponses = Array.AsReadOnly(responses);
    }

    public Guid DecisionId { get; }
    public int PlayerId { get; }
    public DecisionKindId Kind { get; }
    public PresentationToken PresentationToken { get; }
    public IReadOnlyList<DecisionOptionId> AllowedResponses => _allowedResponses;
}

public sealed class PurchaseDecision : PendingDecision
{
    internal PurchaseDecision(Guid decisionId, int playerId, SpaceId spaceId, ResourceAmount price)
        : base(
            decisionId,
            playerId,
            DecisionKinds.Purchase,
            PresentationTokens.PropertyPurchaseDecision,
            [DecisionOptions.Accept, DecisionOptions.Decline])
    {
        if (!spaceId.IsValid) throw new ArgumentException("The space ID is invalid.", nameof(spaceId));
        if (!price.IsValid) throw new ArgumentException("The price is invalid.", nameof(price));
        SpaceId = spaceId;
        Price = price;
    }

    public SpaceId SpaceId { get; }
    public ResourceAmount Price { get; }
}

internal sealed class StatusDecision : PendingDecision
{
    internal StatusDecision(
        Guid decisionId,
        int playerId,
        StatusId statusId,
        ResourceAmount cost,
        bool hasAlternative,
        int currentValue,
        int maximumValue)
        : base(
            decisionId,
            playerId,
            DecisionKinds.Status,
            PresentationTokens.DetentionReleaseDecision,
            [DecisionOptions.Resolve, DecisionOptions.Roll])
    {
        if (!statusId.IsValid) throw new ArgumentException("The status ID is invalid.", nameof(statusId));
        if (!cost.IsValid) throw new ArgumentException("The status cost is invalid.", nameof(cost));
        if (currentValue < 0) throw new ArgumentOutOfRangeException(nameof(currentValue));
        if (maximumValue <= 0 || currentValue > maximumValue) throw new ArgumentOutOfRangeException(nameof(maximumValue));
        StatusId = statusId;
        Cost = cost;
        HasAlternative = hasAlternative;
        CurrentValue = currentValue;
        MaximumValue = maximumValue;
    }

    internal StatusId StatusId { get; }
    internal ResourceAmount Cost { get; }
    internal bool HasAlternative { get; }
    internal int CurrentValue { get; }
    internal int MaximumValue { get; }
}

internal static class LegacyResourceIds
{
    internal static ResourceId Primary { get; } = new("resource.primary");
}

public sealed class GameActionResult
{
    private GameActionResult(
        GameActionStatus status,
        TurnResult? turnResult,
        PendingDecision? pendingDecision,
        GameActionRejectionReason? rejectionReason)
    {
        Status = status;
        TurnResult = turnResult;
        PendingDecision = pendingDecision;
        RejectionReason = rejectionReason;
    }

    public GameActionStatus Status { get; }
    public TurnResult? TurnResult { get; }
    public PendingDecision? PendingDecision { get; }
    public GameActionRejectionReason? RejectionReason { get; }

    internal static GameActionResult Completed(TurnResult result) =>
        new(GameActionStatus.TurnCompleted, result, null, null);

    internal static GameActionResult DecisionRequired(PendingDecision decision) =>
        new(GameActionStatus.DecisionRequired, null, decision, null);

    internal static GameActionResult Over(TurnResult result) =>
        new(GameActionStatus.GameOver, result, null, null);

    internal static GameActionResult Rejected(
        GameActionRejectionReason reason,
        PendingDecision? pendingDecision = null) =>
        new(GameActionStatus.Rejected, null, pendingDecision, reason);
}
