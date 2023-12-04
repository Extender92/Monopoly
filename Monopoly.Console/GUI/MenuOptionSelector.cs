using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class MenuOptionSelector
    {
        private static IConsoleWrapper Console = new ConsoleWrapper();
        public static int GetSelectedOption(List<string> options, int index = 0, int optionsPerLine = 1, bool canCancel = false, ConsoleColor selectColor = ConsoleColor.Red)
        {
            if (optionsPerLine <= 0) optionsPerLine = 1;

            const int startX = 10;
            const int startY = 4;
            const int spacingPerLine = 20;

            ConsoleKey key;

            Console.ShowCursor(false);

            do
            {
                for (int i = 0; i < options.Count; i++)
                {
                    Console.SetPosition(startX + (i % optionsPerLine) * spacingPerLine, startY + i / optionsPerLine);

                    if (i == index) Console.SetTextColor(selectColor);

                    Console.Write(options[i]);

                    Console.ResetColor();
                }

                key = Console.GetPressedKey().Key;
                index = ArrowKeyMenuController.HandleArrowKeyInput(canCancel, index, optionsPerLine, options.Count, key);

            } while (key != ConsoleKey.Enter);

            Console.Clear();
            return index;
        }
    }
}
