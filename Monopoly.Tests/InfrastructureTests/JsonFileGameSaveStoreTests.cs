using System.Text.Json;
using Infrastructure.Persistence;
using Monopoly.Core;
using Monopoly.Core.Persistence;
using Monopoly.Tests.CoreTests;

namespace Monopoly.Tests.InfrastructureTests;

public sealed class JsonFileGameSaveStoreTests
{
    [Fact]
    public void LoadReadsExistingVersionOneFixture()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string savePath = temporaryDirectory.GetPath("fixture.json");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "TestData", "game-state-v1.json"), savePath);
        JsonFileGameSaveStore store = new(savePath);

        Game loaded = store.Load();

        Assert.Equal(2, loaded.Players.Count);
        Assert.Equal("Bob", loaded.CurrentPlayer.Name);
        Assert.Equal(4, loaded.CurrentTurn);
        Assert.Equal(1, loaded.ConsecutiveDoubles);
        Assert.Equal(25, loaded.Fines);
        Assert.True(loaded.TheJail.IsPlayerInJail(loaded.CurrentPlayer));

        string roundTripPath = temporaryDirectory.GetPath("roundtrip.json");
        new JsonFileGameSaveStore(roundTripPath).Save(loaded);
        using JsonDocument fixtureDocument = JsonDocument.Parse(File.ReadAllBytes(savePath));
        using JsonDocument roundTripDocument = JsonDocument.Parse(File.ReadAllBytes(roundTripPath));
        Assert.Equal(
            JsonSerializer.Serialize(fixtureDocument.RootElement),
            JsonSerializer.Serialize(roundTripDocument.RootElement));
    }

    [Fact]
    public void LoadKeepsVersionOnePropertyMatchingCaseInsensitive()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string savePath = temporaryDirectory.GetPath("case-insensitive.json");
        string fixture = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "game-state-v1.json"));
        File.WriteAllText(
            savePath,
            fixture
                .Replace("\"Version\"", "\"version\"", StringComparison.Ordinal)
                .Replace("\"Players\"", "\"players\"", StringComparison.Ordinal)
                .Replace("\"CurrentPlayerId\"", "\"currentPlayerId\"", StringComparison.Ordinal));
        JsonFileGameSaveStore store = new(savePath);

        Game loaded = store.Load();

        Assert.Equal("Bob", loaded.CurrentPlayer.Name);
    }

    [Fact]
    public void SavePreservesVersionOneJsonShapeAndEncoding()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string savePath = temporaryDirectory.GetPath("shape.json");
        JsonFileGameSaveStore store = new(savePath);

        store.Save(CreateGame());

        byte[] bytes = File.ReadAllBytes(savePath);
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        using JsonDocument document = JsonDocument.Parse(bytes);
        Assert.Equal(
            new[]
            {
                "Version", "Rules", "Players", "CurrentPlayerId", "CurrentTurn",
                "ConsecutiveDoubles", "Fines", "Squares", "Jail", "ChanceDeck",
                "CommunityChestDeck"
            },
            document.RootElement.EnumerateObject().Select(property => property.Name));
        Assert.Equal(1, document.RootElement.GetProperty("Version").GetInt32());
        Assert.Equal(JsonValueKind.Number, document.RootElement.GetProperty("Rules").GetProperty("GameLanguage").ValueKind);
        Assert.Equal(JsonValueKind.Number, document.RootElement.GetProperty("Rules").GetProperty("FreeParking").ValueKind);
        Assert.Contains(Environment.NewLine + "  \"Version\"", File.ReadAllText(savePath));
    }

    [Fact]
    public void SaveCreatesThenAtomicallyReplacesFile()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string savePath = temporaryDirectory.GetPath("game.json");
        JsonFileGameSaveStore store = new(savePath);
        Game game = CreateGame();

        store.Save(game);
        game = new GameTestBuilder().WithTurn(1, fines: 75).Build();
        store.Save(game);

        Assert.Equal(75, store.Load().Fines);
        Assert.Equal(new[] { savePath }, Directory.GetFiles(temporaryDirectory.Path));
    }

    [Fact]
    public void LoadClassifiesMissingFile()
    {
        using TemporaryDirectory temporaryDirectory = new();
        JsonFileGameSaveStore store = new(temporaryDirectory.GetPath("missing.json"));

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() => store.Load());

        Assert.Equal(SaveStoreErrorKind.NotFound, exception.Kind);
        Assert.IsAssignableFrom<FileNotFoundException>(exception.InnerException);
    }

    [Theory]
    [InlineData("[")]
    [InlineData("{\"Version\":1}")]
    public void LoadClassifiesMalformedOrInvalidVersionOneData(string json)
    {
        using TemporaryDirectory temporaryDirectory = new();
        string savePath = temporaryDirectory.GetPath("invalid.json");
        File.WriteAllText(savePath, json);
        JsonFileGameSaveStore store = new(savePath);

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() => store.Load());

        Assert.Equal(SaveStoreErrorKind.InvalidData, exception.Kind);
        Assert.NotNull(exception.InnerException);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"Version\":99}")]
    public void LoadClassifiesMissingOrUnsupportedVersion(string json)
    {
        using TemporaryDirectory temporaryDirectory = new();
        string savePath = temporaryDirectory.GetPath("incompatible.json");
        File.WriteAllText(savePath, json);
        JsonFileGameSaveStore store = new(savePath);

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() => store.Load());

        Assert.Equal(SaveStoreErrorKind.IncompatibleVersion, exception.Kind);
        Assert.Contains("Expected version 1", exception.Message);
    }

    [Fact]
    public void LoadClassifiesStorageFailureAndRetainsCause()
    {
        UnauthorizedAccessException cause = new("denied");
        JsonFileGameSaveStore store = new("game.json", new ReadFailingFileOperations(cause));

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() => store.Load());

        Assert.Equal(SaveStoreErrorKind.StorageFailure, exception.Kind);
        Assert.Same(cause, exception.InnerException);
    }

    [Fact]
    public void LoadClassifiesInvalidRuleEnumAsInvalidData()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string savePath = temporaryDirectory.GetPath("invalid-enum.json");
        string fixture = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "game-state-v1.json"));
        File.WriteAllText(savePath, fixture.Replace("\"GameLanguage\": 0", "\"GameLanguage\": 99", StringComparison.Ordinal));
        JsonFileGameSaveStore store = new(savePath);

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() => store.Load());

        Assert.Equal(SaveStoreErrorKind.InvalidData, exception.Kind);
        Assert.IsType<GameStateValidationException>(exception.InnerException);
    }

    [Theory]
    [InlineData(FailureStage.Write)]
    [InlineData(FailureStage.Flush)]
    [InlineData(FailureStage.Replace)]
    public void FailedReplacementPreservesExistingFileAndRemovesTemporaryFile(FailureStage failureStage)
    {
        using TemporaryDirectory temporaryDirectory = new();
        string savePath = temporaryDirectory.GetPath("game.json");
        JsonFileGameSaveStore workingStore = new(savePath);
        Game game = CreateGame();
        workingStore.Save(game);
        byte[] originalBytes = File.ReadAllBytes(savePath);
        game = new GameTestBuilder().WithTurn(1, fines: 99).Build();
        JsonFileGameSaveStore failingStore = new(savePath, new FailingFileOperations(failureStage));

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() => failingStore.Save(game));

        Assert.Equal(SaveStoreErrorKind.StorageFailure, exception.Kind);
        Assert.Equal(originalBytes, File.ReadAllBytes(savePath));
        Assert.Equal(new[] { savePath }, Directory.GetFiles(temporaryDirectory.Path));
    }

    [Fact]
    public void FailedInitialPromotionLeavesNoDestinationOrTemporaryFile()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string savePath = temporaryDirectory.GetPath("game.json");
        JsonFileGameSaveStore store = new(savePath, new FailingFileOperations(FailureStage.Move));

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() => store.Save(CreateGame()));

        Assert.Equal(SaveStoreErrorKind.StorageFailure, exception.Kind);
        Assert.Empty(Directory.GetFiles(temporaryDirectory.Path));
    }

    private static Game CreateGame() => CoreGameSetup.Setup(new GameRules(2, 2, 6));

    public enum FailureStage
    {
        Write,
        Flush,
        Replace,
        Move
    }

    private sealed class FailingFileOperations : IFileOperations
    {
        private readonly PhysicalFileOperations _inner = new();
        private readonly FailureStage _failureStage;

        internal FailingFileOperations(FailureStage failureStage)
        {
            _failureStage = failureStage;
        }

        public bool Exists(string path) => _inner.Exists(path);

        public string ReadAllText(string path) => _inner.ReadAllText(path);

        public IFileWriteSession CreateNewWriteSession(string path) =>
            new FailingFileWriteSession(_inner.CreateNewWriteSession(path), _failureStage);

        public void Replace(string sourcePath, string destinationPath)
        {
            if (_failureStage == FailureStage.Replace)
                throw new IOException("Injected replacement failure.");
            _inner.Replace(sourcePath, destinationPath);
        }

        public void Move(string sourcePath, string destinationPath)
        {
            if (_failureStage == FailureStage.Move)
                throw new IOException("Injected move failure.");
            _inner.Move(sourcePath, destinationPath);
        }

        public void Delete(string path) => _inner.Delete(path);
    }

    private sealed class FailingFileWriteSession : IFileWriteSession
    {
        private readonly IFileWriteSession _inner;
        private readonly FailureStage _failureStage;

        internal FailingFileWriteSession(IFileWriteSession inner, FailureStage failureStage)
        {
            _inner = inner;
            _failureStage = failureStage;
        }

        public void Write(string content)
        {
            if (_failureStage == FailureStage.Write)
                throw new IOException("Injected write failure.");
            _inner.Write(content);
        }

        public void FlushToDisk()
        {
            if (_failureStage == FailureStage.Flush)
                throw new IOException("Injected flush failure.");
            _inner.FlushToDisk();
        }

        public void Dispose() => _inner.Dispose();
    }

    private sealed class ReadFailingFileOperations : IFileOperations
    {
        private readonly Exception _exception;

        internal ReadFailingFileOperations(Exception exception)
        {
            _exception = exception;
        }

        public bool Exists(string path) => throw new NotSupportedException();

        public string ReadAllText(string path) => throw _exception;

        public IFileWriteSession CreateNewWriteSession(string path) => throw new NotSupportedException();

        public void Replace(string sourcePath, string destinationPath) => throw new NotSupportedException();

        public void Move(string sourcePath, string destinationPath) => throw new NotSupportedException();

        public void Delete(string path) => throw new NotSupportedException();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "monopoly-persistence-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        internal string Path { get; }

        internal string GetPath(string fileName) => System.IO.Path.Combine(Path, fileName);

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
