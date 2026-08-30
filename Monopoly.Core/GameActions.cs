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
}

public static class DecisionOptions
{
    public static DecisionOptionId Accept { get; } = new("accept");
    public static DecisionOptionId Decline { get; } = new("decline");
}

public enum GameActionRejectionReason
{
    PendingDecisionRequired,
    NoPendingDecision,
    MalformedResponse,
    StaleDecision,
    DuplicateDecision,
    ResponseNotAllowed,
    WrongPlayer,
    InsufficientResources,
    DecisionPreconditionFailed,
    OperationInProgress
}

public enum ProfileExecutionErrorKind
{
    ResourceOverflow,
    InvalidRuntimeState,
    UnsupportedExecutionShape
}

public sealed class ProfileExecutionException : Exception
{
    internal ProfileExecutionException(
        ProfileExecutionErrorKind kind,
        string path,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Kind = kind;
        Path = path;
    }

    public ProfileExecutionErrorKind Kind { get; }
    public string Path { get; }
}

public sealed record DecisionResponse(Guid DecisionId, int PlayerId, DecisionOptionId Response);

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
        if (!kind.IsValid) throw new ArgumentException("The decision kind is invalid.", nameof(kind));
        if (!presentationToken.IsValid) throw new ArgumentException("The presentation token is invalid.", nameof(presentationToken));
        ArgumentNullException.ThrowIfNull(allowedResponses);

        DecisionOptionId[] responses = allowedResponses.ToArray();
        if (responses.Length == 0 || responses.Any(response => !response.IsValid) || responses.Distinct().Count() != responses.Length)
            throw new ArgumentException("Allowed responses must contain unique, defined values.", nameof(allowedResponses));

        DecisionId = decisionId;
        PlayerId = playerId;
        Kind = kind;
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
    internal PurchaseDecision(
        Guid decisionId,
        int playerId,
        SpaceId spaceId,
        ResourceAmount price,
        PresentationToken presentationToken)
        : base(
            decisionId,
            playerId,
            DecisionKinds.Purchase,
            presentationToken,
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
