using Monopoly.Core.Randomness;

namespace Monopoly.Core;

internal enum TurnContinuationKind
{
    StandardLanding,
    JailDoubleLanding
}

internal sealed class TurnContinuation
{
    internal TurnContinuation(
        TurnContinuationKind kind,
        int playerId,
        DiceRoll roll,
        int landedSquarePosition,
        bool wasReleasedFromJailByDouble)
    {
        if (playerId < 0) throw new ArgumentOutOfRangeException(nameof(playerId));
        ArgumentNullException.ThrowIfNull(roll);
        if (landedSquarePosition < 0) throw new ArgumentOutOfRangeException(nameof(landedSquarePosition));

        Kind = kind;
        PlayerId = playerId;
        Roll = roll;
        LandedSquarePosition = landedSquarePosition;
        WasReleasedFromJailByDouble = wasReleasedFromJailByDouble;
    }

    internal TurnContinuationKind Kind { get; }
    internal int PlayerId { get; }
    internal DiceRoll Roll { get; }
    internal int LandedSquarePosition { get; }
    internal bool WasReleasedFromJailByDouble { get; }
}
