namespace Monopoly.Console;

internal sealed class ConsoleCommandLineException : Exception
{
    internal ConsoleCommandLineException(string message)
        : base(message)
    {
    }
}

internal sealed record ConsoleCommandLineOptions(bool ShowHelp, string? ProfilePath)
{
    internal static ConsoleCommandLineOptions Parse(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Count == 0)
            return new ConsoleCommandLineOptions(false, null);
        if (arguments.Count == 1 && arguments[0] == "--help")
            return new ConsoleCommandLineOptions(true, null);

        string? profilePath = null;
        for (int index = 0; index < arguments.Count; index++)
        {
            string argument = arguments[index];
            if (argument != "--profile")
                throw new ConsoleCommandLineException($"Unknown command-line option '{argument}'.");
            if (profilePath is not null)
                throw new ConsoleCommandLineException("The --profile option can only be specified once.");
            if (index + 1 >= arguments.Count ||
                string.IsNullOrWhiteSpace(arguments[index + 1]) ||
                arguments[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ConsoleCommandLineException("The --profile option requires a path.");
            }

            profilePath = arguments[++index];
        }

        return new ConsoleCommandLineOptions(false, profilePath);
    }
}
