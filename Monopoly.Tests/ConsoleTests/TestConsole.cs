using Monopoly.Console.GUI;

namespace Monopoly.Tests.ConsoleTests;

internal sealed class TestConsole : IConsoleWrapper
{
    private readonly Queue<string> _input;

    internal TestConsole(params string[] input) => _input = new Queue<string>(input);

    internal List<string> Lines { get; } = [];
    internal List<string> Writes { get; } = [];
    internal List<ConsoleColor> Colors { get; } = [];
    internal int ClearCount { get; private set; }
    internal string Output => string.Join("\n", Lines.Concat(Writes));

    public void Clear() => ClearCount++;

    public string ReadLine() => _input.Count > 0
        ? _input.Dequeue()
        : throw new InvalidOperationException("The fake Console has no remaining input.");

    public void WriteLine(string value) => Lines.Add(value);

    public void Write(string value) => Writes.Add(value);

    public void SetTextColor(ConsoleColor color) => Colors.Add(color);

    public void ResetColor() { }
}
