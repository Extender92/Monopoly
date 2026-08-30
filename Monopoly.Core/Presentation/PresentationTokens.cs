namespace Monopoly.Core.Presentation;

/// <summary>Engine-owned fallback keys. Authoritative rules never depend on them.</summary>
public static class PresentationTokens
{
    public static PresentationToken LogNotification { get; } = new("notification.log");
}
