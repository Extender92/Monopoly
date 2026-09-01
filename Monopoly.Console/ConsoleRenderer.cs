using Monopoly.Console.GUI;

namespace Monopoly.Console;

internal sealed class ConsoleRenderer
{
    private readonly IConsoleWrapper _console;

    internal ConsoleRenderer(IConsoleWrapper console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    internal void RenderMatch(ConsoleMatchProjection match, IReadOnlyList<string> messages)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(messages);
        _console.Clear();
        _console.WriteLine(match.ProfileName);
        _console.WriteLine($"Round {match.RoundNumber} | {match.Phase} | Current: {match.CurrentPlayerName}");
        if (match.LastRoll is not null) _console.WriteLine($"Last roll: {match.LastRoll}");
        if (match.WinnerName is not null) _console.WriteLine($"Winner: {match.WinnerName}");

        _console.WriteLine(string.Empty);
        _console.WriteLine("Players");
        foreach (ConsolePlayerProjection player in match.Players)
        {
            string marker = player.IsCurrent ? "*" : " ";
            string resources = string.Join(", ", player.Resources.Select(resource =>
                $"{resource.Name}: {resource.FormattedValue}"));
            _console.WriteLine($"{marker} [{player.PlayerId}] {player.Name} @ {player.SpaceName} | {resources}");
        }

        if (match.Decision is not null)
        {
            _console.WriteLine(string.Empty);
            _console.WriteLine($"Decision: {match.Decision.Prompt}");
        }

        if (messages.Count > 0)
        {
            _console.WriteLine(string.Empty);
            _console.WriteLine("Recent events");
            foreach (string message in messages) _console.WriteLine($"- {message}");
        }

        _console.WriteLine(string.Empty);
    }

    internal void RenderTrack(ConsoleMatchProjection match)
    {
        ArgumentNullException.ThrowIfNull(match);
        _console.Clear();
        _console.WriteLine($"{match.ProfileName} - ordered route");
        _console.WriteLine(string.Empty);
        foreach (ConsoleSpaceProjection space in match.Spaces)
        {
            _console.Write($"{space.Index:D3} ");
            _console.SetTextColor(space.Color);
            _console.Write(space.Name);
            _console.ResetColor();

            List<string> state = [];
            if (space.Players.Count > 0) state.Add($"players: {string.Join(", ", space.Players)}");
            if (space.Owner is not null) state.Add($"owner: {space.Owner}");
            if (space.Capabilities.Count > 0) state.AddRange(space.Capabilities);
            if (state.Count > 0) _console.Write($" | {string.Join(" | ", state)}");
            _console.WriteLine(string.Empty);
        }

        WaitForReturn();
    }

    internal void RenderDecks(ConsoleMatchProjection match)
    {
        ArgumentNullException.ThrowIfNull(match);
        _console.Clear();
        _console.WriteLine($"{match.ProfileName} - decks");
        _console.WriteLine(string.Empty);
        if (match.Decks.Count == 0)
        {
            _console.WriteLine("This profile has no decks.");
        }
        else
        {
            foreach (ConsoleDeckProjection deck in match.Decks)
                _console.WriteLine($"{deck.Name} [{deck.Id}]: {deck.CardCount} cards");
        }

        _console.WriteLine(string.Empty);
        _console.WriteLine("Upcoming cards and deck order are not shown.");
        WaitForReturn();
    }

    internal void WriteMessage(string message) => _console.WriteLine(ConsoleText.Sanitize(message));

    private void WaitForReturn()
    {
        _console.WriteLine(string.Empty);
        _console.WriteLine("Press Enter to return.");
        _console.ReadLine();
    }
}
