using System.Collections.ObjectModel;

namespace Monopoly.Core;

internal enum TurnContinuationKind
{
    StandardLanding,
    JailDoubleLanding
}

internal sealed class TurnContinuation
{
    private readonly ReadOnlyCollection<int> _diceResults;

    internal TurnContinuation(
        TurnContinuationKind kind,
        int playerId,
        IEnumerable<int> diceResults,
        int diceSum,
        int landedSquarePosition,
        bool wasDouble,
        bool wasReleasedFromJailByDouble)
    {
        ArgumentNullException.ThrowIfNull(diceResults);
        int[] copiedResults = diceResults.ToArray();
        if (playerId < 0) throw new ArgumentOutOfRangeException(nameof(playerId));
        if (copiedResults.Any(result => result <= 0)) throw new ArgumentOutOfRangeException(nameof(diceResults));
        if (diceSum < 0 || diceSum != copiedResults.Sum()) throw new ArgumentOutOfRangeException(nameof(diceSum));
        if (landedSquarePosition < 0) throw new ArgumentOutOfRangeException(nameof(landedSquarePosition));

        Kind = kind;
        PlayerId = playerId;
        _diceResults = Array.AsReadOnly(copiedResults);
        DiceSum = diceSum;
        LandedSquarePosition = landedSquarePosition;
        WasDouble = wasDouble;
        WasReleasedFromJailByDouble = wasReleasedFromJailByDouble;
    }

    internal TurnContinuationKind Kind { get; }
    internal int PlayerId { get; }
    internal IReadOnlyList<int> DiceResults => _diceResults;
    internal int DiceSum { get; }
    internal int LandedSquarePosition { get; }
    internal bool WasDouble { get; }
    internal bool WasReleasedFromJailByDouble { get; }
}
