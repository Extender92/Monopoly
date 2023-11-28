using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class Print
    {
        internal static void PrintCard(string header, int posX, int PosY, int lengthX, int lenghtY, List<string> info, ConsoleColor borderColor, ConsoleColor textColor)
        {
            // Header Text
            System.Console.ForegroundColor = textColor;
            System.Console.SetCursorPosition(posX + 1, PosY + 1);
            System.Console.Write(header);

            // Header Border
            System.Console.ForegroundColor = borderColor;
            System.Console.SetCursorPosition(posX, PosY);
            System.Console.Write('┌' + new String('─', lengthX) + '┐');

            System.Console.SetCursorPosition(posX, PosY + 1);
            System.Console.Write('│');
            System.Console.SetCursorPosition((posX + lengthX + 1), PosY + 1);
            System.Console.Write('│');

            System.Console.SetCursorPosition(posX, PosY + 2);
            System.Console.Write('├' + new String('─', lengthX) + '┤');

            // Body
            for (int i = 0; i < lenghtY; i++)
            {
                System.Console.SetCursorPosition(posX, PosY + 3 + i);
                System.Console.Write('│');

                System.Console.ForegroundColor = textColor;
                System.Console.Write(i >= info.Count ?
                    new String(' ', lengthX) :
                    " " + info[i]);

                System.Console.ForegroundColor = borderColor;
                System.Console.SetCursorPosition((posX + lengthX + 1), PosY + 3 + i);
                System.Console.Write('│');
            }

            // Footer
            System.Console.SetCursorPosition(posX, PosY + (lenghtY + 2));
            System.Console.Write('└' + new String('─', lengthX) + '┘');
            System.Console.ResetColor();
        }
    }
}
