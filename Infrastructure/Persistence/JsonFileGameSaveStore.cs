using System.Security;
using System.Text.Json;
using Monopoly.Core;
using Monopoly.Core.Interface;
using Monopoly.Core.Persistence;
using Monopoly.Core.Randomness;

namespace Infrastructure.Persistence;

/// <summary>
/// Transitional persistence boundary while the regional Version 1 format has
/// been retired and Version 2 is not yet available.
/// </summary>
public sealed class JsonFileGameSaveStore : IGameSaveStore
{
    private readonly string _filePath;
    private readonly IFileOperations _files;

    public JsonFileGameSaveStore(string filePath)
        : this(filePath, new PhysicalFileOperations())
    {
    }

    internal JsonFileGameSaveStore(string filePath, IFileOperations files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(files);

        _filePath = Path.GetFullPath(filePath);
        _files = files;
    }

    public void Save(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        throw CompatibilityGap("Saving is unavailable until Save Format Version 2 is implemented.");
    }

    public Game Load(
        IPlayerDecisionProvider? decisions = null,
        IMatchRandomSource? randomSource = null)
    {
        string serializedState;
        try
        {
            serializedState = _files.ReadAllText(_filePath);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new SaveStoreException(
                SaveStoreErrorKind.NotFound,
                $"Save file '{_filePath}' was not found.",
                exception);
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            throw new SaveStoreException(
                SaveStoreErrorKind.StorageFailure,
                $"Save file '{_filePath}' could not be read.",
                exception);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(serializedState);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw InvalidData("The save file must contain a JSON object.");

            JsonProperty? versionProperty = document.RootElement
                .EnumerateObject()
                .Cast<JsonProperty?>()
                .FirstOrDefault(property => property is not null &&
                    property.Value.Name.Equals("Version", StringComparison.OrdinalIgnoreCase));
            if (versionProperty is null)
                throw CompatibilityGap("The save file has no supported format version.");

            if (versionProperty.Value.Value.ValueKind != JsonValueKind.Number ||
                !versionProperty.Value.Value.TryGetInt32(out int version))
                throw InvalidData("The save format version must be an integer.");

            string message = version == 1
                ? "Save Format Version 1 is no longer supported. Version 2 is not available yet."
                : $"Save format version '{version}' is not supported. Version 2 is not available yet.";
            throw CompatibilityGap(message);
        }
        catch (SaveStoreException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw InvalidData("The save file contains malformed JSON.", exception);
        }
    }

    private static SaveStoreException CompatibilityGap(string message) =>
        new(SaveStoreErrorKind.IncompatibleVersion, message);

    private static SaveStoreException InvalidData(string message, Exception? innerException = null) =>
        new(SaveStoreErrorKind.InvalidData, message, innerException);

    private static bool IsStorageException(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or SecurityException or PlatformNotSupportedException;
}
