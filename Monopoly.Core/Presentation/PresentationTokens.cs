namespace Monopoly.Core.Presentation;

/// <summary>Semantic presentation keys used by the current engine contracts.</summary>
public static class PresentationTokens
{
    public static PresentationToken PrimaryResource { get; } = new("resource.primary");
    public static PresentationToken PrimaryDeck { get; } = new("deck.primary");
    public static PresentationToken SecondaryDeck { get; } = new("deck.secondary");
    public static PresentationToken PropertyPurchaseDecision { get; } = new("decision.property-purchase");
    public static PresentationToken DetentionReleaseDecision { get; } = new("decision.detention-release");
    public static PresentationToken DetainedStatus { get; } = new("status.detained");
    public static PresentationToken LogNotification { get; } = new("notification.log");
    public static PresentationToken BoardNotification { get; } = new("notification.board");
    public static PresentationToken PlayerInformationNotification { get; } = new("notification.player-information");
}
