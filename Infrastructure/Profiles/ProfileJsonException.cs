namespace Infrastructure.Profiles;

public enum ProfileJsonErrorKind
{
    InputTooLarge,
    InvalidEncoding,
    MalformedJson,
    DepthExceeded,
    UnknownMember,
    DuplicateMember,
    UnsupportedSchemaVersion,
    InvalidWireValue
}

public sealed class ProfileJsonException : Exception
{
    public ProfileJsonException(ProfileJsonErrorKind kind, string path, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Kind = kind;
        Path = path;
    }

    public ProfileJsonErrorKind Kind { get; }
    public string Path { get; }
}
