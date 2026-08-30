namespace Monopoly.Core.Persistence;

public enum SaveStoreErrorKind
{
    NotFound,
    InvalidData,
    IncompatibleVersion,
    IncompatibleProfile,
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

public enum GameStateValidationErrorKind
{
    InvalidValue,
    DuplicateEntry,
    BrokenReference,
    InconsistentState,
    UnsupportedModuleVersion
}

public sealed class GameStateValidationException : Exception
{
    public GameStateValidationException(
        GameStateValidationErrorKind kind,
        string path,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Kind = kind;
        Path = path;
    }

    public GameStateValidationErrorKind Kind { get; }
    public string Path { get; }
}

public enum GameProfileResolutionErrorKind
{
    NotRegistered,
    FingerprintMismatch
}

public sealed class GameProfileResolutionException : Exception
{
    internal GameProfileResolutionException(
        GameProfileResolutionErrorKind kind,
        string message)
        : base(message)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        Kind = kind;
    }

    public GameProfileResolutionErrorKind Kind { get; }
}
