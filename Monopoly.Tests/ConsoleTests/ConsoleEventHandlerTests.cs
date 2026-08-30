using Monopoly.Console;
using Monopoly.Console.Events;
using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core;
using Monopoly.Core.Models;
using Monopoly.Core.Persistence;
using Moq;

namespace Monopoly.Tests.ConsoleTests;

public class ConsoleEventHandlerTests
{
    [Fact]
    public void PostTurnRefreshDoesNotRenderNotifiedJailLogAgain()
    {
        ConsoleSession session = CreateSession();
        Player player = session.Game.CurrentPlayer;
        using IDisposable subscription = ConsoleEventHandler.Subscribe(session.ConsoleGame);

        session.Game.TheJail.PlayerGoToJail(player);
        session.ConsoleGame.UpdateGameInformation(
            session.Game.Board.Spaces[player.Position],
            player);

        Assert.Single(session.Game.Logs.LogList);
        Assert.Contains("sent to jail", session.Game.Logs.LogList[0].Info);
        Assert.Equal(1, CountLogRefreshes(session.Console));
    }

    [Fact]
    public void SimultaneousConsoleGamesReceiveOnlyTheirMatchNotifications()
    {
        ConsoleSession first = CreateSession();
        ConsoleSession second = CreateSession();
        using IDisposable firstSubscription = ConsoleEventHandler.Subscribe(first.ConsoleGame);
        using IDisposable secondSubscription = ConsoleEventHandler.Subscribe(second.ConsoleGame);

        first.Game.LogWriter.CreateLog("First game log");

        Assert.Equal(1, CountLogRefreshes(first.Console));
        Assert.Equal(0, CountLogRefreshes(second.Console));

        second.Game.LogWriter.CreateLog("Second game log");

        Assert.Equal(1, CountLogRefreshes(first.Console));
        Assert.Equal(1, CountLogRefreshes(second.Console));
    }

    [Fact]
    public void DisposingReplacedSessionKeepsCurrentSessionSubscribed()
    {
        ConsoleSession first = CreateSession();
        ConsoleSession second = CreateSession();
        IDisposable firstSubscription = ConsoleEventHandler.Subscribe(first.ConsoleGame);
        using IDisposable secondSubscription = ConsoleEventHandler.Subscribe(second.ConsoleGame);

        firstSubscription.Dispose();
        firstSubscription.Dispose();
        first.Game.LogWriter.CreateLog("Replaced session log");
        second.Game.LogWriter.CreateLog("Current session log");

        Assert.Equal(0, CountLogRefreshes(first.Console));
        Assert.Equal(1, CountLogRefreshes(second.Console));
        Assert.Equal(0, first.Game.NotificationSubscriberCount);
        Assert.Equal(1, second.Game.NotificationSubscriberCount);
    }

    [Fact]
    public void RepeatedSessionSubscriptionsDoNotAccumulateDelivery()
    {
        for (int cycle = 0; cycle < 3; cycle++)
        {
            ConsoleSession session = CreateSession();
            IDisposable subscription = ConsoleEventHandler.Subscribe(session.ConsoleGame);

            session.Game.LogWriter.CreateLog($"Cycle {cycle}");
            Assert.Equal(1, CountLogRefreshes(session.Console));

            subscription.Dispose();
            subscription.Dispose();
            session.Game.LogWriter.CreateLog($"After cycle {cycle}");

            Assert.Equal(1, CountLogRefreshes(session.Console));
            Assert.Equal(0, session.Game.NotificationSubscriberCount);
        }
    }

    [Fact]
    public void MultipleLogNotificationsKeepNewestEntriesVisibleInOrder()
    {
        ConsoleSession session = CreateSession();
        string[] entries = ["Payment", "Detention", "Event", "Landing"];
        using IDisposable subscription = ConsoleEventHandler.Subscribe(session.ConsoleGame);

        foreach (string entry in entries)
            session.Game.LogWriter.CreateLog(entry);

        Assert.Equal(entries.Length, CountLogRefreshes(session.Console));
        Assert.Equal(entries.Reverse(), GetLastRenderedEntries(session.Console, entries));
    }

    private static ConsoleSession CreateSession()
    {
        GameRules rules = new(2, 2, 6);
        Game game = SyntheticGameFactory.Setup(rules);
        Mock<IConsoleWrapper> console = new();
        ConsolePrinter printer = new(console.Object, game);
        ConsoleLogPrinter logPrinter = new(console.Object);
        ConsoleCardPrinter cardPrinter = new(console.Object, game);
        Mock<IGameSaveStore> saveStore = new();
        Input input = new(console.Object, new Mock<IMenuOptionSelector>().Object);
        ConsolePlayerDecisionProvider decisions = new(printer, input, game, saveStore.Object);
        List<TablePiece> tablePieces = game.Players
            .Select(player => new TablePiece
            {
                PlayerId = player.Id,
                Piece = player.Id.ToString(),
                Color = ConsoleColor.White
            })
            .ToList();
        ConsoleGame consoleGame = new(
            game,
            printer,
            tablePieces,
            input,
            logPrinter,
            cardPrinter,
            saveStore.Object,
            decisions);

        return new ConsoleSession(game, consoleGame, console);
    }

    private static int CountLogRefreshes(Mock<IConsoleWrapper> console) =>
        GetWrites(console).Count(write => write.StartsWith("┌─ Logs ", StringComparison.Ordinal));

    private static IReadOnlyList<string> GetLastRenderedEntries(
        Mock<IConsoleWrapper> console,
        IReadOnlyCollection<string> entries)
    {
        List<string> writes = GetWrites(console);
        int lastHeaderIndex = writes.FindLastIndex(write =>
            write.StartsWith("┌─ Logs ", StringComparison.Ordinal));

        return writes
            .Skip(lastHeaderIndex + 1)
            .Select(write => write.Trim())
            .Where(entries.Contains)
            .ToList();
    }

    private static List<string> GetWrites(Mock<IConsoleWrapper> console) =>
        console.Invocations
            .Where(invocation => invocation.Method.Name == nameof(IConsoleWrapper.Write))
            .Select(invocation => Assert.IsType<string>(invocation.Arguments[0]))
            .ToList();

    private sealed record ConsoleSession(
        Game Game,
        ConsoleGame ConsoleGame,
        Mock<IConsoleWrapper> Console);
}
