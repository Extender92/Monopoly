namespace Monopoly.Core.Persistence;

public enum SaveStoreErrorKind
{
    NotFound,
    InvalidData,
    IncompatibleVersion,
    StorageFailure
}

public sealed class SaveStoreException : Exception
{
    public SaveStoreException(SaveStoreErrorKind kind, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
    }

    public SaveStoreErrorKind Kind { get; }
}

public sealed class GameStateValidationException : Exception
{
    public GameStateValidationException(string message)
        : base(message)
    {
    }
}
