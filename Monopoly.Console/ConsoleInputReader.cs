using System.Globalization;
using Monopoly.Console.GUI;
using Monopoly.Core;

namespace Monopoly.Console;

internal sealed class ConsoleInputReader
{
    private readonly IConsoleWrapper _console;

    internal ConsoleInputReader(IConsoleWrapper console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    internal int? ReadChoice(IReadOnlyList<string> options, bool allowCancel = false)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Count == 0) throw new ArgumentException("At least one option is required.", nameof(options));

        while (true)
        {
            for (int index = 0; index < options.Count; index++)
                _console.WriteLine($"{index + 1}. {options[index]}");
            if (allowCancel) _console.WriteLine("Press Enter to cancel.");
            _console.Write("> ");

            string input = _console.ReadLine().Trim();
            if (allowCancel && input.Length == 0) return null;
            if (int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out int selected) &&
                selected >= 1 && selected <= options.Count)
            {
                return selected - 1;
            }

            _console.WriteLine($"Enter a number from 1 to {options.Count}.");
        }
    }

    internal IReadOnlyList<PlayerSetup>? ReadPlayers(ProfileSetupDefinition setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        int? count = ReadPlayerCount(setup.MinimumPlayers, setup.MaximumPlayers);
        if (count is null) return null;

        List<PlayerSetup> players = new(count.Value);
        for (int index = 0; index < count.Value; index++)
        {
            while (true)
            {
                _console.Write($"Player {index + 1} name (Enter cancels): ");
                string input = _console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) return null;

                string name = input.Trim();
                if (!ConsoleText.IsSafePlayerName(name))
                {
                    _console.WriteLine("Player names cannot contain control characters.");
                    continue;
                }

                players.Add(new PlayerSetup(index, name));
                break;
            }
        }

        return players.AsReadOnly();
    }

    private int? ReadPlayerCount(int minimum, int maximum)
    {
        while (true)
        {
            _console.Write($"Number of players ({minimum}-{maximum}, Enter cancels): ");
            string input = _console.ReadLine().Trim();
            if (input.Length == 0) return null;
            if (int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out int count) &&
                count >= minimum && count <= maximum)
            {
                return count;
            }

            _console.WriteLine($"Enter a whole number from {minimum} to {maximum}.");
        }
    }
}
