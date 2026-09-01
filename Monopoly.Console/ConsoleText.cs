using System.Text;

namespace Monopoly.Console;

internal static class ConsoleText
{
    internal static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        StringBuilder result = new(value.Length);
        foreach (char character in value)
            result.Append(char.IsControl(character) ? ' ' : character);
        return result.ToString();
    }

    internal static bool IsSafePlayerName(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.All(character => !char.IsControl(character));
}
