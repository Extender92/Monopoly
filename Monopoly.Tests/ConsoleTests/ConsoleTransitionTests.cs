using System.Text;
using System.Text.Json.Nodes;
using Infrastructure.Persistence;
using Infrastructure.Profiles;
using Moq;
using Monopoly.Console.GUI;
using Monopoly.Core;
using Monopoly.Core.Persistence;

namespace Monopoly.Tests.ConsoleTests;

public sealed class ConsoleTransitionTests
{
    private static readonly string DemoPath = Path.Combine(
        AppContext.BaseDirectory,
        "TestData",
        "Profiles",
        "Demo",
        "lantern-vale-v1.json");

    [Fact]
    public void DefaultStartSelectsBundledDemoWithoutAnExternalProfile()
    {
        RecordingConsole console = new();
        ValidatedGameProfile? selected = null;

        int exitCode = Monopoly.Console.Program.Run(
            [],
            console,
            (profile, _) => selected = profile);

        Assert.Equal(0, exitCode);
        Assert.NotNull(selected);
        Assert.Equal(new ProfileId("profile.demo-001"), selected.Id);
        Assert.Equal(27, selected.RuleGraph.Track.Count);
        Assert.Empty(console.Lines);
    }

    [Fact]
    public void ExplicitRelativeAndAbsolutePathsSelectTheSameValidatedProfile()
    {
        using TemporaryDirectory directory = new();
        string profilePath = Path.Combine(directory.Path, "profile with spaces.json");
        File.Copy(DemoPath, profilePath);
        string relativePath = Path.GetRelativePath(Environment.CurrentDirectory, profilePath);
        ProfileFingerprint expected = new JsonGameProfileParser().Parse(File.ReadAllBytes(DemoPath)).Fingerprint;

        foreach (string candidate in new[] { relativePath, profilePath })
        {
            RecordingConsole console = new();
            ValidatedGameProfile? selected = null;

            int exitCode = Monopoly.Console.Program.Run(
                ["--profile", candidate],
                console,
                (profile, _) => selected = profile);

            Assert.Equal(0, exitCode);
            Assert.NotNull(selected);
            Assert.Equal(expected, selected.Fingerprint);
            Assert.Empty(console.Lines);
        }
    }

