using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class MenuOptionSelector
    {
        public int StartX { get; set; } = 10;
        public int StartY { get; set; } = 2;

        private IConsoleWrapper Console { get; set; }

        public MenuOptionSelector(IConsoleWrapper consoleWrapper)
        {
            Console = consoleWrapper;
        }

        internal void SetPositions()
        {
            StartX = ConsolePositions.MenuPosX;
            StartY = ConsolePositions.MenuPosY;
        }

        public int GetSelectedOption(List<string> options, int spacingPerLine = 18, int index = 0, int optionsPerLine = 1, bool canCancel = false, ConsoleColor selectColor = ConsoleColor.Red)
        {
            const int spacingBuffer = 2;
            spacingPerLine += spacingBuffer;

            if (optionsPerLine <= 0) optionsPerLine = 1;

            ConsoleKey key;

            Console.ShowCursor(false);

            do
            {
                for (int i = 0; i < options.Count; i++)
                {
                    Console.SetPosition(StartX + (i % optionsPerLine) * spacingPerLine, StartY + i / optionsPerLine);

                    if (i == index) Console.SetTextColor(selectColor);

                    Console.Write(options[i]);

                    Console.ResetColor();
                }

                key = Console.GetPressedKey().Key;
                index = ArrowKeyMenuController.HandleArrowKeyInput(canCancel, index, optionsPerLine, options.Count, key);

            } while (key != ConsoleKey.Enter);

            //Console.Clear();
            return index;
        }
    }
}
