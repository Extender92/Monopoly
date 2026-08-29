namespace Monopoly.Core.Randomness;

/// <summary>A validated request for one integer from a match-scoped random source.</summary>
public readonly record struct RandomRequest
{
    public RandomRequest(
        RandomPurpose purpose,
        int minimumInclusive,
        int maximumExclusive,
        int sequenceIndex)
    {
        if (!Enum.IsDefined(purpose))
            throw new ArgumentOutOfRangeException(nameof(purpose));
        if (minimumInclusive >= maximumExclusive)
            throw new ArgumentOutOfRangeException(nameof(maximumExclusive), "The exclusive maximum must be greater than the inclusive minimum.");
        if (sequenceIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(sequenceIndex));

        Purpose = purpose;
        MinimumInclusive = minimumInclusive;
        MaximumExclusive = maximumExclusive;
        SequenceIndex = sequenceIndex;
    }

    public RandomPurpose Purpose { get; }
    public int MinimumInclusive { get; }
    public int MaximumExclusive { get; }
    public int SequenceIndex { get; }
}
