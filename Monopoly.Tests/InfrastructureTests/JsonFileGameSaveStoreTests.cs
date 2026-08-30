using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Infrastructure.Persistence;
using Infrastructure.Profiles;
using Monopoly.Core;
using Monopoly.Core.Persistence;
using Monopoly.Tests.CoreTests;
using Monopoly.Tests.TestDoubles;

namespace Monopoly.Tests.InfrastructureTests;

public sealed class JsonFileGameSaveStoreTests
{
    private static readonly string DemoPath = Path.Combine(
        AppContext.BaseDirectory,
        "TestData",
        "Profiles",
        "Demo",
        "lantern-vale-v1.json");

    [Fact]
    public void TrackedSchemaDeclaresVersionModulesLimitsAndClosedObjects()
    {
        string schemaPath = Path.Combine(RepositoryRoot(), "schemas", "game-save-v2.schema.json");
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllBytes(schemaPath));
        JsonElement root = schema.RootElement;

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", root.GetProperty("$schema").GetString());
        Assert.Equal(GameSaveFormat.Version2, root.GetProperty("properties").GetProperty("formatVersion").GetProperty("const").GetInt32());
        JsonElement modules = root.GetProperty("$defs").GetProperty("moduleState").GetProperty("properties");
        Assert.Equal(GameSaveFormat.OwnershipModuleVersion, modules.GetProperty("ownership").GetProperty("properties").GetProperty("version").GetProperty("const").GetInt32());
        Assert.Equal(GameSaveFormat.StatusModuleVersion, modules.GetProperty("statuses").GetProperty("properties").GetProperty("version").GetProperty("const").GetInt32());
        AssertClosedObjects(root);
    }

    [Fact]
    public void FreshMatchRoundTripsWithoutConsumingTheLoadRandomSource()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(spaceCount: 4);
        Game original = GameSetup.Create(
            profile,
            [new PlayerSetup(17, "Aster"), new PlayerSetup(3, "Bramble")],
            new MinimumMatchRandomSource());
        using TemporaryDirectory directory = new();
        string path = directory.GetPath("game.json");
        JsonFileGameSaveStore store = new(path);

        store.Save(original);
        MinimumMatchRandomSource loadRandom = new();
        Game loaded = store.Load(Registry(profile), loadRandom);

        Assert.Equal(GameTestSnapshot.Capture(original), GameTestSnapshot.Capture(loaded));
        Assert.Empty(loadRandom.Requests);
        byte[] bytes = File.ReadAllBytes(path);
        Assert.False(bytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
        string json = Encoding.UTF8.GetString(bytes);
        Assert.Contains("\"formatVersion\": 2", json, StringComparison.Ordinal);
        Assert.DoesNotContain("presentation", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(directory.Path, json, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(GameActionStatus.TurnCompleted, original.PlayTurn().Status);
        Assert.Equal(GameActionStatus.TurnCompleted, loaded.PlayTurn().Status);
        Assert.Equal(GameTestSnapshot.Capture(original), GameTestSnapshot.Capture(loaded));
    }

    [Fact]
    public void BundledDemoRoundTripsItsExactProfileAndGenericState()
    {
        ValidatedGameProfile profile = new JsonGameProfileParser().Parse(File.ReadAllBytes(DemoPath));
        Game original = GameSetup.Create(
            profile,
            [new PlayerSetup(12, "Aster"), new PlayerSetup(4, "Bramble")],
            new MinimumMatchRandomSource());
        using TemporaryDirectory directory = new();
        JsonFileGameSaveStore store = new(directory.GetPath("demo.json"));

        store.Save(original);
        Game loaded = store.Load(Registry(profile), new MinimumMatchRandomSource());

        Assert.Equal(new ProfileId("profile.demo-001"), loaded.Profile.Id);
        Assert.Equal(profile.Revision, loaded.Profile.Revision);
        Assert.Equal(profile.Fingerprint, loaded.Profile.Fingerprint);
        Assert.Equal(27, loaded.Board.Track.Count);
        Assert.Single(loaded.Decks.Entries);
        Assert.Equal(GameTestSnapshot.Capture(original), GameTestSnapshot.Capture(loaded));
    }

    [Fact]
    public void PendingPurchaseRoundTripsAndResumesWithoutReplayingTheTurn()
    {
        ValidatedGameProfile profile = PurchasableProfile();
        Game original = GameSetup.Create(
            profile,
            [new PlayerSetup(40, "First"), new PlayerSetup(9, "Second")],
            new ScriptedMatchRandomSource(1));
        PurchaseDecision decision = Assert.IsType<PurchaseDecision>(original.PlayTurn().PendingDecision);
        using TemporaryDirectory directory = new();
        JsonFileGameSaveStore store = new(directory.GetPath("pending.json"));
        store.Save(original);

        ScriptedMatchRandomSource loadRandom = new();
        Game loaded = store.Load(Registry(profile), loadRandom);

        Assert.Equal(GameTestSnapshot.Capture(original), GameTestSnapshot.Capture(loaded));
        Assert.Empty(loadRandom.Requests);
        GameActionResult resumed = loaded.SubmitDecision(new DecisionResponse(
            decision.DecisionId,
            decision.PlayerId,
            DecisionOptions.Accept));
        Assert.Equal(GameActionStatus.TurnCompleted, resumed.Status);
        Assert.Equal(15, loaded.Players[0].Resources[ExecutionProfileFactory.Credits]);
        Assert.Equal(40, loaded.Ownership.BySpaceId[decision.SpaceId].OwnerPlayerId);
        Assert.Equal(GameActionRejectionReason.DuplicateDecision, loaded.SubmitDecision(new DecisionResponse(
            decision.DecisionId,
            decision.PlayerId,
            DecisionOptions.Accept)).RejectionReason);
    }

    [Fact]
    public void ConsumedDecisionHistoryPreservesDuplicateAndStaleClassification()
    {
        IReadOnlyList<CapabilityDefinition> purchase =
        [
            new OwnableCapabilityDefinition(),
            new PurchasableCapabilityDefinition(new ResourceAmount(ExecutionProfileFactory.Credits, 5))
        ];
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            spaceCount: 3,
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [1] = purchase,
                [2] = purchase
            });
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(6, "Decisions")],
            new ScriptedMatchRandomSource(1, 1));
        PurchaseDecision first = Assert.IsType<PurchaseDecision>(game.PlayTurn().PendingDecision);
        Assert.Equal(GameActionStatus.TurnCompleted, game.SubmitDecision(new DecisionResponse(
            first.DecisionId,
            first.PlayerId,
            DecisionOptions.Decline)).Status);
        PurchaseDecision second = Assert.IsType<PurchaseDecision>(game.PlayTurn().PendingDecision);
        Assert.Equal(GameActionStatus.TurnCompleted, game.SubmitDecision(new DecisionResponse(
            second.DecisionId,
            second.PlayerId,
            DecisionOptions.Decline)).Status);
        using TemporaryDirectory directory = new();
        JsonFileGameSaveStore store = new(directory.GetPath("consumed.json"));
        store.Save(game);

        Game loaded = store.Load(Registry(profile), new MinimumMatchRandomSource());

        Assert.Equal(GameActionRejectionReason.StaleDecision, loaded.SubmitDecision(new DecisionResponse(
            first.DecisionId,
            first.PlayerId,
            DecisionOptions.Decline)).RejectionReason);
        Assert.Equal(GameActionRejectionReason.DuplicateDecision, loaded.SubmitDecision(new DecisionResponse(
            second.DecisionId,
            second.PlayerId,
            DecisionOptions.Decline)).RejectionReason);
    }

    [Fact]
    public void TerminalWinnerRoundTripsAndRetainsCompletedNotificationBoundary()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(spaceCount: 2, roundLimit: 1);
        Game original = GameSetup.Create(
            profile,
            [new PlayerSetup(7, "Terminal")],
            new ScriptedMatchRandomSource(1));
        Assert.Equal(GameActionStatus.GameOver, original.PlayTurn().Status);
        using TemporaryDirectory directory = new();
        JsonFileGameSaveStore store = new(directory.GetPath("terminal.json"));
        store.Save(original);

        Game loaded = store.Load(Registry(profile), new ScriptedMatchRandomSource());

        Assert.True(loaded.IsGameOver);
        Assert.Equal(7, loaded.Winner!.Id);
        Assert.Equal(GameActionStatus.GameOver, loaded.PlayTurn().Status);
        Assert.Equal(0, loaded.NotificationSubscriberCount);
        using IDisposable ignored = loaded.Notifications.Subscribe(_ => throw new InvalidOperationException());
        Assert.Equal(0, loaded.NotificationSubscriberCount);
    }

    [Fact]
    public void MultipleDeckOrdersRoundTripExactly()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(
            decks:
            [
                new TestDeckSpec("deck.alpha", [
                    new TestCardSpec("card.alpha-1", []),
                    new TestCardSpec("card.alpha-2", [])]),
                new TestDeckSpec("deck.beta", [
                    new TestCardSpec("card.beta-1", []),
                    new TestCardSpec("card.beta-2", []),
                    new TestCardSpec("card.beta-3", [])])
            ]);
        Game original = GameSetup.Create(
            profile,
            [new PlayerSetup(1, "Decks")],
            new ScriptedMatchRandomSource(0, 1, 0));
        using TemporaryDirectory directory = new();
        JsonFileGameSaveStore store = new(directory.GetPath("decks.json"));
        store.Save(original);

        Game loaded = store.Load(Registry(profile), new ScriptedMatchRandomSource());

        Assert.Equal(
            original.Decks.Entries.Select(deck => (deck.Id, Cards: string.Join(',', deck.Cards.Select(card => card.Id)))),
            loaded.Decks.Entries.Select(deck => (deck.Id, Cards: string.Join(',', deck.Cards.Select(card => card.Id)))));
    }

    [Fact]
    public void MissingOrChangedRegisteredProfileIsTypedAndDoesNotTouchAnActiveMatch()
    {
        ValidatedGameProfile savedProfile = ExecutionProfileFactory.Create(spaceCount: 3);
        Game saved = GameSetup.Create(savedProfile, [new PlayerSetup(1, "Saved")], new MinimumMatchRandomSource());
        Game active = GameSetup.Create(savedProfile, [new PlayerSetup(99, "Active")], new MinimumMatchRandomSource());
        string before = GameTestSnapshot.Capture(active);
        using TemporaryDirectory directory = new();
        JsonFileGameSaveStore store = new(directory.GetPath("profile.json"));
        store.Save(saved);
        ValidatedGameProfile changed = ExecutionProfileFactory.Create(spaceCount: 4);

        SaveStoreException changedException = Assert.Throws<SaveStoreException>(() =>
            store.Load(Registry(changed), new MinimumMatchRandomSource()));
        ValidatedGameProfile unrelated = GameProfileValidator.Validate(ProfileTestFactory.Create());
        SaveStoreException missingException = Assert.Throws<SaveStoreException>(() =>
            store.Load(Registry(unrelated), new MinimumMatchRandomSource()));

        Assert.Equal(SaveStoreErrorKind.IncompatibleProfile, changedException.Kind);
        Assert.Equal(SaveStoreErrorKind.IncompatibleProfile, missingException.Kind);
        Assert.Equal(before, GameTestSnapshot.Capture(active));
    }

    [Theory]
    [InlineData("{\"Version\":1}")]
    [InlineData("{\"Version\":1,\"legacyDeck\":[]}")]
    [InlineData("{\"formatVersion\":99}")]
    public void RetiredOrUnsupportedFormatsReturnCompatibilityError(string json)
    {
        using TemporaryDirectory directory = new();
        string path = directory.GetPath("legacy.json");
        File.WriteAllText(path, json);

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() =>
            new JsonFileGameSaveStore(path).Load(Registry(ExecutionProfileFactory.Create())));

        Assert.Equal(SaveStoreErrorKind.IncompatibleVersion, exception.Kind);
    }

    [Theory]
    [InlineData("[")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"formatVersion\":2,\"formatVersion\":2}")]
    [InlineData("{\"formatVersion\":2,\"unknown\":true}")]
    public void MalformedDuplicateOrUnknownJsonReturnsInvalidData(string json)
    {
        using TemporaryDirectory directory = new();
        string path = directory.GetPath("invalid.json");
        File.WriteAllText(path, json);

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() =>
            new JsonFileGameSaveStore(path).Load(Registry(ExecutionProfileFactory.Create())));

        Assert.Equal(SaveStoreErrorKind.InvalidData, exception.Kind);
    }

    [Fact]
    public void InvalidEncodingAndOversizedInputReturnInvalidData()
    {
        using TemporaryDirectory directory = new();
        ValidatedGameProfile profile = ExecutionProfileFactory.Create();
        foreach ((string file, byte[] content) in new[]
                 {
                     ("utf16.json", Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("{}")).ToArray()),
                     ("oversized.json", new byte[GameSaveFormat.MaximumInputBytes + 1])
                 })
        {
            string path = directory.GetPath(file);
            File.WriteAllBytes(path, content);
            SaveStoreException exception = Assert.Throws<SaveStoreException>(() =>
                new JsonFileGameSaveStore(path).Load(Registry(profile)));
            Assert.Equal(SaveStoreErrorKind.InvalidData, exception.Kind);
        }
    }

    [Fact]
    public void Utf8BomIsAcceptedButNeverWritten()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create();
        Game game = GameSetup.Create(profile, [new PlayerSetup(1, "One")], new MinimumMatchRandomSource());
        using TemporaryDirectory directory = new();
        string path = directory.GetPath("bom.json");
        JsonFileGameSaveStore store = new(path);
        store.Save(game);
        byte[] canonical = File.ReadAllBytes(path);
        File.WriteAllBytes(path, Encoding.UTF8.GetPreamble().Concat(canonical).ToArray());

        Game loaded = store.Load(Registry(profile));
        store.Save(loaded);

        Assert.Equal(GameTestSnapshot.Capture(game), GameTestSnapshot.Capture(loaded));
        Assert.False(File.ReadAllBytes(path).AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
    }

    [Theory]
    [InlineData("duplicate-resource")]
    [InlineData("foreign-owner")]
    [InlineData("missing-deck")]
    [InlineData("negative-resource")]
    [InlineData("invalid-continuation")]
    [InlineData("consumed-pending-decision")]
    [InlineData("unsupported-status-state")]
    public void InconsistentWholeMatchStateIsRejected(string corruption)
    {
        ValidatedGameProfile profile = PurchasableProfile(withDeck: true);
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(1, "One"), new PlayerSetup(2, "Two")],
            ScriptedMatchRandomSource.ForDice(1));
        Assert.Equal(GameActionStatus.DecisionRequired, game.PlayTurn().Status);
        using TemporaryDirectory directory = new();
        string path = directory.GetPath("corrupt.json");
        JsonFileGameSaveStore store = new(path);
        store.Save(game);
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        JsonObject match = root["match"]!.AsObject();

        switch (corruption)
        {
            case "duplicate-resource":
                JsonArray resources = match["players"]![0]!["resources"]!.AsArray();
                resources.Add(resources[0]!.DeepClone());
                break;
            case "foreign-owner":
                match["moduleState"]!["ownership"]!["entries"]![0]!["ownerPlayerId"] = 404;
                break;
            case "missing-deck":
                match["decks"]!.AsArray().Clear();
                break;
            case "negative-resource":
                match["players"]![0]!["resources"]![0]!["value"] = -1;
                break;
            case "invalid-continuation":
                match["continuation"]!["nextCapabilityIndex"] = 99;
                break;
            case "consumed-pending-decision":
                match["consumedDecisionIds"]!.AsArray().Add(match["pendingDecision"]!["decisionId"]!.DeepClone());
                match["lastConsumedDecisionId"] = match["pendingDecision"]!["decisionId"]!.DeepClone();
                break;
            case "unsupported-status-state":
                match["moduleState"]!["statuses"]!["entries"]!.AsArray().Add(new JsonObject
                {
                    ["playerId"] = 1,
                    ["statusId"] = "status.unsupported",
                    ["value"] = 1
                });
                break;
        }
        File.WriteAllText(path, root.ToJsonString());

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() =>
            store.Load(Registry(profile), new MinimumMatchRandomSource()));

        Assert.Equal(SaveStoreErrorKind.InvalidData, exception.Kind);
    }

    [Fact]
    public void InvalidTerminalWinnerIsRejected()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(spaceCount: 2, roundLimit: 1);
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(7, "Seven"), new PlayerSetup(2, "Two")],
            new ScriptedMatchRandomSource(1, 1));
        Assert.Equal(GameActionStatus.TurnCompleted, game.PlayTurn().Status);
        Assert.Equal(GameActionStatus.GameOver, game.PlayTurn().Status);
        Assert.Equal(2, game.Winner!.Id);
        using TemporaryDirectory directory = new();
        string path = directory.GetPath("winner.json");
        JsonFileGameSaveStore store = new(path);
        store.Save(game);
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        root["match"]!["winnerPlayerId"] = 7;
        File.WriteAllText(path, root.ToJsonString());

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() => store.Load(Registry(profile)));

        Assert.Equal(SaveStoreErrorKind.InvalidData, exception.Kind);
    }

    [Fact]
    public void UnsupportedModuleVersionReturnsCompatibilityError()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create();
        Game game = GameSetup.Create(profile, [new PlayerSetup(1, "One")], new MinimumMatchRandomSource());
        using TemporaryDirectory directory = new();
        string path = directory.GetPath("module.json");
        JsonFileGameSaveStore store = new(path);
        store.Save(game);
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        root["match"]!["moduleState"]!["ownership"]!["version"] = 2;
        File.WriteAllText(path, root.ToJsonString());

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() => store.Load(Registry(profile)));

        Assert.Equal(SaveStoreErrorKind.IncompatibleVersion, exception.Kind);
    }

    [Fact]
    public void SaveCreatesAndAtomicallyReplacesPhysicalFile()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create();
        Game game = GameSetup.Create(profile, [new PlayerSetup(1, "One")], new MinimumMatchRandomSource());
        using TemporaryDirectory directory = new();
        string path = directory.GetPath("atomic.json");
        JsonFileGameSaveStore store = new(path);

        store.Save(game);
        byte[] first = File.ReadAllBytes(path);
        Assert.Single(Directory.GetFiles(directory.Path));
        store.Save(game);
        Assert.Equal(first, File.ReadAllBytes(path));
        game.PlayTurn();
        store.Save(game);

        Assert.NotEqual(first, File.ReadAllBytes(path));
        Assert.Single(Directory.GetFiles(directory.Path));
        Assert.Equal(GameTestSnapshot.Capture(game), GameTestSnapshot.Capture(store.Load(Registry(profile))));
    }

    [Theory]
    [InlineData(StorageFailurePoint.Create)]
    [InlineData(StorageFailurePoint.Write)]
    [InlineData(StorageFailurePoint.Flush)]
    [InlineData(StorageFailurePoint.Replace)]
    public void AtomicOverwriteFailurePreservesPreviousFileAndCleansTemporaryState(StorageFailurePoint failure)
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create();
        Game game = GameSetup.Create(profile, [new PlayerSetup(1, "One")], new MinimumMatchRandomSource());
        byte[] original = [1, 2, 3, 4];
        MemoryFileOperations files = new("game.json", original) { Failure = failure };
        JsonFileGameSaveStore store = new("game.json", files);

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() => store.Save(game));

        Assert.Equal(SaveStoreErrorKind.StorageFailure, exception.Kind);
        Assert.Equal(original, files.TargetContent);
        Assert.Empty(files.TemporaryPaths);
    }

    [Fact]
    public void NewFilePromotionFailureLeavesNoTargetOrTemporaryFile()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create();
        Game game = GameSetup.Create(profile, [new PlayerSetup(1, "One")], new MinimumMatchRandomSource());
        MemoryFileOperations files = new("game.json", null) { Failure = StorageFailurePoint.Move };
        JsonFileGameSaveStore store = new("game.json", files);

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() => store.Save(game));

        Assert.Equal(SaveStoreErrorKind.StorageFailure, exception.Kind);
        Assert.False(files.TargetExists);
        Assert.Empty(files.TemporaryPaths);
    }

    [Fact]
    public void CleanupFailureDoesNotHideTheOriginalWriteFailure()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create();
        Game game = GameSetup.Create(profile, [new PlayerSetup(1, "One")], new MinimumMatchRandomSource());
        byte[] original = [8, 9];
        MemoryFileOperations files = new("game.json", original)
        {
            Failure = StorageFailurePoint.Write,
            DeleteFails = true
        };

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() =>
            new JsonFileGameSaveStore("game.json", files).Save(game));

        Assert.Equal(SaveStoreErrorKind.StorageFailure, exception.Kind);
        Assert.Equal("write failed", exception.InnerException!.Message);
        Assert.Equal(original, files.TargetContent);
        Assert.Single(files.TemporaryPaths);
    }

    [Fact]
    public void LoadClassifiesMissingAndTechnicalFailures()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create();
        using TemporaryDirectory directory = new();
        SaveStoreException missing = Assert.Throws<SaveStoreException>(() =>
            new JsonFileGameSaveStore(directory.GetPath("missing.json")).Load(Registry(profile)));
        Assert.Equal(SaveStoreErrorKind.NotFound, missing.Kind);

        UnauthorizedAccessException cause = new("denied");
        MemoryFileOperations files = new("game.json", []) { ReadFailure = cause };
        SaveStoreException storage = Assert.Throws<SaveStoreException>(() =>
            new JsonFileGameSaveStore("game.json", files).Load(Registry(profile)));
        Assert.Equal(SaveStoreErrorKind.StorageFailure, storage.Kind);
        Assert.Same(cause, storage.InnerException);
    }

    private static ValidatedGameProfile PurchasableProfile(bool withDeck = false)
    {
        IReadOnlyList<TestDeckSpec> decks = withDeck
            ? [new TestDeckSpec("deck.save", [new TestCardSpec("card.save", [])])]
            : [];
        return ExecutionProfileFactory.Create(
            spaceCount: 2,
            spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
            {
                [1] =
                [
                    new OwnableCapabilityDefinition(),
                    new PurchasableCapabilityDefinition(new ResourceAmount(ExecutionProfileFactory.Credits, 5)),
                    new UsageFeeCapabilityDefinition(new ResourceAmount(ExecutionProfileFactory.Credits, 2))
                ]
            },
            decks: decks,
            startingCredits: 20);
    }

    private static GameProfileRegistry Registry(params ValidatedGameProfile[] profiles) => new(profiles);

    private static void AssertClosedObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("type", out JsonElement type) &&
                type.ValueKind == JsonValueKind.String && type.GetString() == "object")
            {
                Assert.True(element.TryGetProperty("additionalProperties", out JsonElement additional));
                Assert.False(additional.GetBoolean());
            }
            foreach (JsonProperty property in element.EnumerateObject()) AssertClosedObjects(property.Value);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in element.EnumerateArray()) AssertClosedObjects(child);
        }
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Monopoly.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    public enum StorageFailurePoint
    {
        Create,
        Write,
        Flush,
        Replace,
        Move
    }

    private sealed class MemoryFileOperations : IFileOperations
    {
        private readonly string _targetName;
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);

        internal MemoryFileOperations(string targetName, byte[]? targetContent)
        {
            _targetName = Path.GetFullPath(targetName);
            if (targetContent is not null) _files[_targetName] = targetContent.ToArray();
        }

        internal StorageFailurePoint? Failure { get; init; }
        internal Exception? ReadFailure { get; init; }
        internal bool DeleteFails { get; init; }
        internal bool TargetExists => _files.ContainsKey(_targetName);
        internal byte[] TargetContent => _files[_targetName];
        internal IReadOnlyList<string> TemporaryPaths => _files.Keys.Where(path => path != _targetName).ToArray();

        public bool Exists(string path) => _files.ContainsKey(path);

        public byte[] ReadBytes(string path, int maximumBytes)
        {
            if (ReadFailure is not null) throw ReadFailure;
            if (!_files.TryGetValue(path, out byte[]? content)) throw new FileNotFoundException();
            if (content.Length > maximumBytes) throw new FileContentLimitExceededException(maximumBytes);
            return content.ToArray();
        }

        public IFileWriteSession CreateNewWriteSession(string path)
        {
            if (Failure == StorageFailurePoint.Create) throw new IOException("create failed");
            if (_files.ContainsKey(path)) throw new IOException("exists");
            _files.Add(path, []);
            return new MemoryWriteSession(this, path);
        }

        public void Replace(string sourcePath, string destinationPath)
        {
            if (Failure == StorageFailurePoint.Replace) throw new IOException("replace failed");
            _files[destinationPath] = _files[sourcePath];
            _files.Remove(sourcePath);
        }

        public void Move(string sourcePath, string destinationPath)
        {
            if (Failure == StorageFailurePoint.Move) throw new IOException("move failed");
            _files.Add(destinationPath, _files[sourcePath]);
            _files.Remove(sourcePath);
        }

        public void Delete(string path)
        {
            if (DeleteFails) throw new IOException("delete failed");
            _files.Remove(path);
        }

        private sealed class MemoryWriteSession(MemoryFileOperations owner, string path) : IFileWriteSession
        {
            public void Write(ReadOnlyMemory<byte> content)
            {
                if (owner.Failure == StorageFailurePoint.Write) throw new IOException("write failed");
                owner._files[path] = content.ToArray();
            }

            public void FlushToDisk()
            {
                if (owner.Failure == StorageFailurePoint.Flush) throw new IOException("flush failed");
            }

            public void Dispose()
            {
            }
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "save-v2-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        internal string Path { get; }
        internal string GetPath(string fileName) => System.IO.Path.Combine(Path, fileName);

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
    }
}
