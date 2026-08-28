namespace Monopoly.Core.Notifications;

internal sealed class GameNotificationHub : IGameNotificationSource
{
    private readonly object _gate = new();
    private readonly Dictionary<long, Action<GameNotification>> _subscribers = [];
    private long _nextSubscriptionId;
    private bool _completed;

    internal int SubscriberCount
    {
        get
        {
            lock (_gate)
            {
                return _subscribers.Count;
            }
        }
    }

    public IDisposable Subscribe(Action<GameNotification> subscriber)
    {
        ArgumentNullException.ThrowIfNull(subscriber);

        lock (_gate)
        {
            if (_completed)
                return EmptySubscription.Instance;

            long id = checked(++_nextSubscriptionId);
            _subscribers.Add(id, subscriber);
            return new Subscription(this, id);
        }
    }

    internal void Publish(GameNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        Action<GameNotification>[] subscribers;
        lock (_gate)
        {
            if (_completed || _subscribers.Count == 0)
                return;

            subscribers = _subscribers.Values.ToArray();
        }

        foreach (Action<GameNotification> subscriber in subscribers)
        {
            try
            {
                subscriber(notification);
            }
            catch (Exception)
            {
                // Presentation failures must not interrupt authoritative rules.
            }
        }
    }

    internal void Complete()
    {
        lock (_gate)
        {
            _completed = true;
            _subscribers.Clear();
        }
    }

    private void Unsubscribe(long id)
    {
        lock (_gate)
        {
            _subscribers.Remove(id);
        }
    }

    private sealed class Subscription(GameNotificationHub owner, long id) : IDisposable
    {
        private GameNotificationHub? _owner = owner;

        public void Dispose() =>
            Interlocked.Exchange(ref _owner, null)?.Unsubscribe(id);
    }

    private sealed class EmptySubscription : IDisposable
    {
        internal static EmptySubscription Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
