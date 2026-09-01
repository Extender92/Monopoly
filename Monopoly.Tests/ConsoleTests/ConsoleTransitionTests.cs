using System.Text;
using System.Text.Json.Nodes;
using Infrastructure.Profiles;
using Monopoly.Console;
using Monopoly.Core;

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
        TestConsole console = new();
        ValidatedGameProfile? selected = null;

        int exitCode = Monopoly.Console.Program.Run([], console, (profile, _) => selected = profile);

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
            TestConsole console = new();
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
        TestConsole console = new();
        bool applicationStarted = false;

        int exitCode = Monopoly.Console.Program.Run(
            ["--profile", missingPath],
            console,
            (_, _) => applicationStarted = true);

        Assert.Equal(1, exitCode);
        Assert.False(applicationStarted);
        Assert.Equal("The profile file was not found.", Assert.Single(console.Lines));
        Assert.DoesNotContain(missingPath, console.Output, StringComparison.OrdinalIgnoreCase);
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
            TestConsole console = new();
            bool applicationStarted = false;

            int exitCode = Monopoly.Console.Program.Run(
                ["--profile", path],
                console,
                (_, _) => applicationStarted = true);

            Assert.Equal(1, exitCode);
            Assert.False(applicationStarted);
            Assert.Equal(expectedMessage, Assert.Single(console.Lines));
            Assert.DoesNotContain(directory.Path, console.Output, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void HelpAndInvalidArgumentsDoNotStartTheApplication()
    {
        TestConsole help = new();
        bool started = false;
        Assert.Equal(0, Monopoly.Console.Program.Run(["--help"], help, (_, _) => started = true));
        Assert.False(started);
        Assert.Contains("Usage: Monopoly.Console", Assert.Single(help.Lines));

        TestConsole invalid = new();
        Assert.Equal(2, Monopoly.Console.Program.Run(["--profile"], invalid, (_, _) => started = true));
        Assert.False(started);
        Assert.Equal(2, invalid.Lines.Count);
    }

    [Fact]
    public void ExplicitSyntheticProfileCanEnterAndLeaveAPlayableSession()
    {
        string profilePath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "Profiles",
            "synthetic-zero-decks-v1.json");
        TestConsole console = new("1", "1", "Aster", "5", "3");

        int exitCode = Monopoly.Console.Program.Run(["--profile", profilePath], console);

        Assert.Equal(0, exitCode);
        Assert.Contains("Selected profile: Quiet Loop", console.Output, StringComparison.Ordinal);
        Assert.Contains("New match created.", console.Output, StringComparison.Ordinal);
        Assert.Contains("This profile has no decks", RenderDeckView(profilePath), StringComparison.Ordinal);
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

    private static string RenderDeckView(string profilePath)
    {
        ValidatedGameProfile profile = new JsonFileGameProfileSource(profilePath).Load();
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(0, "Aster")],
            new Monopoly.Tests.TestDoubles.MinimumMatchRandomSource());
        ConsoleMatchProjection projection = new ConsoleProjectionBuilder().Build(game);
        TestConsole console = new(string.Empty);
        new ConsoleRenderer(console).RenderDecks(projection);
        return console.Output;
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
