namespace Monopoly.Core.Randomness;

/// <summary>A normal non-cryptographic source for one production match.</summary>
public sealed class SystemMatchRandomSource : IMatchRandomSource
{
    private readonly Random _random = new();

    public int NextInt(RandomRequest request)
    {
        RandomRequest validatedRequest = new(
            request.Purpose,
            request.MinimumInclusive,
            request.MaximumExclusive,
            request.SequenceIndex);
        return _random.Next(validatedRequest.MinimumInclusive, validatedRequest.MaximumExclusive);
    }
}
