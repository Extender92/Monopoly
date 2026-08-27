using System.Security;
using System.Text.Json;
using Monopoly.Core;
using Monopoly.Core.Interface;
using Monopoly.Core.Persistence;

namespace Infrastructure.Persistence;

public sealed class JsonFileGameSaveStore : IGameSaveStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

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

        GameStateV1 state = GameStateV1Mapper.ToState(game);
        string serializedState = JsonSerializer.Serialize(state, JsonOptions);
        string directory = Path.GetDirectoryName(_filePath)
            ?? throw new ArgumentException("The configured save path has no parent directory.", nameof(_filePath));
        string temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(_filePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (IFileWriteSession session = _files.CreateNewWriteSession(temporaryPath))
            {
                session.Write(serializedState);
                session.FlushToDisk();
            }

            if (_files.Exists(_filePath))
                _files.Replace(temporaryPath, _filePath);
            else
                _files.Move(temporaryPath, _filePath);

            temporaryPath = string.Empty;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            throw new SaveStoreException(
                SaveStoreErrorKind.StorageFailure,
                $"The save file '{_filePath}' could not be written.",
                exception);
        }
        finally
        {
            if (temporaryPath.Length > 0)
                TryDeleteTemporaryFile(temporaryPath);
        }
    }

    public Game Load(IPlayerDecisionProvider? decisions = null)
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
                $"The save file '{_filePath}' could not be read.",
                exception);
        }

        VersionEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<VersionEnvelope>(serializedState, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw InvalidData("The save file contains malformed JSON.", exception);
        }

        if (envelope?.Version != GameStateV1Mapper.CurrentVersion)
        {
            throw new SaveStoreException(
                SaveStoreErrorKind.IncompatibleVersion,
                $"Unsupported or missing save version. Expected version {GameStateV1Mapper.CurrentVersion}.");
        }

        GameStateV1? state;
        try
        {
            state = JsonSerializer.Deserialize<GameStateV1>(serializedState, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw InvalidData("The save file contains invalid Version 1 data.", exception);
        }

        if (state is null)
            throw InvalidData("The save file does not contain Version 1 game state.");

        try
        {
            return GameStateV1Mapper.FromState(state, decisions);
        }
        catch (GameStateValidationException exception)
        {
            throw InvalidData(exception.Message, exception);
        }
    }

    private static SaveStoreException InvalidData(string message, Exception? innerException = null) =>
        new(SaveStoreErrorKind.InvalidData, message, innerException);

    private static bool IsStorageException(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or SecurityException or PlatformNotSupportedException;

    private void TryDeleteTemporaryFile(string temporaryPath)
    {
        try
        {
            _files.Delete(temporaryPath);
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            // Preserve the primary write failure. A later save can ignore stale temporary files.
        }
    }

    private sealed class VersionEnvelope
    {
        public int? Version { get; set; }
    }
}
