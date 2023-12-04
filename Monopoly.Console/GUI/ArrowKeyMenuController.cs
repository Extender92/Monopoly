using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class ArrowKeyMenuController
    {
        public static int HandleArrowKeyInput(bool canCancel, int index, int optionsPerLine, int optionCount, ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.LeftArrow:
                    {
                        if (index % optionsPerLine > 0)
                            index--;
                        break;
                    }
                case ConsoleKey.RightArrow:
                    {
                        if (index % optionsPerLine < optionsPerLine - 1)
                            index++;
                        break;
                    }
                case ConsoleKey.UpArrow:
                    {
                        if (index >= optionsPerLine)
                            index -= optionsPerLine;
                        break;
                    }
                case ConsoleKey.DownArrow:
                    {
                        if (index + optionsPerLine < optionCount)
                            index += optionsPerLine;
                        break;
                    }
                case ConsoleKey.Escape:
                    {
                        if (canCancel)
                            return -1;
                        break;
                    }
            }
            return index;
        }
    }
}
