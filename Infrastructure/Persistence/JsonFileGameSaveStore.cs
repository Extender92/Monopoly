using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Monopoly.Core;
using Monopoly.Core.Persistence;
using Monopoly.Core.Randomness;

namespace Infrastructure.Persistence;

/// <summary>Strict Save Format Version 2 storage with atomic file promotion.</summary>
public sealed class JsonFileGameSaveStore : IGameSaveStore
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        MaxDepth = GameSaveFormat.MaximumJsonDepth,
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true
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

        byte[] content;
        try
        {
            GameStateV2 state = GameStateV2Mapper.Capture(game);
            _ = GameStateV2Mapper.Restore(state, game.Profile, ValidationOnlyRandomSource.Instance);
            content = JsonSerializer.SerializeToUtf8Bytes(ToDto(state), JsonOptions);
        }
        catch (GameStateValidationException exception)
        {
            throw InvalidData("The active match cannot be represented as a valid Version 2 save.", exception);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or JsonException)
        {
            throw InvalidData("The active match could not be serialized as Save Format Version 2.", exception);
        }

        string temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.temp";
        try
        {
            using (IFileWriteSession session = _files.CreateNewWriteSession(temporaryPath))
            {
                session.Write(content);
                session.FlushToDisk();
            }

            if (_files.Exists(_filePath))
                _files.Replace(temporaryPath, _filePath);
            else
                _files.Move(temporaryPath, _filePath);
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            TryDelete(temporaryPath);
            throw new SaveStoreException(
                SaveStoreErrorKind.StorageFailure,
                "The save file could not be written atomically.",
                exception);
        }
    }

    public Game Load(GameProfileRegistry profiles, IMatchRandomSource? randomSource = null)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        byte[] content;
        try
        {
            content = _files.ReadBytes(_filePath, GameSaveFormat.MaximumInputBytes);
        }
        catch (FileContentLimitExceededException exception)
        {
            throw InvalidData($"The save file exceeds {GameSaveFormat.MaximumInputBytes} bytes.", exception);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new SaveStoreException(SaveStoreErrorKind.NotFound, "The save file was not found.", exception);
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            throw new SaveStoreException(SaveStoreErrorKind.StorageFailure, "The save file could not be read.", exception);
        }

        GameStateV2 state = Parse(content);
        ValidatedGameProfile profile;
        try
        {
            profile = profiles.ResolveExact(state.ProfileId, state.ProfileRevision, state.ProfileFingerprint);
        }
        catch (GameProfileResolutionException exception)
        {
            throw new SaveStoreException(
                SaveStoreErrorKind.IncompatibleProfile,
                "The exact profile required by the save is not registered.",
                exception);
        }

        try
        {
            return GameStateV2Mapper.Restore(state, profile, randomSource);
        }
        catch (GameStateValidationException exception) when (
            exception.Kind == GameStateValidationErrorKind.UnsupportedModuleVersion)
        {
            throw new SaveStoreException(
                SaveStoreErrorKind.IncompatibleVersion,
                "The save uses a module-state version that is not supported.",
                exception);
        }
        catch (GameStateValidationException exception)
        {
            throw InvalidData("The save contains an invalid or inconsistent match state.", exception);
        }
    }

    private GameStateV2 Parse(ReadOnlySpan<byte> input)
    {
        if (input.Length == 0)
            throw InvalidData("The save file is empty.");
        if (input.Length > GameSaveFormat.MaximumInputBytes)
            throw InvalidData($"The save file exceeds {GameSaveFormat.MaximumInputBytes} bytes.");

        ReadOnlySpan<byte> json = input;
        if (json.StartsWith(new byte[] { 239, 187, 191 })) json = json[3..];
        if (json.StartsWith(new byte[] { 255, 254 }) || json.StartsWith(new byte[] { 254, 255 }) ||
            json.Length >= 4 && ((json[0] != 0 && json[1] == 0 && json[3] == 0) || (json[0] == 0 && json[1] != 0 && json[2] == 0)))
        {
            throw InvalidData("Save Format Version 2 must use UTF-8.");
        }
        try
        {
            _ = StrictUtf8.GetCharCount(json);
        }
        catch (DecoderFallbackException exception)
        {
            throw InvalidData("The save file contains invalid UTF-8.", exception);
        }

        InspectMembers(json);
        InspectFormatVersion(json);
        GameSaveDocumentDto document;
        try
        {
            document = JsonSerializer.Deserialize<GameSaveDocumentDto>(json, JsonOptions)
                ?? throw InvalidData("The save file must contain one JSON object.");
        }
        catch (SaveStoreException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw InvalidData("The save JSON does not match the Version 2 schema.", exception);
        }

        if (document.FormatVersion != GameSaveFormat.Version2)
        {
            throw new SaveStoreException(
                SaveStoreErrorKind.IncompatibleVersion,
                $"Save format version '{document.FormatVersion}' is not supported.");
        }

        try
        {
            return FromDto(document);
        }
        catch (SaveStoreException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            throw InvalidData("The save contains invalid Version 2 values.", exception);
        }
    }

    private static void InspectMembers(ReadOnlySpan<byte> json)
    {
        try
        {
            Utf8JsonReader reader = new(json, new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = GameSaveFormat.MaximumJsonDepth
            });
            Stack<HashSet<string>> objects = [];
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                        objects.Push(new HashSet<string>(StringComparer.Ordinal));
                        break;
                    case JsonTokenType.PropertyName:
                        string name = reader.GetString()!;
                        if (!objects.Peek().Add(name))
                            throw InvalidData($"JSON member '{name}' is duplicated in the same object.");
                        if (objects.Count == 1 && name.Equals("Version", StringComparison.OrdinalIgnoreCase))
                            throw new SaveStoreException(SaveStoreErrorKind.IncompatibleVersion, "The save uses a retired legacy format.");
                        break;
                    case JsonTokenType.EndObject:
                        objects.Pop();
                        break;
                }
            }
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

    private static void InspectFormatVersion(ReadOnlySpan<byte> json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json.ToArray(), new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = GameSaveFormat.MaximumJsonDepth
            });
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw InvalidData("The save file must contain one JSON object.");
            if (!document.RootElement.TryGetProperty("formatVersion", out JsonElement version))
                throw InvalidData("The required field 'formatVersion' is missing.");
            if (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int value))
                throw InvalidData("The save format version must be an integer.");
            if (value != GameSaveFormat.Version2)
            {
                throw new SaveStoreException(
                    SaveStoreErrorKind.IncompatibleVersion,
                    $"Save format version '{value}' is not supported.");
            }
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

    private static GameSaveDocumentDto ToDto(GameStateV2 state) => new()
    {
        FormatVersion = state.FormatVersion,
        Profile = new SavedProfileDto
        {
            Id = state.ProfileId.Value,
            Revision = state.ProfileRevision.Value,
            Fingerprint = state.ProfileFingerprint.Value
        },
        Match = new SavedMatchDto
        {
            Players = state.Players.Select(player => (SavedPlayerDto?)new SavedPlayerDto
            {
                PlayerId = player.PlayerId,
                Name = player.Name,
                SpaceId = player.SpaceId.Value,
                Resources = player.Resources.Select(resource => (SavedResourceDto?)new SavedResourceDto
                {
                    ResourceId = resource.ResourceId.Value,
                    Value = resource.Value
                }).ToList()
            }).ToList(),
            CurrentPlayerId = state.CurrentPlayerId,
            RoundAnchorPlayerId = state.RoundAnchorPlayerId,
            RoundNumber = state.RoundNumber,
            Phase = PhaseName(state.Phase),
            LastDiceRoll = state.LastDiceRoll is null ? null : new SavedDiceRollDto
            {
                Purpose = PurposeName(state.LastDiceRoll.Purpose),
                Results = state.LastDiceRoll.Results.ToList()
            },
            WinnerPlayerId = state.WinnerPlayerId,
            Decks = state.Decks.Select(deck => (SavedDeckDto?)new SavedDeckDto
            {
                DeckId = deck.DeckId.Value,
                CardIds = deck.CardIds.Select(id => (string?)id.Value).ToList()
            }).ToList(),
            ModuleState = new SavedModuleStateDto
            {
                Ownership = new SavedOwnershipModuleDto
                {
                    Version = state.ModuleState.OwnershipVersion,
                    Entries = state.ModuleState.Ownership.Select(entry => (SavedOwnershipDto?)new SavedOwnershipDto
                    {
                        SpaceId = entry.SpaceId.Value,
                        OwnerPlayerId = entry.OwnerPlayerId
                    }).ToList()
                },
                Statuses = new SavedStatusModuleDto
                {
                    Version = state.ModuleState.StatusVersion,
                    Entries = state.ModuleState.Statuses.Select(entry => (SavedStatusDto?)new SavedStatusDto
                    {
                        PlayerId = entry.PlayerId,
                        StatusId = entry.StatusId.Value,
                        Value = entry.Value
                    }).ToList()
                }
            },
            PendingDecision = state.PendingDecision is null ? null : new SavedPendingDecisionDto
            {
                DecisionId = state.PendingDecision.DecisionId,
                Kind = state.PendingDecision.Kind.Value,
                PlayerId = state.PendingDecision.PlayerId,
                AllowedResponses = state.PendingDecision.AllowedResponses.Select(response => (string?)response.Value).ToList(),
                SpaceId = state.PendingDecision.SpaceId.Value,
                Price = new SavedResourceDto
                {
                    ResourceId = state.PendingDecision.ResourceId.Value,
                    Value = state.PendingDecision.ResourceAmount
                }
            },
            Continuation = state.Continuation is null ? null : new SavedContinuationDto
            {
                PlayerId = state.Continuation.PlayerId,
                SpaceId = state.Continuation.SpaceId.Value,
                NextCapabilityIndex = state.Continuation.NextCapabilityIndex
            },
            ConsumedDecisionIds = state.ConsumedDecisionIds.ToList(),
            LastConsumedDecisionId = state.LastConsumedDecisionId
        }
    };

    private static GameStateV2 FromDto(GameSaveDocumentDto document)
    {
        SavedProfileDto profile = Required(document.Profile, "profile");
        SavedMatchDto match = Required(document.Match, "match");
        SavedModuleStateDto modules = Required(match.ModuleState, "match.moduleState");
        SavedOwnershipModuleDto ownership = Required(modules.Ownership, "match.moduleState.ownership");
        SavedStatusModuleDto statuses = Required(modules.Statuses, "match.moduleState.statuses");

        return new GameStateV2(
            Required(document.FormatVersion, "formatVersion"),
            Id(profile.Id, "profile.id", value => new ProfileId(value)),
            new ProfileRevision(Required(profile.Revision, "profile.revision")),
            new ProfileFingerprint(Required(profile.Fingerprint, "profile.fingerprint")),
            Required(match.Players, "match.players").Select((player, index) =>
            {
                SavedPlayerDto entry = Required(player, $"match.players[{index}]");
                return new PlayerStateV2(
                    Required(entry.PlayerId, $"match.players[{index}].playerId"),
                    Required(entry.Name, $"match.players[{index}].name"),
                    Id(entry.SpaceId, $"match.players[{index}].spaceId", value => new SpaceId(value)),
                    Required(entry.Resources, $"match.players[{index}].resources").Select((resource, resourceIndex) =>
                    {
                        SavedResourceDto amount = Required(resource, $"match.players[{index}].resources[{resourceIndex}]");
                        return new ResourceBalanceStateV2(
                            Id(amount.ResourceId, $"match.players[{index}].resources[{resourceIndex}].resourceId", value => new ResourceId(value)),
                            Required(amount.Value, $"match.players[{index}].resources[{resourceIndex}].value"));
                    }));
            }),
            Required(match.CurrentPlayerId, "match.currentPlayerId"),
            Required(match.RoundAnchorPlayerId, "match.roundAnchorPlayerId"),
            Required(match.RoundNumber, "match.roundNumber"),
            ParsePhase(match.Phase),
            match.LastDiceRoll is null ? null : new DiceRollStateV2(
                ParsePurpose(match.LastDiceRoll.Purpose),
                Required(match.LastDiceRoll.Results, "match.lastDiceRoll.results")),
            match.WinnerPlayerId,
            Required(match.Decks, "match.decks").Select((deck, index) =>
            {
                SavedDeckDto entry = Required(deck, $"match.decks[{index}]");
                return new DeckStateV2(
                    Id(entry.DeckId, $"match.decks[{index}].deckId", value => new DeckId(value)),
                    Required(entry.CardIds, $"match.decks[{index}].cardIds")
                        .Select((id, cardIndex) => Id(id, $"match.decks[{index}].cardIds[{cardIndex}]", value => new CardId(value))));
            }),
            new ModuleStateV2(
                Required(ownership.Version, "match.moduleState.ownership.version"),
                Required(ownership.Entries, "match.moduleState.ownership.entries").Select((entry, index) =>
                {
                    SavedOwnershipDto value = Required(entry, $"match.moduleState.ownership.entries[{index}]");
                    return new OwnershipStateV2(
                        Id(value.SpaceId, $"match.moduleState.ownership.entries[{index}].spaceId", id => new SpaceId(id)),
                        value.OwnerPlayerId);
                }),
                Required(statuses.Version, "match.moduleState.statuses.version"),
                Required(statuses.Entries, "match.moduleState.statuses.entries").Select((entry, index) =>
                {
                    SavedStatusDto value = Required(entry, $"match.moduleState.statuses.entries[{index}]");
                    return new PlayerStatusStateV2(
                        Required(value.PlayerId, $"match.moduleState.statuses.entries[{index}].playerId"),
                        Id(value.StatusId, $"match.moduleState.statuses.entries[{index}].statusId", id => new StatusId(id)),
                        Required(value.Value, $"match.moduleState.statuses.entries[{index}].value"));
                })),
            match.PendingDecision is null ? null : MapDecision(match.PendingDecision),
            match.Continuation is null ? null : new TurnContinuationStateV2(
                Required(match.Continuation.PlayerId, "match.continuation.playerId"),
                Id(match.Continuation.SpaceId, "match.continuation.spaceId", value => new SpaceId(value)),
                Required(match.Continuation.NextCapabilityIndex, "match.continuation.nextCapabilityIndex")),
            Required(match.ConsumedDecisionIds, "match.consumedDecisionIds"),
            match.LastConsumedDecisionId);
    }

    private static PendingDecisionStateV2 MapDecision(SavedPendingDecisionDto decision)
    {
        SavedResourceDto price = Required(decision.Price, "match.pendingDecision.price");
        return new PendingDecisionStateV2(
            Required(decision.DecisionId, "match.pendingDecision.decisionId"),
            Id(decision.Kind, "match.pendingDecision.kind", value => new DecisionKindId(value)),
            Required(decision.PlayerId, "match.pendingDecision.playerId"),
            Required(decision.AllowedResponses, "match.pendingDecision.allowedResponses")
                .Select((response, index) => Id(response, $"match.pendingDecision.allowedResponses[{index}]", value => new DecisionOptionId(value))),
            Id(decision.SpaceId, "match.pendingDecision.spaceId", value => new SpaceId(value)),
            Id(price.ResourceId, "match.pendingDecision.price.resourceId", value => new ResourceId(value)),
            Required(price.Value, "match.pendingDecision.price.value"));
    }

    private static string PhaseName(GamePhase phase) => phase switch
    {
        GamePhase.ReadyForTurn => "ready-for-turn",
        GamePhase.AwaitingDecision => "awaiting-decision",
        GamePhase.GameOver => "game-over",
        _ => throw new ArgumentOutOfRangeException(nameof(phase))
    };

    private static GamePhase ParsePhase(string? value) => value switch
    {
        "ready-for-turn" => GamePhase.ReadyForTurn,
        "awaiting-decision" => GamePhase.AwaitingDecision,
        "game-over" => GamePhase.GameOver,
        _ => throw InvalidData("The saved phase is not supported.")
    };

    private static string PurposeName(RandomPurpose purpose) => purpose switch
    {
        RandomPurpose.TurnDice => "turn-dice",
        _ => throw new ArgumentOutOfRangeException(nameof(purpose))
    };

    private static RandomPurpose ParsePurpose(string? value) => value switch
    {
        "turn-dice" => RandomPurpose.TurnDice,
        _ => throw InvalidData("The saved dice purpose is not supported.")
    };

    private static TId Id<TId>(string? value, string path, Func<string, TId> factory)
    {
        try
        {
            return factory(Required(value, path));
        }
        catch (ArgumentException exception)
        {
            throw InvalidData($"The value at '{path}' is invalid.", exception);
        }
    }

    private static T Required<T>(T? value, string path) where T : class =>
        value ?? throw InvalidData($"The required field '{path}' is missing or null.");

    private static string Required(string? value, string path) =>
        value ?? throw InvalidData($"The required field '{path}' is missing or null.");

    private static int Required(int? value, string path) =>
        value ?? throw InvalidData($"The required field '{path}' is missing or null.");

    private static Guid Required(Guid? value, string path) =>
        value ?? throw InvalidData($"The required field '{path}' is missing or null.");

    private void TryDelete(string path)
    {
        try
        {
            if (_files.Exists(path)) _files.Delete(path);
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            // Cleanup is best-effort and must not hide the original storage failure.
        }
    }

    private static SaveStoreException InvalidData(string message, Exception? innerException = null) =>
        new(SaveStoreErrorKind.InvalidData, message, innerException);

    private static bool IsStorageException(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or SecurityException or PlatformNotSupportedException or NotSupportedException;

    private sealed class ValidationOnlyRandomSource : IMatchRandomSource
    {
        internal static ValidationOnlyRandomSource Instance { get; } = new();

        public int NextInt(RandomRequest request) =>
            throw new InvalidOperationException("Whole-state validation must not consume runtime randomness.");
    }
}
