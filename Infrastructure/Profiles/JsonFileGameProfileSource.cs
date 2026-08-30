using System.Security;
using Monopoly.Core;

namespace Infrastructure.Profiles;

internal interface IProfileFileAccess
{
    Stream OpenRead(string path);
}

internal sealed class PhysicalProfileFileAccess : IProfileFileAccess
{
    public Stream OpenRead(string path)
    {
        if (Directory.Exists(path))
            throw new ArgumentException("The selected profile path names a directory.", nameof(path));

        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.SequentialScan);
    }
}

/// <summary>Loads one explicitly named JSON profile file through the bounded parser.</summary>
public sealed class JsonFileGameProfileSource : IGameProfileSource
{
    private readonly string _path;
    private readonly IProfileFileAccess _files;
    private readonly JsonGameProfileParser _parser;

    public JsonFileGameProfileSource(string path)
        : this(path, new PhysicalProfileFileAccess(), new JsonGameProfileParser())
    {
    }

    internal JsonFileGameProfileSource(
        string path,
        IProfileFileAccess files,
        JsonGameProfileParser parser)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw Error(ProfileSourceErrorKind.InvalidPath, "A profile path is required.");

        _path = path;
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    public ValidatedGameProfile Load()
    {
        try
        {
            using Stream input = _files.OpenRead(_path);
            return _parser.Parse(input);
        }
        catch (ProfileJsonException exception) when (IsTechnicalReadFailure(exception.InnerException))
        {
            throw Error(ProfileSourceErrorKind.StorageFailure, "The profile file could not be read.");
        }
        catch (ProfileJsonException)
        {
            throw;
        }
        catch (ProfileValidationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            throw Error(ProfileSourceErrorKind.NotFound, "The profile file was not found.");
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or SecurityException)
        {
            throw Error(ProfileSourceErrorKind.AccessDenied, "Access to the profile file was denied.");
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw Error(ProfileSourceErrorKind.InvalidPath, "The profile path is invalid.");
        }
        catch (Exception exception) when (exception is IOException or PlatformNotSupportedException)
        {
            throw Error(ProfileSourceErrorKind.StorageFailure, "The profile file could not be read.");
        }
    }

    private static bool IsTechnicalReadFailure(Exception? exception) =>
        exception is IOException or NotSupportedException or ObjectDisposedException;

    private static ProfileSourceException Error(ProfileSourceErrorKind kind, string message) =>
        new(kind, message);
}
