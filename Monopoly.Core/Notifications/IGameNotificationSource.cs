namespace Monopoly.Core.Notifications;

/// <summary>
/// Exposes presentation notifications for one match. Subscribing does not
/// grant permission to publish notifications or mutate authoritative state.
/// </summary>
public interface IGameNotificationSource
{
    /// <summary>
    /// Subscribes to notifications from this match until the returned handle
    /// is disposed or the match ends.
    /// </summary>
    IDisposable Subscribe(Action<GameNotification> subscriber);
}
