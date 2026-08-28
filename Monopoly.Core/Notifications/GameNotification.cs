using Monopoly.Core.Logs;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models.FortuneCard;

namespace Monopoly.Core.Notifications;

/// <summary>A non-authoritative presentation hint emitted by one match.</summary>
public abstract record GameNotification;

public sealed record LogAddedNotification(Log Log) : GameNotification;

public sealed record CardDrawnNotification(
    IFortuneCardView Card,
    string PresentationToken) : GameNotification;

public sealed record SpaceReachedNotification(Square Space) : GameNotification;

public sealed record BoardChangedNotification : GameNotification;

public sealed record PlayerInformationChangedNotification : GameNotification;
