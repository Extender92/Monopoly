using Monopoly.Console.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class TablePieceInputManager
    {
        private readonly IConsoleWrapper _console;

        public TablePieceInputManager(IConsoleWrapper console)
        {
            _console = console;
        }


        internal TablePiece GetTablePieceFromUserInput(int playerId)
        {
            TablePiece tablePiece = new TablePiece();
            do
            {
                do
                {
                    tablePiece.Piece = GetUserSelectedPieceKey(playerId);
                    _console.Write("\n You entered: " + tablePiece.Piece + "\n Do you want to continue?");

                } while (!Input.GetUserConfirmation());

                do
                {
                    tablePiece.Color = GetUserSelectedColor(playerId);

                    _console.Write("\n You selected color: ");
                    ConsolePrinter.PrintColoredText(tablePiece.Color.ToString(), tablePiece.Color);
                    _console.WriteLine("\n Do you want to continue?");

                } while (!Input.GetUserConfirmation());

                _console.Write("\n You entered: ");
                ConsolePrinter.PrintColoredText(tablePiece.Piece, tablePiece.Color);
                _console.Write(" With color: ");
                ConsolePrinter.PrintColoredText(tablePiece.Color.ToString(), tablePiece.Color);
                _console.WriteLine("\n Do you want to continue?");

            } while (!Input.GetUserConfirmation());

            tablePiece.PlayerId = playerId;
            return tablePiece;
        }

        private string GetUserSelectedPieceKey(int playerId)
        {
            string keyInput = null;

            while (string.IsNullOrEmpty(keyInput) || keyInput.Length != 1 || !char.IsLetter(keyInput[0]))
            {
                _console.Write($"\nPlayer {playerId + 1}, enter a key to select your board piece: ");
                keyInput = _console.ReadKey().ToUpper();
                _console.Clear();
            }

            return keyInput;
        }

        private ConsoleColor GetUserSelectedColor(int playerId)
        {
            _console.Write($"\n Player {playerId + 1}, select a color for your board piece: ");
            List<string> menuChoices = Helpers.StringHelper.CreateStringList("Red", "Blue", "Green", "Yellow", "Orange", "Purple", "Magenta", "White");
            int index = MenuOptionSelector.GetSelectedOption(menuChoices, 0, (menuChoices.Count / 2));
            return GetConsoleColorFromIndex(index);
        }

        private static ConsoleColor GetConsoleColorFromIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return ConsoleColor.Red;
                case 1:
                    return ConsoleColor.Blue;
                case 2:
                    return ConsoleColor.Green;
                case 3:
                    return ConsoleColor.Yellow;
                case 4:
                    return ConsoleColor.DarkYellow; // Approximating Orange
                case 5:
                    return ConsoleColor.DarkMagenta; // Approximating Purple
                case 6:
                    return ConsoleColor.Magenta;
                case 7:
                    return ConsoleColor.White;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), "Index must be between 0 and 7 inclusive.");
            }
        }
    }
}
