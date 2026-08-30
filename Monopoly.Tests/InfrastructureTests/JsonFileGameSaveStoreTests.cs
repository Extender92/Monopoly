using Infrastructure.Persistence;
using Monopoly.Core;
using Monopoly.Core.Persistence;
using Monopoly.Tests.CoreTests;

namespace Monopoly.Tests.InfrastructureTests;

public sealed class JsonFileGameSaveStoreTests
{
    [Fact]
    public void SaveRejectsTheTransitionGapWithoutCreatingFile()
    {
        using TemporaryDirectory directory = new();
        string path = directory.GetPath("game.json");

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() =>
            new JsonFileGameSaveStore(path).Save(new GameTestBuilder().Build()));

        Assert.Equal(SaveStoreErrorKind.IncompatibleVersion, exception.Kind);
        Assert.Contains("Version 2", exception.Message, StringComparison.Ordinal);
        Assert.False(File.Exists(path));
        Assert.Empty(Directory.GetFiles(directory.Path));
    }

    [Fact]
    public void SaveRejectsTheTransitionGapWithoutChangingAnExistingFile()
    {
        using TemporaryDirectory directory = new();
        string path = directory.GetPath("game.json");
        byte[] original = [1, 2, 3, 4];
        File.WriteAllBytes(path, original);

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() =>
            new JsonFileGameSaveStore(path).Save(new GameTestBuilder().Build()));

        Assert.Equal(SaveStoreErrorKind.IncompatibleVersion, exception.Kind);
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Equal([path], Directory.GetFiles(directory.Path));
    }

    [Theory]
    [InlineData("{\"Version\":1}", "Version 1")]
    [InlineData("{\"version\":1}", "Version 1")]
    [InlineData("{\"Version\":99}", "99")]
    [InlineData("{}", "no supported format version")]
    public void LoadRejectsRetiredOrUnavailableVersions(string json, string expectedMessage)
    {
        using TemporaryDirectory directory = new();
        string path = directory.GetPath("game.json");
        File.WriteAllText(path, json);

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() =>
            new JsonFileGameSaveStore(path).Load());

        Assert.Equal(SaveStoreErrorKind.IncompatibleVersion, exception.Kind);
        Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("[")]
    [InlineData("[]")]
    [InlineData("{\"Version\":\"1\"}")]
    public void LoadClassifiesMalformedShapeOrVersionTypeAsInvalidData(string json)
    {
        using TemporaryDirectory directory = new();
        string path = directory.GetPath("invalid.json");
        File.WriteAllText(path, json);

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() =>
            new JsonFileGameSaveStore(path).Load());

        Assert.Equal(SaveStoreErrorKind.InvalidData, exception.Kind);
    }

    [Fact]
    public void LoadClassifiesMissingFile()
    {
        using TemporaryDirectory directory = new();

        SaveStoreException exception = Assert.Throws<SaveStoreException>(() =>
            new JsonFileGameSaveStore(directory.GetPath("missing.json")).Load());

        Assert.Equal(SaveStoreErrorKind.NotFound, exception.Kind);
        Assert.IsAssignableFrom<FileNotFoundException>(exception.InnerException);
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

    private sealed class ReadFailingFileOperations(Exception exception) : IFileOperations
    {
        public bool Exists(string path) => throw new NotSupportedException();
        public string ReadAllText(string path) => throw exception;
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
                "neutral-save-gap-tests",
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
