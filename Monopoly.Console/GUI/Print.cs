using Monopoly.Console.Models;
using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class Print
    {
        private static IConsoleWrapper Console = new ConsoleWrapper();

        internal static void PrintBoard(List<Player> players, List<TablePiece> tablePieces)
        {
            int length = 10;
            int borderBuffer = 5;
            int horizontalBuffer = 3;
            int correctPosition = 0;
            int playerBuffer = players.Count / 2;

            int x = 0;
            int y = 0;

            // Left Border
            for (int i = length; i > 0; i--)
            {
                x = borderBuffer;
                y = borderBuffer + i;
                Console.SetPosition(x,y);

                var playersOnCurrentPosition = players.Where(player => player.Position == correctPosition).ToList();
                if (playersOnCurrentPosition.Count > 0) PrintPlayers(playersOnCurrentPosition, tablePieces);
                else Console.Write("[ ]");

                correctPosition++;
            }
            // Top border
            for (int i = 0; i < length; i++)
            {
                x = borderBuffer + (i * horizontalBuffer) + (playerBuffer * i);
                y = borderBuffer;
                Console.SetPosition(x,y);

                var playersOnCurrentPosition = players.Where(player => player.Position == correctPosition).ToList();
                if (playersOnCurrentPosition.Count > 0) PrintPlayers(playersOnCurrentPosition, tablePieces);
                else Console.Write("[ ]");

                correctPosition++;
            }
            // Right border
            for (int i = 0; i < length; i++)
            {
                x = borderBuffer + (length * horizontalBuffer) + (playerBuffer * length);
                y = borderBuffer + i;
                Console.SetPosition(x,y);

                var playersOnCurrentPosition = players.Where(player => player.Position == correctPosition).ToList();
                if (playersOnCurrentPosition.Count > 0) PrintPlayers(playersOnCurrentPosition, tablePieces);
                else Console.Write("[ ]");

                correctPosition++;
            }
            // Bottom border
            for (int i = length; i > 0; i--)
            {
                x = borderBuffer + (i * horizontalBuffer) + (playerBuffer * i);
                y = borderBuffer + length;
                Console.SetPosition(x,y);

                var playersOnCurrentPosition = players.Where(player => player.Position == correctPosition).ToList();
                if (playersOnCurrentPosition.Count > 0) PrintPlayers(playersOnCurrentPosition, tablePieces);
                else Console.Write("[ ]");

                correctPosition++;
            }
        }

        private static void PrintPlayers(List<Player> players, List<TablePiece> tablePieces)
        {
            Console.Write("[");
            foreach (Player player in players)
            {
                TablePiece piece = tablePieces.First(x => x.PlayerId == player.Id);
                Console.SetTextColor(piece.Color);
                Console.Write(piece.Piece);
            }
            Console.ResetColor();
            Console.Write("]");
        }

        internal static void PrintCard(string header, int posX, int PosY, int lengthX, int lengthY, List<string> info, ConsoleColor borderColor, ConsoleColor textColor)
        {
            // Header Text
            // Row two
            Console.SetTextColor(textColor);
            Console.SetPosition(posX + 1, PosY + 1);
            Console.Write(header);

            // Header Border
            // Row one
            Console.SetTextColor(borderColor);
            Console.SetPosition(posX, PosY);
            Console.Write('┌' + new String('─', lengthX) + '┐');

            // Row two
            Console.SetPosition(posX, PosY + 1);
            Console.Write("│");
            Console.SetPosition((posX + lengthX + 1), PosY + 1);
            Console.Write("│");

            // Row Three
            Console.SetPosition(posX, PosY + 2);
            Console.Write('├' + new String('─', lengthX) + '┤');

            // Body
            // For each row in Y length
            for (int i = 0; i < lengthY; i++)
            {
                Console.SetPosition(posX, PosY + 3 + i);
                Console.Write("│");

                Console.SetTextColor(textColor);
                Console.Write(i >= info.Count ?
                    new String(' ', lengthX) :
                    " " + info[i]);

                Console.SetTextColor(borderColor);
                Console.SetPosition((posX + lengthX + 1), PosY + 3 + i);
                Console.Write("│");
            }

            // Footer
            Console.SetPosition(posX, PosY + (lengthY + 2));
            Console.Write('└' + new String('─', lengthX) + '┘');
            Console.ResetColor();
        }
    }
}
