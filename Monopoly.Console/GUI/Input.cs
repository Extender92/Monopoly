using Monopoly.Console.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class Input
    {
        private static IConsoleWrapper Console = new ConsoleWrapper();

        internal static TablePiece ChooseTablePiece(int playerId)
        {
            TablePiece tablePiece = new TablePiece();
            do
            {
                do
                {
                    tablePiece.Piece = null;
                    while (tablePiece.Piece == null || tablePiece.Piece.Count() < 1)
                    {
                        Console.Write($"\n Player {playerId + 1} enter a Key to select your board piece: ");
                        string input = Console.ReadKey();
                        if (char.IsLetter(char.Parse(input))) tablePiece.Piece = input.ToUpper();
                        Console.Clear();
                    }

                    Console.Write("\n You entered: " + tablePiece.Piece);

                } while (!UserConfirm());
                do
                {
                    tablePiece.Color = ConsoleColor.Red;
                    if (playerId == 1)
                    {
                        tablePiece.Color = ConsoleColor.Blue;
                    }
                    else if (playerId == 2)
                    {
                        tablePiece.Color = ConsoleColor.Green;
                    }

                    Console.Write("\n You selected color: " );
                    Print.PrintColoredText(tablePiece.Color.ToString(), tablePiece.Color);

                } while (!UserConfirm());

                Console.Write("\n You entered: ");
                Print.PrintColoredText(tablePiece.Piece, tablePiece.Color);
                Console.Write(" With color: ");
                Print.PrintColoredText(tablePiece.Color.ToString(), tablePiece.Color);

            } while (!UserConfirm());

            tablePiece.PlayerId = playerId;
            return tablePiece;
        }


        internal static bool UserConfirm()
        {
            Console.WriteLine("\n\n Are You Sure?");
            Console.ReadLine();
            Console.Clear();
            return true;
        }
    }
}
