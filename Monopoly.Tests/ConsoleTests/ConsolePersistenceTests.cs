using Monopoly.Console;
using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core;
using Monopoly.Core.Interface;
using Monopoly.Core.Persistence;
using Moq;

namespace Monopoly.Tests.ConsoleTests;

public sealed class ConsolePersistenceTests
{
    [Fact]
    public void ApplicationCompositionExplicitlySelectsTheBundledDemoProfile()
    {
        ValidatedGameProfile profile = Program.LoadBundledDemoProfile();

        Assert.Equal(new ProfileId("profile.demo-001"), profile.Id);
        Assert.Equal(new ProfileRevision(1), profile.Revision);
        Assert.Equal(27, profile.RuleGraph.Track.Count);
    }

    [Fact]
    public void StartNewGameShowsTheValidatedDemoTransitionMessageWithoutUsingStorage()
    {
        Mock<IGameSaveStore> saveStore = new();
        Mock<IConsoleWrapper> console = new();
        console.Setup(wrapper => wrapper.ReadLine()).Returns(string.Empty);

        Program.StartNewGame(saveStore.Object, console.Object);

        console.Verify(wrapper => wrapper.WriteLine(It.Is<string>(message => message.StartsWith(
            "Match play is temporarily unavailable while validated Demo capability execution is being completed.",
            StringComparison.Ordinal))), Times.Once);
        console.Verify(wrapper => wrapper.ReadLine(), Times.Once);
        saveStore.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(SaveStoreErrorKind.NotFound, "No save file was found.")]
    [InlineData(SaveStoreErrorKind.InvalidData, "The save file contains invalid data.")]
    [InlineData(SaveStoreErrorKind.IncompatibleVersion, "The save file uses an unsupported version.")]
    [InlineData(SaveStoreErrorKind.StorageFailure, "The save storage could not be accessed.")]
    public void LoadGameHandlesTypedFailuresWithoutStartingSession(
        SaveStoreErrorKind kind,
        string expectedMessage)
    {
        Mock<IGameSaveStore> saveStore = new();
        saveStore
            .Setup(store => store.Load(
                It.IsAny<IPlayerDecisionProvider?>(),
                It.Is<IMatchRandomSource?>(source => source is SystemMatchRandomSource)))
            .Throws(new SaveStoreException(kind, "failure"));
        Mock<IConsoleWrapper> console = new();
        console.Setup(wrapper => wrapper.ReadLine()).Returns(string.Empty);

        Program.LoadGame(saveStore.Object, console.Object);

        saveStore.Verify(store => store.Load(
            It.IsAny<IPlayerDecisionProvider?>(),
            It.Is<IMatchRandomSource?>(source => source is SystemMatchRandomSource)), Times.Once);
        console.Verify(
            wrapper => wrapper.WriteLine(It.Is<string>(message => message.StartsWith(expectedMessage, StringComparison.Ordinal))),
            Times.Once);
        console.Verify(wrapper => wrapper.ReadLine(), Times.Once);
    }

    [Fact]
    public void PlayerActionMenuSavesThroughInjectedStoreExactlyOnce()
    {
        Game game = SyntheticGameFactory.Setup(new GameRules(2, 2, 6));
        Mock<IGameSaveStore> saveStore = new();
        Mock<IConsoleWrapper> console = new();
        PlayerActionMenu menu = new(game, game.CurrentPlayer, saveStore.Object, console.Object);

        menu.SaveCurrentGame();

        saveStore.Verify(store => store.Save(game), Times.Once);
        console.Verify(wrapper => wrapper.WriteLine(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void PlayerActionMenuHandlesTypedSaveFailure()
    {
        Game game = SyntheticGameFactory.Setup(new GameRules(2, 2, 6));
        Mock<IGameSaveStore> saveStore = new();
        saveStore
            .Setup(store => store.Save(game))
            .Throws(new SaveStoreException(SaveStoreErrorKind.StorageFailure, "write failed"));
        Mock<IConsoleWrapper> console = new();
        console.Setup(wrapper => wrapper.ReadLine()).Returns(string.Empty);
        PlayerActionMenu menu = new(game, game.CurrentPlayer, saveStore.Object, console.Object);

        menu.SaveCurrentGame();

        saveStore.Verify(store => store.Save(game), Times.Once);
        console.Verify(
            wrapper => wrapper.WriteLine(It.Is<string>(message => message.Contains("write failed", StringComparison.Ordinal))),
            Times.Once);
        console.Verify(wrapper => wrapper.ReadLine(), Times.Once);
    }

    [Fact]
    public void ConsoleGameCreatesEventAndTurnMenusWithItsInjectedStore()
    {
        Game game = SyntheticGameFactory.Setup(new GameRules(2, 2, 6));
        Mock<IConsoleWrapper> console = new();
        ConsolePrinter printer = new(console.Object, game);
        ConsoleLogPrinter logPrinter = new(console.Object);
        ConsoleCardPrinter cardPrinter = new(console.Object, game);
        Mock<IGameSaveStore> saveStore = new();
        Input input = new(console.Object, new Mock<IMenuOptionSelector>().Object);
        ConsolePlayerDecisionProvider decisions = new(printer, input, game, saveStore.Object);
        ConsoleGame consoleGame = new(
            game,
            printer,
            new List<TablePiece>(),
            input,
            logPrinter,
            cardPrinter,
            saveStore.Object,
            decisions);

        PlayerActionMenu menu = consoleGame.CreatePlayerActionMenu(game.CurrentPlayer);

        Assert.Same(saveStore.Object, consoleGame.SaveStore);
        Assert.Same(saveStore.Object, menu.CurrentSaveStore);
    }
}
