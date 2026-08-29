namespace Monopoly.Core.Randomness;

internal sealed class MatchRandomizer
{
    private readonly IMatchRandomSource _source;

    internal MatchRandomizer(IMatchRandomSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    internal int NextInt(RandomRequest request)
    {
        RandomRequest validatedRequest = new(
            request.Purpose,
            request.MinimumInclusive,
            request.MaximumExclusive,
            request.SequenceIndex);

        int value;
        try
        {
            value = _source.NextInt(validatedRequest);
        }
        catch (RandomSourceException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new RandomSourceException(
                RandomSourceErrorKind.SourceFailure,
                validatedRequest,
                $"The match random source failed while handling {validatedRequest.Purpose}.",
                innerException: exception);
        }

        if (value < validatedRequest.MinimumInclusive || value >= validatedRequest.MaximumExclusive)
        {
            throw new RandomSourceException(
                RandomSourceErrorKind.OutOfRange,
                validatedRequest,
                $"The match random source returned {value}, outside [{validatedRequest.MinimumInclusive}, {validatedRequest.MaximumExclusive}).",
                value);
        }

        return value;
    }
}
