using Monopoly.Console.Models;
using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class ConsolePrinter
    {
        private const int TotalSides = 4;
        private const int SideLength = 10;
        private const int HorizontalBuffer = 3;

        public ConsolePrinter(ConsolePositions positions)
        {
            Positions = positions;
        }

        public ConsolePositions Positions { get; set; }

        private static int BoardPosX { get; set; }
        private static int BoardPosY { get; set; }
        private static int LogPosX { get; set; }
        private static int LogPosY { get; set; }
        private static int TextPosX { get; set; }
        private static int TextPosY { get; set; }
        private static int CardPosX { get; set; }
        private static int CardPosY { get; set; }

        internal void SetBoardPosition()
        {
            BoardPosX = Positions.BoardXPos;
            BoardPosY = Positions.BoardYPos;
        }

        internal void SetTextPosition()
        {
            TextPosX = Positions.TextPosX;
            TextPosY = Positions.TextPosY;
        }

        internal void SetCardPosition()
        {
            CardPosX = Positions.CardPosX;
            CardPosY = Positions.CardPosY;
        }

        internal void SetLogPosition()
        {
            LogPosX = Positions.LogPosX;
            LogPosY = Positions.LogPosY;
        }

        private static IConsoleWrapper Console = new ConsoleWrapper();

        internal static void PrintGameBoard(List<Player> players, List<TablePiece> tablePieces)
        {
            int playerBuffer = players.Count / 2;
            int startPosition = 0;

            for (int side = 0; side < TotalSides; side++)
            {
                PrintSingleSide(playerBuffer, side, startPosition, players, tablePieces);
                startPosition += SideLength;
            }
        }

        private static void PrintSingleSide(int playerBuffer, int side, int startSidePosition, List<Player> players, List<TablePiece> tablePieces)
        {
            int x, y;

            if (side < 0 || side >= TotalSides)
            {
                throw new ArgumentOutOfRangeException(nameof(side), $"Invalid value for 'side': {side}");
            }

            for (int i = 0; i < SideLength; i++)
            {
                var playersOnCurrentPosition = players.Where(player => player.Position == startSidePosition + i).ToList();

                GetPositionCoordinates(side, i, playerBuffer, out x, out y);
                Console.SetPosition(x, y);
                PrintPositionContent(playersOnCurrentPosition, tablePieces);
            }
        }

        private static void GetPositionCoordinates(int side, int positionIndex, int playerBuffer, out int x, out int y)
        {
            x = BoardPosX;
            y = BoardPosY;

            switch (side)
            {
                case 0: // Bottom border
                    positionIndex = SideLength - positionIndex;
                    x += (positionIndex * HorizontalBuffer) + (playerBuffer * positionIndex);
                    y += SideLength;
                    break;

                case 1: // Left Border
                    y += SideLength - positionIndex;
                    break;

                case 2: // Top border
                    x += (positionIndex * HorizontalBuffer) + (playerBuffer * positionIndex);
                    break;

                case 3: // Right border
                    x += (SideLength * HorizontalBuffer) + (playerBuffer * SideLength);
                    y += positionIndex;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(side), $"Invalid value for 'side': {side}");
            }
        }

        private static void PrintPositionContent(List<Player> playersOnCurrentPosition, List<TablePiece> tablePieces)
        {
            Console.Write("[");
            Console.Write(playersOnCurrentPosition.Count > 0 ? "" : " ");
            PrintPlayers(playersOnCurrentPosition, tablePieces);
            Console.Write("]");
        }

        private static void PrintPlayers(List<Player> players, List<TablePiece> tablePieces)
        {
            foreach (Player player in players)
            {
                TablePiece piece = tablePieces.First(x => x.PlayerId == player.Id);
                PrintColoredText(piece.Piece, piece.Color);
            }
        }

        internal static void PrintCard(string header, int width, int height, List<string> info, ConsoleColor borderColor, ConsoleColor textColor)
        {
            // Header Text
            // Row two
            Console.SetTextColor(textColor);
            Console.SetPosition(CardPosX + 1, CardPosY + 1);
            Console.Write(header);

            // Header Border
            // Row one
            Console.SetTextColor(borderColor);
            Console.SetPosition(CardPosX, CardPosY);
            Console.Write('┌' + new String('─', width) + '┐');

            // Header Border
            // Row two
            Console.SetPosition(CardPosX, CardPosY + 1);
            Console.Write("│");
            Console.SetPosition((CardPosX + width + 1), CardPosY + 1);
            Console.Write("│");

            // Header Border
            // Row Three
            Console.SetPosition(CardPosX, CardPosY + 2);
            Console.Write('├' + new String('─', width) + '┤');

            // Body
            // For each row in Y length
            for (int i = 0; i < height; i++)
            {
                Console.SetPosition(CardPosX, CardPosY + 3 + i);
                Console.Write("│");

                Console.SetTextColor(textColor);
                Console.Write(i >= info.Count ?
                    new String(' ', width) :
                    " " + info[i]);

                Console.SetTextColor(borderColor);
                Console.SetPosition((CardPosX + width + 1), CardPosY + 3 + i);
                Console.Write("│");
            }

            // Footer
            Console.SetPosition(CardPosX, CardPosY + (height + 2));
            Console.Write('└' + new String('─', width) + '┘');
            Console.ResetColor();
        }

        internal static void PrintColoredText(string text, ConsoleColor color)
        {
            Console.SetTextColor(color);
            Console.Write(text);
            Console.ResetColor();
        }

        internal static void PrintText(string text)
        {
            Console.ResetColor();
            Console.SetPosition(1, 0);
            Console.Write(text);
        }

        internal static void DisplayPlayerTurnAndWaitForInput(Player player)
        {
            Console.SetPosition(1, 0);
            Console.WriteLine($"{player.Name}'s Turn\n Press Enter To Roll Dice");
            Console.ReadLine();
        }

        internal static void PrintLogs(string header, List<string> logs, ConsoleColor borderColor, ConsoleColor textColor)
        {
            int width = logs.Max(x => x.Length) + 2;
            if (width < header.Length + 2) width = header.Length + 4;

            // Border and Header Text
            Console.SetTextColor(borderColor);
            Console.SetPosition(LogPosX, LogPosY);
            Console.Write("┌─ " + header + " " + new String('─', width - header.Length - 3) + '┐');

            // Body
            for (int i = 0; i < logs.Count; i++)
            {
                Console.SetPosition(LogPosX, LogPosY + 1 + i);
                Console.Write("│");

                Console.SetTextColor(textColor);
                Console.Write(" " + logs[i] + " ");

                Console.SetTextColor(borderColor);
                Console.SetPosition((LogPosX + width + 1), LogPosY + 1 + i);
                Console.Write("│");
            }

            // Footer
            Console.SetPosition(LogPosX, LogPosY + (logs.Count));
            Console.Write('└' + new String('─', width) + '┘');
            Console.ResetColor();
        }
    }
}
