namespace Monopoly.Core.Randomness;

public enum RandomSourceErrorKind
{
    Exhausted,
    OutOfRange,
    SourceFailure
}

/// <summary>Describes a random source that could not satisfy a validated Core request.</summary>
public sealed class RandomSourceException : Exception
{
    public RandomSourceException(
        RandomSourceErrorKind kind,
        RandomRequest request,
        string message,
        int? returnedValue = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));

        Kind = kind;
        Request = request;
        ReturnedValue = returnedValue;
    }

    public RandomSourceErrorKind Kind { get; }
    public RandomRequest Request { get; }
    public int? ReturnedValue { get; }
}
