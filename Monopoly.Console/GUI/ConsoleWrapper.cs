namespace Monopoly.Console.GUI;

internal sealed class ConsoleWrapper : IConsoleWrapper
{
    public void Clear() => System.Console.Clear();

    public string ReadLine() => System.Console.ReadLine() ?? string.Empty;

    public void WriteLine(string value) => System.Console.WriteLine(value);

    public void Write(string value) => System.Console.Write(value);

    public void SetTextColor(ConsoleColor color) => System.Console.ForegroundColor = color;

    public void ResetColor() => System.Console.ResetColor();
}
