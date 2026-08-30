using Monopoly.Core.Logs;
using Monopoly.Core.Presentation;

namespace Monopoly.Core.Notifications;

/// <summary>A non-authoritative presentation hint emitted by one match after commit.</summary>
public abstract record GameNotification(PresentationToken PresentationToken);

public sealed record LogAddedNotification(Log Log, PresentationToken Token)
    : GameNotification(Token);

public sealed record PlayerMovedNotification(
    int PlayerId,
    SpaceId FromSpaceId,
    SpaceId ToSpaceId,
    int OriginPasses,
    PresentationToken Token)
    : GameNotification(Token);

public sealed record ResourceChangedNotification(
    int PlayerId,
    ResourceId ResourceId,
    int PreviousValue,
    int CurrentValue,
    PresentationToken Token)
    : GameNotification(Token);

public sealed record OwnershipChangedNotification(
    SpaceId SpaceId,
    int? PreviousOwnerPlayerId,
    int? CurrentOwnerPlayerId,
    PresentationToken Token)
    : GameNotification(Token);

public sealed record CardDrawnNotification(
    ICardView Card,
    DeckId DeckId,
    PresentationToken DeckPresentationToken)
    : GameNotification(DeckPresentationToken);

public sealed record TurnAdvancedNotification(int CurrentPlayerId, int RoundNumber, PresentationToken Token)
    : GameNotification(Token);

public sealed record MatchEndedNotification(
    int WinnerPlayerId,
    int RoundNumber,
    ResourceId ScoreResourceId,
    PresentationToken Token)
    : GameNotification(Token);
