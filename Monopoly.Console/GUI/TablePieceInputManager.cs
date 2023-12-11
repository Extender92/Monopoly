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
        private IConsoleWrapper Console = new ConsoleWrapper();

        internal TablePiece GetTablePieceFromUserInput(int playerId, List<TablePiece> tablePieces)
        {
            TablePiece tablePiece = new TablePiece();
            do
            {
                do
                {
                    tablePiece.Piece = GetUserSelectedPieceKey(playerId, tablePieces);
                    Console.Write(" You entered: " + tablePiece.Piece);

                    //_console.Write("\n Do you want to continue?");
                } while (/*!Input.GetUserConfirmation()*/ false);

                do
                {
                    tablePiece.Color = GetUserSelectedColor(playerId, tablePieces);

                    //_console.Write("\n You selected color: ");
                    //ConsolePrinter.PrintColoredText(tablePiece.Color.ToString(), tablePiece.Color);

                    //_console.WriteLine("\n Do you want to continue?");
                } while (/*!Input.GetUserConfirmation()*/ false);

                Console.Write(" You entered: ");
                ConsolePrinter.PrintColoredText(tablePiece.Piece, tablePiece.Color);
                Console.Write(" With color: ");
                ConsolePrinter.PrintColoredText(tablePiece.Color.ToString(), tablePiece.Color);
                Console.WriteLine("\n Do you want to continue?");

            } while (!Input.GetUserConfirmation());

            tablePiece.PlayerId = playerId;
            return tablePiece;
        }

        private string GetUserSelectedPieceKey(int playerId, List<TablePiece> tablePieces)
        {
            string keyInput = null;

            while (string.IsNullOrEmpty(keyInput) || keyInput.Length != 1 || !char.IsLetter(keyInput[0]) || IsKeyInUse(keyInput, tablePieces))
            {
                Console.Write($"\nPlayer {playerId + 1}, enter a key to select your board piece: ");
                keyInput = Console.ReadKey().ToUpper();
                Console.Clear();
            }

            return keyInput;
        }

        private bool IsKeyInUse(string key, List<TablePiece> tablePieces)
        {
            // Check if the key is the same as any existing piece key in the tablePieces
            return tablePieces.Any(piece => piece.Piece != null && piece.Piece.Equals(key, StringComparison.OrdinalIgnoreCase));
        }


        private ConsoleColor GetUserSelectedColor(int playerId, List<TablePiece> tablePieces)
        {
            Console.Write($"\n Player {playerId + 1}, select a color for your board piece: ");

            List<ConsoleColor> colors = GetConsoleColors(tablePieces.Select(piece => piece.Color).ToList());

            List<string> menuChoices = colors.Select(x => x.ToString()).ToList();

            int index = MenuOptionSelector.GetSelectedOption(menuChoices, menuChoices.Max(s => s.Length), 0, (menuChoices.Count / 2));
            return colors[index];
        }

        private List<ConsoleColor> GetConsoleColors(List<ConsoleColor> pickedColors = null)
        {
            List<ConsoleColor> colors = new List<ConsoleColor>
            {
                ConsoleColor.Red,
                ConsoleColor.Blue,
                ConsoleColor.Green,
                ConsoleColor.Yellow,
                ConsoleColor.DarkYellow,
                ConsoleColor.DarkMagenta,
                ConsoleColor.Magenta,
                ConsoleColor.White
            };

            // Remove picked colors from the list if pickedColors is not null
            if (pickedColors != null)
            {
                colors.RemoveAll(color => pickedColors.Contains(color));
            }

            return colors;
        }
    }
}
