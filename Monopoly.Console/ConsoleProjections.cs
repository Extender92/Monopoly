namespace Monopoly.Console;

internal sealed record ConsoleResourceProjection(string Name, string FormattedValue);

internal sealed record ConsolePlayerProjection(
    int PlayerId,
    string Name,
    string SpaceName,
    bool IsCurrent,
    IReadOnlyList<ConsoleResourceProjection> Resources);

internal sealed record ConsoleSpaceProjection(
    int Index,
    string Name,
    ConsoleColor Color,
    IReadOnlyList<string> Players,
    string? Owner,
    IReadOnlyList<string> Capabilities);

internal sealed record ConsoleDeckProjection(string Id, string Name, int CardCount);

internal sealed record ConsoleDecisionOptionProjection(
    Monopoly.Core.DecisionOptionId Id,
    string Label);

internal sealed record ConsoleDecisionProjection(
    Guid DecisionId,
    int PlayerId,
    string Prompt,
    IReadOnlyList<ConsoleDecisionOptionProjection> Options);

internal sealed record ConsoleMatchProjection(
    string ProfileName,
    string Phase,
    int RoundNumber,
    string CurrentPlayerName,
    string? LastRoll,
    IReadOnlyList<ConsolePlayerProjection> Players,
    IReadOnlyList<ConsoleSpaceProjection> Spaces,
    IReadOnlyList<ConsoleDeckProjection> Decks,
    ConsoleDecisionProjection? Decision,
    string? WinnerName);
