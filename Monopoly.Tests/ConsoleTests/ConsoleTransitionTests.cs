using Infrastructure.Persistence;
using Moq;
using Monopoly.Console.GUI;
using Monopoly.Core.Persistence;

namespace Monopoly.Tests.ConsoleTests;

public sealed class ConsoleTransitionTests
{
    [Fact]
    public void NewGameValidatesBundledDemoAndReportsProjectionGap()
    {
        Mock<IGameSaveStore> store = new();
        RecordingConsole console = new();

        Monopoly.Console.Program.StartNewGame(store.Object, console);

        Assert.Contains("Demo capability execution is available in Core", Assert.Single(console.Lines));
        Assert.Contains("generic Console projections", console.Lines[0]);
    }

    [Fact]
    public void LoadKeepsTypedCompatibilityMessage()
    {
        Mock<IGameSaveStore> store = new();
        store.Setup(candidate => candidate.Load(It.IsAny<Monopoly.Core.Randomness.IMatchRandomSource>()))
            .Throws(new SaveStoreException(SaveStoreErrorKind.IncompatibleVersion, "gap"));
        RecordingConsole console = new();

        Monopoly.Console.Program.LoadGame(store.Object, console);

        Assert.Contains("unsupported version", Assert.Single(console.Lines));
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
}


