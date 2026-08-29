using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class ListOptionSelector
    {
        private readonly IConsoleWrapper Console;
        private readonly ConsolePresentationResolver _presentation;

        public ListOptionSelector(Monopoly.Core.Presentation.ProfilePresentation presentation)
        {
            Console = new ConsoleWrapper();
            _presentation = new ConsolePresentationResolver(presentation);
        }

        public int GetSelectedOption(List<Square> options, int spacingPerLine = 18, int index = 0, string errorMessage = "", int optionsPerLine = 1, bool canCancel = false, ConsoleColor selectColor = ConsoleColor.Red)
        {
            ConsoleColor textColor = ConsoleColor.White;

            const int spacingBuffer = 4;
            spacingPerLine += spacingBuffer;

            if (optionsPerLine <= 0) optionsPerLine = 1;

            ConsoleKey key;

            Console.ShowCursor(false);

            do
            {
                Console.Clear();

                Console.SetPosition(ConsolePositions.ListMenuPosX, ConsolePositions.ListMenuPosY - 2);
                Console.Write(errorMessage);

                for (int i = 0; i < options.Count; i++)
                {
                    Console.SetPosition(ConsolePositions.ListMenuPosX + (i % optionsPerLine) * spacingPerLine, ConsolePositions.ListMenuPosY + i / optionsPerLine);

                    textColor = options[i] is PropertySquare property
                        ? _presentation.GetColor(property.GroupPresentationToken)
                        : ConsoleColor.White;
                    Console.SetTextColor(textColor);

                    if (i == index)
                    {
                        Console.Write(_presentation.GetDisplayText(options[i].PresentationToken));

                        Console.SetTextColor(selectColor);
                        Console.Write(" *");

                        Console.SetPosition((ConsolePositions.ListMenuPosX + (i % optionsPerLine) * spacingPerLine) - 2, ConsolePositions.ListMenuPosY + i / optionsPerLine);
                        Console.Write("*");
                    }
                    else Console.Write(_presentation.GetDisplayText(options[i].PresentationToken));
                }

                Console.ResetColor();

                key = Console.GetPressedKey().Key;
                index = ArrowKeyMenuController.HandleArrowKeyInput(canCancel, index, optionsPerLine, options.Count, key);

            } while (key != ConsoleKey.Enter);

            Console.Clear();

            return index;
        }
    }
}
