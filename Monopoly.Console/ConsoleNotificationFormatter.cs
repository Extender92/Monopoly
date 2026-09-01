using Monopoly.Console.GUI;
using Monopoly.Core;
using Monopoly.Core.Models;
using Monopoly.Core.Notifications;

namespace Monopoly.Console;

internal sealed class ConsoleNotificationFormatter
{
    internal IReadOnlyList<string> Format(Game game, IEnumerable<GameNotification> notifications)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(notifications);
        ConsolePresentationResolver presentation = new(game.Presentation);
        string[] messages = notifications.Select(notification =>
            FormatOne(game, presentation, notification)).ToArray();
        return Array.AsReadOnly(messages);
    }

    private static string FormatOne(
        Game game,
        ConsolePresentationResolver presentation,
        GameNotification notification) => notification switch
        {
            LogAddedNotification added => ConsoleText.Sanitize(added.Log.Info),
            PlayerMovedNotification moved => FormatMove(game, presentation, moved),
            ResourceChangedNotification changed =>
                $"{PlayerName(game, changed.PlayerId)}: " +
                $"{presentation.GetDisplayText(ConsoleProjectionBuilder.ResourceToken(game.Profile, changed.ResourceId))} " +
                $"changed from {changed.PreviousValue} to {changed.CurrentValue}.",
            OwnershipChangedNotification changed =>
                changed.CurrentOwnerPlayerId is int owner
                    ? $"{PlayerName(game, owner)} acquired {SpaceName(game, presentation, changed.SpaceId)}."
                    : $"{SpaceName(game, presentation, changed.SpaceId)} is now unowned.",
            DecisionResolvedNotification resolved =>
                $"{PlayerName(game, resolved.PlayerId)} chose {DecisionOptionText(resolved.Response)}.",
            CardDrawnNotification drawn =>
                $"{presentation.GetDisplayText(drawn.DeckPresentationToken)}: " +
                presentation.GetDisplayText(drawn.Card.PresentationToken),
            TurnAdvancedNotification advanced =>
                $"Round {advanced.RoundNumber}: {PlayerName(game, advanced.CurrentPlayerId)} is next.",
            MatchEndedNotification ended =>
                $"{PlayerName(game, ended.WinnerPlayerId)} won after round {ended.RoundNumber}.",
            _ => throw new ConsoleProjectionException(
                ConsoleProjectionErrorKind.InconsistentState,
                $"Notification '{notification.GetType().Name}' is not supported by the Console baseline.")
        };

    private static string FormatMove(
        Game game,
        ConsolePresentationResolver presentation,
        PlayerMovedNotification moved)
    {
        string passes = moved.OriginPasses > 0
            ? $" and passed the route origin {moved.OriginPasses} time(s)"
            : string.Empty;
        return $"{PlayerName(game, moved.PlayerId)} moved from " +
            $"{SpaceName(game, presentation, moved.FromSpaceId)} to " +
            $"{SpaceName(game, presentation, moved.ToSpaceId)}{passes}.";
    }

    private static string PlayerName(Game game, int playerId)
    {
        Player? player = game.Players.SingleOrDefault(candidate => candidate.Id == playerId);
        return player is null
            ? throw new ConsoleProjectionException(
                ConsoleProjectionErrorKind.InconsistentState,
                $"Notification references unknown player '{playerId}'.")
            : ConsoleText.Sanitize(player.Name);
    }

    private static string SpaceName(
        Game game,
        ConsolePresentationResolver presentation,
        SpaceId spaceId) =>
        presentation.GetDisplayText(game.Board.GetSpace(spaceId).PresentationToken);

    private static string DecisionOptionText(DecisionOptionId option) => option switch
    {
        var id when id == DecisionOptions.Accept => "accept",
        var id when id == DecisionOptions.Decline => "decline",
        _ => ConsoleText.Sanitize(option.Value)
    };
}
