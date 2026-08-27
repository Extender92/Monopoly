using Monopoly.Console;
using Monopoly.Console.Events;
using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core;
using Monopoly.Core.Events;
using Monopoly.Core.Models;
using Monopoly.Core.Persistence;
using Moq;
using System.Reflection;

namespace Monopoly.Tests.ConsoleTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsoleEventCollection
{
    public const string Name = "Console events";
}

[Collection(ConsoleEventCollection.Name)]
public class ConsoleEventHandlerTests
{
    [Fact]
    public void PostTurnRefreshDoesNotRenderNotifiedJailLogAgain()
    {
        ConsoleSession session = CreateSession();
        Player player = session.Game.CurrentPlayer;

        ConsoleEventHandler.SubscribeToEvents(session.ConsoleGame);
        try
        {
            session.Game.TheJail.PlayerGoToJail(player);
            session.ConsoleGame.UpdateGameInformation(
                session.Game.Board.GetSquareAtPosition(player.Position),
                player);

            Assert.Single(session.Game.Logs.LogList);
            Assert.Contains("sent to jail", session.Game.Logs.LogList[0].Info);
            Assert.Equal(1, CountLogRefreshes(session.Console));
        }
        finally
        {
            ConsoleEventHandler.UnsubscribeFromEvents(session.ConsoleGame);
        }
    }

    [Fact]
    public void StartingAnotherConsoleGameDoesNotDuplicateOrMisrouteSubscribers()
    {
        ConsoleSession first = CreateSession();
        ConsoleSession second = CreateSession();

        ConsoleEventHandler.SubscribeToEvents(first.ConsoleGame);
        try
        {
            Assert.Equal(1, GetSubscriberCount("UpdateGameBoard"));

            ConsoleEventHandler.SubscribeToEvents(second.ConsoleGame);
            Assert.Equal(1, GetSubscriberCount("UpdateGameBoard"));

            first.Game.LogWriter.CreateLog("First game log");
            second.Game.LogWriter.CreateLog("Second game log");

            Assert.Equal(0, CountLogRefreshes(first.Console));
            Assert.Equal(1, CountLogRefreshes(second.Console));
        }
        finally
        {
            ConsoleEventHandler.UnsubscribeFromEvents(second.ConsoleGame);
            ConsoleEventHandler.UnsubscribeFromEvents(first.ConsoleGame);
        }

        Assert.Equal(0, GetSubscriberCount("UpdateGameBoard"));
    }

    [Fact]
    public void UnsubscribingReplacedSessionKeepsCurrentSessionSubscribed()
    {
        ConsoleSession first = CreateSession();
        ConsoleSession second = CreateSession();

        ConsoleEventHandler.SubscribeToEvents(first.ConsoleGame);
        ConsoleEventHandler.SubscribeToEvents(second.ConsoleGame);
        try
        {
            ConsoleEventHandler.UnsubscribeFromEvents(first.ConsoleGame);
            second.Game.LogWriter.CreateLog("Current session log");

            Assert.Equal(1, CountLogRefreshes(second.Console));

            ConsoleEventHandler.UnsubscribeFromEvents(second.ConsoleGame);
            second.Game.LogWriter.CreateLog("Log after session cleanup");

            Assert.Equal(1, CountLogRefreshes(second.Console));
        }
        finally
        {
            ConsoleEventHandler.UnsubscribeFromEvents(second.ConsoleGame);
            ConsoleEventHandler.UnsubscribeFromEvents(first.ConsoleGame);
        }
    }

    [Fact]
    public void RepeatedSubscribeAndUnsubscribeDoesNotAccumulateLogDelivery()
    {
        for (int cycle = 0; cycle < 3; cycle++)
        {
            ConsoleSession session = CreateSession();

            ConsoleEventHandler.SubscribeToEvents(session.ConsoleGame);
            ConsoleEventHandler.SubscribeToEvents(session.ConsoleGame);
            try
            {
                session.Game.LogWriter.CreateLog($"Cycle {cycle}");
                Assert.Equal(1, CountLogRefreshes(session.Console));
            }
            finally
            {
                ConsoleEventHandler.UnsubscribeFromEvents(session.ConsoleGame);
            }

            session.Game.LogWriter.CreateLog($"After cycle {cycle}");
            Assert.Equal(1, CountLogRefreshes(session.Console));
            Assert.Equal(0, GetSubscriberCount("LogAddedEvent"));
        }
    }

    [Fact]
    public void MultipleLogNotificationsKeepNewestEntriesVisibleInOrder()
    {
        ConsoleSession session = CreateSession();
        string[] entries = ["Payment", "Jail", "Card", "Landing"];

        ConsoleEventHandler.SubscribeToEvents(session.ConsoleGame);
        try
        {
            foreach (string entry in entries)
                session.Game.LogWriter.CreateLog(entry);

            Assert.Equal(entries.Length, CountLogRefreshes(session.Console));
            Assert.Equal(entries.Reverse(), GetLastRenderedEntries(session.Console, entries));
        }
        finally
        {
            ConsoleEventHandler.UnsubscribeFromEvents(session.ConsoleGame);
        }
    }

    private static ConsoleSession CreateSession()
    {
        GameRules rules = new(2, 2, 6);
        Game game = CoreGameSetup.Setup(rules);
        Mock<IConsoleWrapper> console = new();
        ConsolePrinter printer = new(console.Object, game.Board.Squares, rules);
        ConsoleLogPrinter logPrinter = new(console.Object);
        ConsoleCardPrinter cardPrinter = new(console.Object, game.Board.Squares, rules);
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
            null!,
            logPrinter,
            cardPrinter,
            new Mock<IGameSaveStore>().Object);

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

    private static int GetSubscriberCount(string eventName)
    {
        FieldInfo? field = typeof(GameEvents)
            .GetField(eventName, BindingFlags.Static | BindingFlags.NonPublic);
        return ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0;
    }

    private sealed record ConsoleSession(
        Game Game,
        ConsoleGame ConsoleGame,
        Mock<IConsoleWrapper> Console);
}
