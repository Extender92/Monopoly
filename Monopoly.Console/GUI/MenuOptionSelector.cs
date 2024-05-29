using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    public interface IMenuOptionSelector
    {
        int GetSelectedOption(List<string> options, int spacingPerLine = 18, int index = 0, int optionsPerLine = 1, bool canCancel = false, ConsoleColor selectColor = ConsoleColor.Red);
        void SetPositions();
    }

    internal class MenuOptionSelector : IMenuOptionSelector
    {
        public int StartX { get; set; }
        public int StartY { get; set; }

        private IConsoleWrapper Console { get; set; }

        public MenuOptionSelector(IConsoleWrapper consoleWrapper)
        {
            Console = consoleWrapper;
            SetPositions();
        }

        public void SetPositions()
        {
            StartX = ConsolePositions.MenuPosX;
            StartY = ConsolePositions.MenuPosY;
        }

        public void ClearMenuZone(int length, int height)
        {
            if (length <= 0 || height <= 0)
            {
                throw new ArgumentException("Length and height must be positive values.");
            }

            int xMin = (StartX - 10) > 0 ? (StartX - 10) : 0;
            int xMax = (StartX + length + 10);
            int width = xMax - xMin;

            for (int i = 0; i < height; i++)
            {
                Console.SetPosition(xMin, (StartY + i));
                Console.Write(new String(' ', width));
            }
        }

        public int GetSelectedOption(List<string> options, int spacingPerLine = 18, int index = 0, int optionsPerLine = 1, bool canCancel = false, ConsoleColor selectColor = ConsoleColor.Red)
        {
            const int spacingBuffer = 2;
            spacingPerLine += spacingBuffer;

            if (optionsPerLine <= 0) optionsPerLine = 1;

            ConsoleKey key;

            Console.ShowCursor(false);

            ClearMenuZone((spacingPerLine * optionsPerLine), (options.Count / optionsPerLine));

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

            ClearMenuZone(options.Max(s => s.Length), options.Count);

            return index;
        }
    }
}
