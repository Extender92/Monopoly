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
                System.Console.SetCursorPosition(x,y);

                var playersOnCurrentPosition = players.Where(player => player.Position == correctPosition).ToList();
                if (playersOnCurrentPosition.Count > 0) PrintPlayers(playersOnCurrentPosition, tablePieces);
                else System.Console.Write("[ ]");

                correctPosition++;
            }
            // Top border
            for (int i = 0; i < length; i++)
            {
                x = borderBuffer + (i * horizontalBuffer) + (playerBuffer * i);
                y = borderBuffer;
                System.Console.SetCursorPosition(x,y);

                var playersOnCurrentPosition = players.Where(player => player.Position == correctPosition).ToList();
                if (playersOnCurrentPosition.Count > 0) PrintPlayers(playersOnCurrentPosition, tablePieces);
                else System.Console.Write("[ ]");

                correctPosition++;
            }
            // Right border
            for (int i = 0; i < length; i++)
            {
                x = borderBuffer + (length * horizontalBuffer) + (playerBuffer * length);
                y = borderBuffer + i;
                System.Console.SetCursorPosition(x,y);

                var playersOnCurrentPosition = players.Where(player => player.Position == correctPosition).ToList();
                if (playersOnCurrentPosition.Count > 0) PrintPlayers(playersOnCurrentPosition, tablePieces);
                else System.Console.Write("[ ]");

                correctPosition++;
            }
            // Bottom border
            for (int i = length; i > 0; i--)
            {
                x = borderBuffer + (i * horizontalBuffer) + (playerBuffer * i);
                y = borderBuffer + length;
                System.Console.SetCursorPosition(x,y);

                var playersOnCurrentPosition = players.Where(player => player.Position == correctPosition).ToList();
                if (playersOnCurrentPosition.Count > 0) PrintPlayers(playersOnCurrentPosition, tablePieces);
                else System.Console.Write("[ ]");

                correctPosition++;
            }
        }

        private static void PrintPlayers(List<Player> players, List<TablePiece> tablePieces)
        {
            System.Console.Write("[");
            foreach (Player player in players)
            {
                TablePiece piece = tablePieces.First(x => x.PlayerId == player.Id);
                System.Console.ForegroundColor = piece.Color;
                System.Console.Write(piece.Piece);
            }
            System.Console.ResetColor();
            System.Console.Write("]");
        }

        internal static void PrintCard(string header, int posX, int PosY, int lengthX, int lengthY, List<string> info, ConsoleColor borderColor, ConsoleColor textColor)
        {
            // Header Text
            // Row two
            System.Console.ForegroundColor = textColor;
            System.Console.SetCursorPosition(posX + 1, PosY + 1);
            System.Console.Write(header);

            // Header Border
            // Row one
            System.Console.ForegroundColor = borderColor;
            System.Console.SetCursorPosition(posX, PosY);
            System.Console.Write('┌' + new String('─', lengthX) + '┐');

            // Row two
            System.Console.SetCursorPosition(posX, PosY + 1);
            System.Console.Write('│');
            System.Console.SetCursorPosition((posX + lengthX + 1), PosY + 1);
            System.Console.Write('│');

            // Row Three
            System.Console.SetCursorPosition(posX, PosY + 2);
            System.Console.Write('├' + new String('─', lengthX) + '┤');

            // Body
            // For each row in Y length
            for (int i = 0; i < lengthY; i++)
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
            System.Console.SetCursorPosition(posX, PosY + (lengthY + 2));
            System.Console.Write('└' + new String('─', lengthX) + '┘');
            System.Console.ResetColor();
        }
    }
}
