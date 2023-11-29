
namespace Monopoly.Console.GUI
{
    internal interface IConsoleWrapper
    {
        void Clear();
        string ReadKey();
        string ReadLine();
        void WriteLine(string s);
        void Write(string s);
        void SetTextColor(ConsoleColor color);
        void ResetColor();
        void SetPosition(int x, int y);
    }
}