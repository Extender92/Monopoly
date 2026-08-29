using Monopoly.Core.Logs;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Presentation;

namespace Monopoly.Core.Notifications;

/// <summary>A non-authoritative presentation hint emitted by one match.</summary>
public abstract record GameNotification(PresentationToken PresentationToken);

public sealed record LogAddedNotification(Log Log)
    : GameNotification(PresentationTokens.LogNotification);

public sealed record CardDrawnNotification(
    ICardView Card,
    DeckId DeckId,
    PresentationToken DeckPresentationToken)
    : GameNotification(DeckPresentationToken);

public sealed record SpaceReachedNotification(Square Space)
    : GameNotification(Space.PresentationToken);

public sealed record BoardChangedNotification()
    : GameNotification(PresentationTokens.BoardNotification);

public sealed record PlayerInformationChangedNotification()
    : GameNotification(PresentationTokens.PlayerInformationNotification);
