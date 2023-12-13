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
        internal Game TheGame;
        internal ConsolePrinter Printer { get; set; }
        internal ConsoleLogPrinter LogPrinter { get; set; }
        internal ConsoleCardPrinter CardPrinter { get; set; }
        internal List<TablePiece> TablePieces { get; set; }
        internal Input PlayerInput { get; set; }

        public ConsoleGame(Game game, ConsolePrinter consolePrinter, List<TablePiece> tablePieces, Input input, ConsoleLogPrinter logPrinter, ConsoleCardPrinter cardPrinter)
        {
            TheGame = game;
            Printer = consolePrinter;
            TablePieces = tablePieces;
            PlayerInput = input;
            LogPrinter = logPrinter;
            CardPrinter = cardPrinter;
        }


        internal void StartGame()
        {
            ConsoleEventHandler.SubscribeToEvents(this);

            System.Console.Clear();
            Printer.PrintGameBoard(TablePieces, TheGame.Players);

            while (TheGame.Players.Count(p => !p.IsBankrupt) > 1)
            {
                foreach (var player in TheGame.Players.Where(p => !p.IsBankrupt))
                {
                    do
                    {
                        Printer.StartPlayerTurnInfo(player, TheGame.Players);

                        if (player.InJail)
                        {
                            TheGame.TheJail.TakeTurnInJail(player);
                        }
                        else
                        {
                            TheGame.Handler.RoleDiceAndMovePlayer(player);
                        }

                        Square landedSquare = TheGame.Board.GetSquareAtPosition(player.Position);

                        UpdateGameInformation(landedSquare, player);

                        if (!player.IsBankrupt)
                        {
                            player.LandOnSquare(landedSquare, TheGame);

                            UpdateGameInformation(landedSquare, player);

                            // Check for a double roll
                            if (TheGame.Handler.IsDiceDouble())
                            {
                                Printer.PrintText($"{player.Name} rolled a double! Taking another turn.");
                            }
                        }

                        Printer.EndPlayerTurnInfo(player, TheGame.Players);
                    } while (TheGame.Handler.IsDiceDouble() && !player.IsBankrupt);
                }
            }
            TheGame.Handler.Winning(TheGame.Players.First(p => !p.IsBankrupt));
        }

        private void UpdateGameInformation(Square landedSquare, Player player)
        {
            Printer.Console.Clear();
            Printer.PrintGameBoard(TablePieces, TheGame.Players);
            Printer.DisplayPlayersInformation(player, TheGame.Players);
            CardPrinter.PrepareAndPrintSquareCard(landedSquare.Position);
            LogPrinter.PrintNewestLogs(10, TheGame.Logs.LogList);
        }
    }
}
