namespace Infrastructure.Profiles;

public enum ProfileSourceErrorKind
{
    NotFound,
    AccessDenied,
    InvalidPath,
    StorageFailure
}

/// <summary>A sanitized technical failure while opening or reading a profile source.</summary>
public sealed class ProfileSourceException : Exception
{
    public ProfileSourceException(ProfileSourceErrorKind kind, string message)
        : base(message)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Kind = kind;
    }

    public ProfileSourceErrorKind Kind { get; }
}
