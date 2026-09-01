namespace Monopoly.Console.GUI;

internal interface IConsoleWrapper
{
    void Clear();
    string ReadLine();
    void WriteLine(string value);
    void Write(string value);
    void SetTextColor(ConsoleColor color);
    void ResetColor();
}
