using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class ConsoleWrapper : IConsoleWrapper
    {
        public void Clear() => System.Console.Clear();

        public string ReadLine() => System.Console.ReadLine();

        public string ReadKey() => System.Console.ReadKey().KeyChar.ToString();

        public void WriteLine(string s) => System.Console.WriteLine(s);

        public void Write(string s) => System.Console.Write(s);

        public void SetTextColor(ConsoleColor color) => System.Console.ForegroundColor = color;
        
        public void ResetColor() => System.Console.ResetColor();

        public void SetPosition(int x, int y) => System.Console.SetCursorPosition(x, y);
    }
}
