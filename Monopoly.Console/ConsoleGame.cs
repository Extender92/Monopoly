using Monopoly.Console.Events;
using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console
{
    internal class ConsoleGame
    {
        internal readonly Game CurrentGame;
        internal readonly ConsolePrinter Printer;
        internal readonly ConsoleLogPrinter LogPrinter;
        internal readonly ConsoleCardPrinter CardPrinter;
        internal readonly List<TablePiece> TablePieces;
        internal readonly Input PlayerInput;

        internal bool StartedGame { get; set; }

        public ConsoleGame(Game game, ConsolePrinter consolePrinter, List<TablePiece> tablePieces, Input input, ConsoleLogPrinter logPrinter, ConsoleCardPrinter cardPrinter)
        {
            CurrentGame = game;
            Printer = consolePrinter;
            TablePieces = tablePieces;
            PlayerInput = input;
            LogPrinter = logPrinter;
            CardPrinter = cardPrinter;
        }

        internal void StartConsoleGame()
        {
            StartedGame = true;
            ConsolePositions.SetGameBoardMenuPositions();

            ConsoleEventHandler.SubscribeToEvents(this);

            System.Console.Clear();
            Printer.PrintGameBoard(TablePieces, CurrentGame.Players);
            PlayerActionMenu PlayerActionMenu = new PlayerActionMenu(CurrentGame, CurrentGame.CurrentPlayer);

            while (StartedGame)
            {
                Printer.StartPlayerTurnInfo(CurrentGame.CurrentPlayer, CurrentGame.Players);

                if (CurrentGame.TheJail.IsPlayerInJail(CurrentGame.CurrentPlayer)) CurrentGame.HandlePlayerInJail();

                CurrentGame.PlayerTakeTurn();

                Printer.EndPlayerTurnInfo(CurrentGame.CurrentPlayer, CurrentGame.Players);

                CurrentGame.NextPlayer();
            }
        }


        internal void StartGame()
        {
            ConsolePositions.SetGameBoardMenuPositions();

            ConsoleEventHandler.SubscribeToEvents(this);

            System.Console.Clear();
            Printer.PrintGameBoard(TablePieces, CurrentGame.Players);

            while (CurrentGame.Players.Count() > 1)
            {
                foreach (var player in CurrentGame.Players)
                {
                    CurrentGame.CurrentPlayer = player;
                    PlayerActionMenu PlayerActionMenu = new PlayerActionMenu(CurrentGame, player);
                    do
                    {
                        Printer.StartPlayerTurnInfo(player, CurrentGame.Players);
                        PlayerActionMenu.DisplayPlayerActionMainMenu();

                        if (CurrentGame.TheJail.IsPlayerInJail(player))
                        {
                            CurrentGame.PlayerTakeTurnInJail();
                        }

                        Square landedSquare = CurrentGame.Board.GetSquareAtPosition(player.Position);

                        UpdateGameInformation(landedSquare, player);

                        if (true)
                        {

                            UpdateGameInformation(landedSquare, player);

                            // Check for a double roll
                            if (CurrentGame.Handler.IsDiceDouble())
                            {
                                Printer.PrintText($"{player.Name} rolled a double! Taking another turn.");
                            }
                        }

                        Printer.EndPlayerTurnInfo(player, CurrentGame.Players);
                    } while (CurrentGame.Handler.IsDiceDouble());
                }
            }
        }

        private void UpdateGameInformation(Square landedSquare, Player player)
        {
            Printer.Console.Clear();
            Printer.PrintGameBoard(TablePieces, CurrentGame.Players);
            Printer.DisplayPlayersInformation(player, CurrentGame.Players);
            CardPrinter.PrepareAndPrintSquareCard(landedSquare.Position);
            LogPrinter.PrintNewestLogs(10, CurrentGame.Logs.LogList);
        }
    }
}
