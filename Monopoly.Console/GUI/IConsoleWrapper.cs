
namespace Monopoly.Console.GUI
{
    internal interface IConsoleWrapper
    {
        void Clear();
        string ReadKey();
        string ReadLine();
        void SetColor(ConsoleColor color);
        void WriteLine(string s);
    }
}