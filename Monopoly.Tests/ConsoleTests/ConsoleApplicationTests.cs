using Moq;
using Monopoly.Console;
using Monopoly.Core;
using Monopoly.Core.Persistence;
using Monopoly.Tests.TestDoubles;

namespace Monopoly.Tests.ConsoleTests;

public sealed class ConsoleApplicationTests
{
    [Fact]
    public void NewMatchCollectsPlayersAndUsesTheGenericSession()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create();
        TestConsole console = new("1", "2", "Aster", "Bramble", "5", "3");
        Mock<IGameSaveStore> store = new(MockBehavior.Strict);
        ConsoleApplication application = new(
            profile,
            console,
            store.Object,
            static () => new MinimumMatchRandomSource());

        application.Run();

        Assert.Contains("Selected profile: Execution Profile", console.Output, StringComparison.Ordinal);
        Assert.Contains("New match created.", console.Output, StringComparison.Ordinal);
        Assert.Contains("[0] Aster", console.Output, StringComparison.Ordinal);
        Assert.Contains("[1] Bramble", console.Output, StringComparison.Ordinal);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public void DuplicatePlayerNamesAreAllowedAndBlankInputCancelsSetup()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create();
        ConsoleInputReader duplicateReader = new(new TestConsole("2", "Aster", "Aster"));

        IReadOnlyList<PlayerSetup>? players = duplicateReader.ReadPlayers(profile.Setup);

        Assert.NotNull(players);
        Assert.Equal([0, 1], players.Select(player => player.Id));
        Assert.All(players, player => Assert.Equal("Aster", player.Name));

        TestConsole cancelledConsole = new("invalid", "");
        Assert.Null(new ConsoleInputReader(cancelledConsole).ReadPlayers(profile.Setup));
        Assert.Contains("whole number", cancelledConsole.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ControlCharactersAreRejectedBeforePlayerSetupCompletes()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create();
        TestConsole console = new("1", "Aster\nInjected", "Aster");

        IReadOnlyList<PlayerSetup>? players = new ConsoleInputReader(console).ReadPlayers(profile.Setup);

        Assert.NotNull(players);
        Assert.Equal("Aster", Assert.Single(players).Name);
        Assert.Contains("control characters", console.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MenuChoiceRetriesUntilInputIsInRange()
    {
        TestConsole console = new("0", "not-a-number", "2");

        int? selected = new ConsoleInputReader(console).ReadChoice(["First", "Second"]);

        Assert.Equal(1, selected);
        Assert.Equal(2, console.Lines.Count(line => line.Contains("number from 1 to 2", StringComparison.Ordinal)));
    }

    [Fact]
    public void LoadedMatchUsesTheSameSessionRunnerAndExactSelectedProfile()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create();
        Game loaded = GameSetup.Create(
            profile,
            [new PlayerSetup(8, "Aster")],
            new MinimumMatchRandomSource());
        TestConsole console = new("2", "5", "3");
        Mock<IGameSaveStore> store = new();
        store.Setup(candidate => candidate.Load(
                It.Is<GameProfileRegistry>(registry =>
                    registry.Profiles.Count == 1 &&
                    registry.Profiles[0].Fingerprint == profile.Fingerprint),
                It.IsAny<MinimumMatchRandomSource>()))
            .Returns(loaded);
        ConsoleApplication application = new(
            profile,
            console,
            store.Object,
            static () => new MinimumMatchRandomSource());

        application.Run();

        Assert.Contains("Saved match loaded.", console.Output, StringComparison.Ordinal);
        Assert.Contains("[8] Aster", console.Output, StringComparison.Ordinal);
        store.VerifyAll();
    }

    [Theory]
    [InlineData(SaveStoreErrorKind.NotFound, "No save file")]
    [InlineData(SaveStoreErrorKind.InvalidData, "invalid data")]
    [InlineData(SaveStoreErrorKind.IncompatibleVersion, "unsupported version")]
    [InlineData(SaveStoreErrorKind.IncompatibleProfile, "different or changed profile")]
    [InlineData(SaveStoreErrorKind.StorageFailure, "could not be accessed")]
    public void LoadErrorsAreSafeAndRemainInTheMainMenu(
        SaveStoreErrorKind error,
        string expected)
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create();
        TestConsole console = new("2", "3");
        Mock<IGameSaveStore> store = new();
        store.Setup(candidate => candidate.Load(
                It.IsAny<GameProfileRegistry>(),
                It.IsAny<Monopoly.Core.Randomness.IMatchRandomSource>()))
            .Throws(new SaveStoreException(error, "C:\\private\\secret.json"));
        ConsoleApplication application = new(
            profile,
            console,
            store.Object,
            static () => new MinimumMatchRandomSource());

        application.Run();

        Assert.Contains(expected, console.Output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret.json", console.Output, StringComparison.OrdinalIgnoreCase);
    }
}
