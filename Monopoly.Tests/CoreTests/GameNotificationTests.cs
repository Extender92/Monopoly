using Monopoly.Core.Notifications;

namespace Monopoly.Tests.CoreTests;

public class GameNotificationTests
{
    [Fact]
    public void SimultaneousMatchesNeverShareSubscribers()
    {
        Game first = new GameTestBuilder().Build();
        Game second = new GameTestBuilder().Build();
        List<GameNotification> firstNotifications = [];
        List<GameNotification> secondNotifications = [];
        using IDisposable firstSubscription = first.Notifications.Subscribe(firstNotifications.Add);
        using IDisposable secondSubscription = second.Notifications.Subscribe(secondNotifications.Add);

        first.LogWriter.CreateLog("First");

        Assert.Single(firstNotifications);
        Assert.IsType<LogAddedNotification>(firstNotifications[0]);
        Assert.Empty(secondNotifications);

        second.LogWriter.CreateLog("Second");

        Assert.Single(firstNotifications);
        Assert.Single(secondNotifications);
        Assert.IsType<LogAddedNotification>(secondNotifications[0]);
    }

    [Fact]
    public void SubscriptionDisposalIsIdempotent()
    {
        Game game = new GameTestBuilder().Build();
        int delivered = 0;
        IDisposable subscription = game.Notifications.Subscribe(_ => delivered++);

        Assert.Equal(1, game.NotificationSubscriberCount);

        subscription.Dispose();
        subscription.Dispose();
        game.LogWriter.CreateLog("After disposal");

        Assert.Equal(0, delivered);
        Assert.Equal(0, game.NotificationSubscriberCount);
    }

    [Fact]
    public void SubscriberFailureCannotInterruptAuthoritativeMutationOrOtherSubscribers()
    {
        Game game = new GameTestBuilder().Build();
        int delivered = 0;
        using IDisposable failing = game.Notifications.Subscribe(_ =>
            throw new InvalidOperationException("Presentation failed."));
        using IDisposable observing = game.Notifications.Subscribe(_ => delivered++);

        game.LogWriter.CreateLog("Committed log");

        Assert.Single(game.Logs.LogList);
        Assert.Equal("Committed log", game.Logs.LogList[0].Info);
        Assert.Equal(1, delivered);
    }

    [Fact]
    public void SubscriberCannotReenterAuthoritativeGameOperations()
    {
        Game game = new GameTestBuilder().Build();
        GameActionResult? reentrantResult = null;
        int turnBefore = game.CurrentTurn;
        int positionBefore = game.CurrentPlayer.Position;
        using IDisposable subscription = game.Notifications.Subscribe(_ =>
            reentrantResult = game.PlayTurn());

        game.LogWriter.CreateLog("Presentation-only notification");

        Assert.NotNull(reentrantResult);
        Assert.Equal(GameActionStatus.Rejected, reentrantResult.Status);
        Assert.Equal(GameActionRejectionReason.OperationInProgress, reentrantResult.RejectionReason);
        Assert.Equal(turnBefore, game.CurrentTurn);
        Assert.Equal(positionBefore, game.CurrentPlayer.Position);
    }

    [Fact]
    public void GameOverReleasesMatchSubscriptions()
    {
        Game game = new GameTestBuilder()
            .WithPlayer(1, money: 0, isBankrupt: true)
            .Build();
        IDisposable subscription = game.Notifications.Subscribe(_ => { });

        GameActionResult result = game.PlayTurn();

        Assert.Equal(GameActionStatus.GameOver, result.Status);
        Assert.Equal(0, game.NotificationSubscriberCount);
        subscription.Dispose();
    }

    [Fact]
    public void PublicNotificationSourceDoesNotExposePublication()
    {
        Assert.DoesNotContain(
            typeof(IGameNotificationSource).GetMethods(),
            method => method.Name.Contains("Publish", StringComparison.Ordinal));
    }
}
