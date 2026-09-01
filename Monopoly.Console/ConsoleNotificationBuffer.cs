using Monopoly.Core.Notifications;

namespace Monopoly.Console;

internal sealed class ConsoleNotificationBuffer : IDisposable
{
    private readonly object _gate = new();
    private readonly List<GameNotification> _notifications = [];
    private readonly IDisposable _subscription;

    internal ConsoleNotificationBuffer(IGameNotificationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _subscription = source.Subscribe(notification =>
        {
            lock (_gate) _notifications.Add(notification);
        });
    }

    internal IReadOnlyList<GameNotification> Drain()
    {
        lock (_gate)
        {
            GameNotification[] result = _notifications.ToArray();
            _notifications.Clear();
            return Array.AsReadOnly(result);
        }
    }

    public void Dispose() => _subscription.Dispose();
}
