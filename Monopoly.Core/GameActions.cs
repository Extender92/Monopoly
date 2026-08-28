using System.Collections.ObjectModel;

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

public enum DecisionKind
{
    PropertyPurchase,
    JailRelease
}

public enum DecisionOption
{
    Purchase,
    Decline,
    LeaveJail,
    RollForDoubles
}

public enum GameActionRejectionReason
{
    PendingDecisionRequired,
    NoPendingDecision,
    MalformedResponse,
    StaleDecision,
    DuplicateDecision,
    ResponseNotAllowed,
    OperationInProgress
}

public sealed record DecisionResponse(Guid DecisionId, DecisionOption Response);

public abstract class PendingDecision
{
    private readonly ReadOnlyCollection<DecisionOption> _allowedResponses;

    protected PendingDecision(
        Guid decisionId,
        int playerId,
        DecisionKind kind,
        IEnumerable<DecisionOption> allowedResponses)
    {
        if (decisionId == Guid.Empty) throw new ArgumentException("A decision ID cannot be empty.", nameof(decisionId));
        if (playerId < 0) throw new ArgumentOutOfRangeException(nameof(playerId));
        ArgumentNullException.ThrowIfNull(allowedResponses);

        DecisionOption[] responses = allowedResponses.ToArray();
        if (responses.Length == 0 || responses.Any(response => !Enum.IsDefined(response)) || responses.Distinct().Count() != responses.Length)
            throw new ArgumentException("Allowed responses must contain unique, defined values.", nameof(allowedResponses));

        DecisionId = decisionId;
        PlayerId = playerId;
        Kind = kind;
        _allowedResponses = Array.AsReadOnly(responses);
    }

    public Guid DecisionId { get; }
    public int PlayerId { get; }
    public DecisionKind Kind { get; }
    public IReadOnlyList<DecisionOption> AllowedResponses => _allowedResponses;
}

public sealed class PropertyPurchaseDecision : PendingDecision
{
    internal PropertyPurchaseDecision(Guid decisionId, int playerId, int squarePosition, int price)
        : base(
            decisionId,
            playerId,
            DecisionKind.PropertyPurchase,
            [DecisionOption.Purchase, DecisionOption.Decline])
    {
        if (squarePosition < 0) throw new ArgumentOutOfRangeException(nameof(squarePosition));
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));

        SquarePosition = squarePosition;
        Price = price;
    }

    public int SquarePosition { get; }
    public int Price { get; }
}

public sealed class JailReleaseDecision : PendingDecision
{
    internal JailReleaseDecision(
        Guid decisionId,
        int playerId,
        int fine,
        bool hasGetOutOfJailCard,
        int turnsInJail,
        int maximumTurnsInJail)
        : base(
            decisionId,
            playerId,
            DecisionKind.JailRelease,
            [DecisionOption.LeaveJail, DecisionOption.RollForDoubles])
    {
        if (fine < 0) throw new ArgumentOutOfRangeException(nameof(fine));
        if (turnsInJail < 0) throw new ArgumentOutOfRangeException(nameof(turnsInJail));
        if (maximumTurnsInJail <= 0) throw new ArgumentOutOfRangeException(nameof(maximumTurnsInJail));

        Fine = fine;
        HasGetOutOfJailCard = hasGetOutOfJailCard;
        TurnsInJail = turnsInJail;
        MaximumTurnsInJail = maximumTurnsInJail;
    }

    public int Fine { get; }
    public bool HasGetOutOfJailCard { get; }
    public int TurnsInJail { get; }
    public int MaximumTurnsInJail { get; }
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
