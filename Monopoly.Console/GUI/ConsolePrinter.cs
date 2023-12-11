using Monopoly.Console.Builder;
using Monopoly.Console.Models;
using Monopoly.Console.Models.Board;
using Monopoly.Core;
using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
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

        private static IConsoleWrapper Console = new ConsoleWrapper();

        public ConsolePrinter(ConsolePositions positions)
        {
            Positions = positions;
            InitializePositions();
        }

        public static ConsolePositions Positions { get; set; }

        private static int BoardPosX { get; set; }
        private static int BoardPosY { get; set; }
        private static int LogPosX { get; set; }
        private static int LogPosY { get; set; }
        private static int TextPosX { get; set; }
        private static int TextPosY { get; set; }
        private static int CardPosX { get; set; }
        private static int CardPosY { get; set; }
        public static int PlayerInformationX { get; set; }
        public static int PlayerInformationY { get; set; }

        internal void InitializePositions()
        {
            SetBoardPosition();
            SetTextPosition();
            SetCardPosition();
            SetLogPosition();
            SetPlayerInformationPosition();
        }

        internal void SetBoardPosition()
        {
            BoardPosX = Positions.BoardPosX;
            BoardPosY = Positions.BoardPosY;
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

        internal void SetPlayerInformationPosition()
        {
            PlayerInformationX = Positions.PlayerInformationX;
            PlayerInformationY = Positions.PlayerInformationY;
        }

        internal static void PrintGameBoard(List<TablePiece> tablePieces)
        {
            int playerBuffer = Game.Players.Count / 2;
            int startPosition = 0;

            for (int side = 0; side < TotalSides; side++)
            {
                PrintSingleSide(playerBuffer, side, startPosition, Game.Players, tablePieces);
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
                var currentSquare = Game.Board.Squares.First(s => s.Position == startSidePosition + i);
                var playersOnCurrentPosition = players.Where(player => player.Position == currentSquare.Position).ToList();
                ConsoleColor ownerColor = ConsoleColor.White;
                if (currentSquare is PropertySquare property && property.Owner != null)
                {
                    ownerColor = tablePieces.First(t => t.PlayerId == property.Owner.Id).Color;
                }

                GetPositionCoordinates(side, i, playerBuffer, out x, out y);
                Console.SetPosition(x, y);
                PrintPositionContent(playersOnCurrentPosition, tablePieces, ownerColor);
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

        private static void PrintPositionContent(List<Player> playersOnCurrentPosition, List<TablePiece> tablePieces, ConsoleColor ownerColor = ConsoleColor.White)
        {
            string firstPart = "[";
            firstPart += playersOnCurrentPosition.Count > 0 ? "" : " ";
            PrintColoredText(firstPart, ownerColor);
            PrintPlayers(playersOnCurrentPosition, tablePieces);
            PrintColoredText("]", ownerColor);
        }

        private static void PrintPlayers(List<Player> players, List<TablePiece> tablePieces)
        {
            foreach (Player player in players)
            {
                TablePiece piece = tablePieces.First(x => x.PlayerId == player.Id);
                PrintColoredText(piece.Piece, piece.Color);
            }
        }

        internal static void PrintCard(string header, int width, int maxInfoVerticalLength, List<string> info, ConsoleColor borderColor = ConsoleColor.White, ConsoleColor textColor = ConsoleColor.White)
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
            for (int i = 0; i <= maxInfoVerticalLength; i++)
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
            Console.SetPosition(CardPosX, CardPosY + (maxInfoVerticalLength + 3));
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
            Console.SetPosition(TextPosX, TextPosY);
            Console.Write(text);
        }

        internal static void WaitForInput(Player player)
        {
            DisplayPlayersInformation(player);
            Console.SetPosition(1, 0);
            Console.WriteLine($"{player.Name}'s Turn.\n Press Enter To Continue");
            Console.ReadLine();
        }

        internal static void DisplayPlayersInformation(Player player)
        {
            for (int i = 0; i < Game.Players.Count; i++)
            {
                Console.SetPosition(PlayerInformationX, PlayerInformationY + i);
                if (Game.Players[i] == player)
                {
                    Console.SetTextColor(ConsoleColor.Yellow);
                }
                Console.Write($"{Game.Players[i].Name} Money: {Game.Players[i].Money}{Game.Rules.CurrencySymbol}");
                Console.ResetColor();
            }
        }



        internal static void PrintLogs(string header, List<string> logs, ConsoleColor borderColor = ConsoleColor.White, ConsoleColor textColor = ConsoleColor.White)
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
            Console.SetPosition(LogPosX, LogPosY + (logs.Count + 1));
            Console.Write('└' + new String('─', width) + '┘');
            Console.ResetColor();
        }

        internal static void PrintNewestLogs(int maxAmountOfLogs)
        {
            // Ensure that maxAmountOfLogs is within the range of logList.Count
            int startIndex = Math.Max(0, Game.Logs.LogList.Count - maxAmountOfLogs);

            List<string> logStrings = Game.Logs.LogList.Count > 0
                ? Helpers.StringHelper.CreateStringList(Game.Logs.LogList
                    .Skip(startIndex)
                    .Select(l => l.Info)
                    .ToArray())
                : new List<string> { "Logs is empty" };

            logStrings.Reverse();  // Reverse the order of logStrings

            PrintLogs("Logs", logStrings, ConsoleColor.Green, ConsoleColor.White);
        }


        internal static void PrepareAndPrintSquareCard(int boardPosition)
        {
            List<SquareCard> squareList = SquareCardBuilder.BuildAllSquareCards(Game.Board.Squares, Game.Rules);
            SquareCard squareCard = squareList.First(s => s.BoardPosition == boardPosition);

            int cardHorizontalLength = 30;
            int maxInfoVerticalLength = 9;

            var borderColor = ConsoleColor.White;

            List<string> infoLines = new List<string>();
            List<string> rents = new List<string>();
            int infoTextLength = 0;

            if (squareCard is PropertySquareCard propertySquareCard)
            {
                borderColor = propertySquareCard.BorderColor;
                infoLines.AddRange(propertySquareCard.Prop);
                rents.AddRange(propertySquareCard.Rent);
                infoTextLength = infoLines.Select((line, i) => line.Length + rents[i].Length + 4).Max();
            }
            else if (squareCard is RailroadSquareCard railroadSquareCard)
            {
                infoLines.AddRange(railroadSquareCard.Prop);
                rents.AddRange(railroadSquareCard.Rent);
                infoTextLength = infoLines.Select((line, i) => line.Length + rents[i].Length + 4).Max();
            }


            string header = squareCard.Name;

            cardHorizontalLength = Math.Max(cardHorizontalLength, Math.Max(header.Length + 2, infoTextLength));

            List<string> info = new List<string>();

            for (int i = 0; i < infoLines.Count; i++)
            {
                int space = cardHorizontalLength - (infoLines[i].Length + rents[i].Length + 2);
                info.Add($"{infoLines[i]}:{new string(' ', space)}{rents[i]}");
            }
            info.Add("");

            string infoText = squareCard.Info;
            int length = cardHorizontalLength - 1;

            List<string> stringList = Helpers.StringHelper.CenterStringInList(Helpers.StringHelper.GetListOfStringsFromString(infoText, length), length);
            info.AddRange(stringList);
            header = Helpers.StringHelper.CenterString(header, cardHorizontalLength);

            if (maxInfoVerticalLength < info.Count) maxInfoVerticalLength = info.Count;

            PrintCard(header, cardHorizontalLength, maxInfoVerticalLength, info, borderColor);
        }
    }
}
