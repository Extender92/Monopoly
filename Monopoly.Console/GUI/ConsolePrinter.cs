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

        internal IConsoleWrapper Console {  get; set; }
        private List<Square> _squares;
        private GameRules _rules;

        public ConsolePrinter(IConsoleWrapper consoleWrapper, List<Square> squares, GameRules rules)
        {
            Console = consoleWrapper;
            _squares = squares;
            _rules = rules;
            InitializePositions();
        }

        private int BoardPosX { get; set; }
        private int BoardPosY { get; set; }
        private int TextPosX { get; set; }
        private int TextPosY { get; set; }
        public int PlayerInformationX { get; set; }
        public int PlayerInformationY { get; set; }

        internal void InitializePositions()
        {
            SetBoardPosition();
            SetTextPosition();
            SetPlayerInformationPosition();
        }

        internal void SetBoardPosition()
        {
            BoardPosX = ConsolePositions.BoardPosX;
            BoardPosY = ConsolePositions.BoardPosY;
        }

        internal void SetTextPosition()
        {
            TextPosX = ConsolePositions.TextPosX;
            TextPosY = ConsolePositions.TextPosY;
        }

        internal void SetPlayerInformationPosition()
        {
            PlayerInformationX = ConsolePositions.PlayerInformationX;
            PlayerInformationY = ConsolePositions.PlayerInformationY;
        }

        internal void PrintGameBoard(List<TablePiece> tablePieces, List<Player> players)
        {
            int playerBuffer = players.Count / 2;
            int startPosition = 0;

            for (int side = 0; side < TotalSides; side++)
            {
                PrintSingleSide(playerBuffer, side, startPosition, players, tablePieces);
                startPosition += SideLength;
            }
        }

        internal void PrintSingleSide(int playerBuffer, int side, int startSidePosition, List<Player> players, List<TablePiece> tablePieces)
        {
            int x, y;

            if (side < 0 || side >= TotalSides)
            {
                throw new ArgumentOutOfRangeException(nameof(side), $"Invalid value for 'side': {side}");
            }

            for (int i = 0; i < SideLength; i++)
            {
                var currentSquare = _squares.First(s => s.Position == startSidePosition + i);
                var playersOnCurrentPosition = players.Where(player => player.Position == currentSquare.Position).ToList();

                ConsoleColor ownerColor = currentSquare.Owner?.Id != null
                    ? tablePieces.FirstOrDefault(t => t.PlayerId == currentSquare.Owner.Id)?.Color ?? ConsoleColor.White
                    : ConsoleColor.White;


                GetPositionCoordinates(side, i, playerBuffer, out x, out y);
                Console.SetPosition(x, y);
                PrintPositionContent(playersOnCurrentPosition, tablePieces, ownerColor);
            }
        }

        internal void GetPositionCoordinates(int side, int positionIndex, int playerBuffer, out int x, out int y)
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

        internal void PrintPositionContent(List<Player> playersOnCurrentPosition, List<TablePiece> tablePieces, ConsoleColor ownerColor = ConsoleColor.White)
        {
            string firstPart = "[";
            firstPart += playersOnCurrentPosition.Count > 0 ? "" : " ";
            PrintColoredText(firstPart, ownerColor);
            PrintPlayers(playersOnCurrentPosition, tablePieces);
            PrintColoredText("]", ownerColor);
        }

        internal void PrintPlayers(List<Player> players, List<TablePiece> tablePieces)
        {
            foreach (Player player in players)
            {
                TablePiece piece = tablePieces.First(x => x.PlayerId == player.Id);
                PrintColoredText(piece.Piece, piece.Color);
            }
        }

        internal void PrintColoredText(string text, ConsoleColor color)
        {
            Console.SetTextColor(color);
            Console.Write(text);
            Console.ResetColor();
        }

        internal void PrintText(string text)
        {
            Console.ResetColor();
            Console.SetPosition(TextPosX, TextPosY);
            Console.Write(text);
        }

        internal void PrintTextWaitForInput(string s)
        {
            PrintText(s);
            Console.ReadLine();
        }

        internal void EndPlayerTurnInfo(Player player, List<Player> players)
        {
            DisplayPlayersInformation(player, players);
            PrintTextWaitForInput($"{player.Name}'s Turn.\n Press Enter To End Turn");
        }

        internal void StartPlayerTurnInfo(Player player, List<Player> players)
        {
            DisplayPlayersInformation(player, players);
            PrintTextWaitForInput($"{player.Name}'s Turn.\n Press Enter To Continue");
        }

        internal void DisplayPlayersInformation(Player player, List<Player> players)
        {
            for (int i = 0; i < players.Count; i++)
            {
                Console.SetPosition(PlayerInformationX, PlayerInformationY + i);
                if (players[i] == player)
                {
                    Console.SetTextColor(ConsoleColor.Yellow);
                }
                Console.Write($"{players[i].Name} Money: {players[i].Money}{_rules.CurrencySymbol}");
                Console.ResetColor();
            }
        }
    }
}