    [Fact]
    public void ExplicitFailureDoesNotFallBackOrStartTheApplication()
    {
        using TemporaryDirectory directory = new();
        string missingPath = Path.Combine(directory.Path, "private reference.json");
        RecordingConsole console = new();
        bool applicationStarted = false;

        int exitCode = Monopoly.Console.Program.Run(
            ["--profile", missingPath],
            console,
            (_, _) => applicationStarted = true);

        Assert.Equal(1, exitCode);
        Assert.False(applicationStarted);
        Assert.Equal("The profile file was not found.", Assert.Single(console.Lines));
        Assert.DoesNotContain(missingPath, console.Lines[0], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Lantern Vale", console.Lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileFailureCategoriesHaveDistinctSafeMessages()
    {
        using TemporaryDirectory directory = new();
        string demo = File.ReadAllText(DemoPath);
        List<(string Name, byte[] Content, string Message)> cases =
        [
            ("oversized.json", new byte[GameProfileSchema.MaximumInputBytes + 1], "The profile file exceeds the supported size limit."),
            ("malformed.json", Encoding.UTF8.GetBytes("{"), "The profile file contains malformed JSON."),
            ("utf16.json", Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(demo)).ToArray(), "The profile file must use valid UTF-8."),
            ("version.json", Encoding.UTF8.GetBytes(demo.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2", StringComparison.Ordinal)), "The profile uses an unsupported schema version."),
            ("invalid.json", Encoding.UTF8.GetBytes(demo.Replace("\"profileId\": \"profile.demo-001\"", "\"profileId\": \"INVALID\"", StringComparison.Ordinal)), "The profile content is invalid."),
            ("unsupported.json", Encoding.UTF8.GetBytes(AddUnsupportedStatus(demo)), "The profile uses components that this engine version does not support.")
        ];

        foreach ((string name, byte[] content, string expectedMessage) in cases)
        {
            string path = Path.Combine(directory.Path, name);
            File.WriteAllBytes(path, content);
            RecordingConsole console = new();
            bool applicationStarted = false;

            int exitCode = Monopoly.Console.Program.Run(
                ["--profile", path],
                console,
                (_, _) => applicationStarted = true);

            Assert.Equal(1, exitCode);
            Assert.False(applicationStarted);
            Assert.Equal(expectedMessage, Assert.Single(console.Lines));
            Assert.DoesNotContain(directory.Path, console.Lines[0], StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void HelpReturnsWithoutLoadingOrStartingTheApplication()
    {
        RecordingConsole console = new();
        bool applicationStarted = false;

        int exitCode = Monopoly.Console.Program.Run(
            ["--help"],
            console,
            (_, _) => applicationStarted = true);

        Assert.Equal(0, exitCode);
        Assert.False(applicationStarted);
        Assert.Equal("Usage: Monopoly.Console [--profile <path>] [--help]", Assert.Single(console.Lines));
    }

    [Theory]
    [MemberData(nameof(InvalidArguments))]
    public void InvalidArgumentsReturnUsageError(string[] arguments)
    {
        RecordingConsole console = new();
        bool applicationStarted = false;

        int exitCode = Monopoly.Console.Program.Run(
            arguments,
            console,
            (_, _) => applicationStarted = true);

        Assert.Equal(2, exitCode);
        Assert.False(applicationStarted);
        Assert.Equal(2, console.Lines.Count);
        Assert.Equal("Usage: Monopoly.Console [--profile <path>] [--help]", console.Lines[1]);
    }

    public static TheoryData<string[]> InvalidArguments => new()
    {
        new[] { "--unknown" },
        new[] { "--profile" },
        new[] { "--profile", " " },
        new[] { "--profile", "first.json", "--profile", "second.json" },
        new[] { "--profile", "first.json", "extra" },
        new[] { "--help", "--profile", "first.json" }
    };

    [Fact]
    public void NewGameReportsSelectedProfileProjectionGap()
    {
        Mock<IGameSaveStore> store = new();
        RecordingConsole console = new();
        ValidatedGameProfile profile = Monopoly.Console.Program.LoadBundledDemoProfile();

        Monopoly.Console.Program.StartNewGame(store.Object, profile, console);

        Assert.Contains("selected profile is valid and supported", Assert.Single(console.Lines), StringComparison.Ordinal);
        Assert.Contains("generic Console projections", console.Lines[0], StringComparison.Ordinal);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public void LoadKeepsTypedCompatibilityMessage()
    {
        Mock<IGameSaveStore> store = new();
        store.Setup(candidate => candidate.Load(
                It.IsAny<GameProfileRegistry>(),
                It.IsAny<Monopoly.Core.Randomness.IMatchRandomSource>()))
            .Throws(new SaveStoreException(SaveStoreErrorKind.IncompatibleVersion, "gap"));
        RecordingConsole console = new();
        ValidatedGameProfile profile = Monopoly.Console.Program.LoadBundledDemoProfile();

        Monopoly.Console.Program.LoadGame(store.Object, profile, console);

        Assert.Contains("unsupported version", Assert.Single(console.Lines));
    }

    [Fact]
    public void LoadRegistersOnlyTheSelectedProfileAndReportsTheProjectionGap()
    {
        Mock<IGameSaveStore> store = new();
        RecordingConsole console = new();
        ValidatedGameProfile profile = Monopoly.Console.Program.LoadBundledDemoProfile();
        Game loaded = GameSetup.Create(
            profile,
            [new PlayerSetup(1, "Aster"), new PlayerSetup(2, "Bramble")],
            new Monopoly.Tests.TestDoubles.MinimumMatchRandomSource());
        store.Setup(candidate => candidate.Load(
                It.Is<GameProfileRegistry>(registry =>
                    registry.Profiles.Count == 1 &&
                    registry.Profiles[0].Id == profile.Id &&
                    registry.Profiles[0].Revision == profile.Revision &&
                    registry.Profiles[0].Fingerprint == profile.Fingerprint),
                It.IsAny<Monopoly.Core.Randomness.SystemMatchRandomSource>()))
            .Returns(loaded);

        Monopoly.Console.Program.LoadGame(store.Object, profile, console);

        Assert.Contains("saved match is valid for the selected profile", Assert.Single(console.Lines), StringComparison.OrdinalIgnoreCase);
        store.VerifyAll();
    }

    [Fact]
    public void LoadReportsAnExactProfileMismatchSeparately()
    {
        Mock<IGameSaveStore> store = new();
        RecordingConsole console = new();
        ValidatedGameProfile profile = Monopoly.Console.Program.LoadBundledDemoProfile();
        store.Setup(candidate => candidate.Load(
                It.IsAny<GameProfileRegistry>(),
                It.IsAny<Monopoly.Core.Randomness.IMatchRandomSource>()))
            .Throws(new SaveStoreException(SaveStoreErrorKind.IncompatibleProfile, "mismatch"));

        Monopoly.Console.Program.LoadGame(store.Object, profile, console);

        Assert.Contains("different or changed profile", Assert.Single(console.Lines), StringComparison.OrdinalIgnoreCase);
    }

    private static string AddUnsupportedStatus(string json)
    {
        JsonObject document = JsonNode.Parse(json)!.AsObject();
        document["presentation"]!.AsArray().Add(new JsonObject
        {
            ["token"] = "status.local",
            ["displayText"] = "Local status"
        });
        document["statuses"] = new JsonArray(new JsonObject
        {
            ["id"] = "status.local",
            ["presentationToken"] = "status.local",
            ["maximumValue"] = 1
        });
        return document.ToJsonString();
    }

    private sealed class RecordingConsole : IConsoleWrapper
    {
        internal List<string> Lines { get; } = [];
        public void Clear() { }
        public string ReadKey() => string.Empty;
        public string ReadLine() => string.Empty;
        public ConsoleKeyInfo GetPressedKey() => default;
        public void WriteLine(string s) => Lines.Add(s);
        public void Write(string s) { }
        public void SetTextColor(ConsoleColor color) { }
        public void ResetColor() { }
        public void SetPosition(int x, int y) { }
        public void ShowCursor(bool b) { }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "monopoly-profile-selection-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        internal string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
    }
}
