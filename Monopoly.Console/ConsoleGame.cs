using Monopoly.Console.Events;
using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;

namespace Monopoly.Console;

internal class ConsoleGame
{
    internal readonly Game CurrentGame;
    internal readonly ConsolePrinter Printer;
    internal readonly ConsoleLogPrinter LogPrinter;
    internal readonly ConsoleCardPrinter CardPrinter;
    internal readonly List<TablePiece> TablePieces;
    internal readonly Input PlayerInput;

    internal bool StartedGame { get; private set; }

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

        try
        {
            System.Console.Clear();
            Printer.PrintGameBoard(TablePieces, CurrentGame.Players);

            while (StartedGame && !CurrentGame.IsGameOver)
            {
                Player player = CurrentGame.CurrentPlayer;
                Printer.StartPlayerTurnInfo(player, CurrentGame.Players);

                PlayerActionMenu playerActionMenu = new(CurrentGame, player);
                playerActionMenu.DisplayPlayerActionMainMenu();

                if (playerActionMenu.LastTurnResult is TurnResult result)
                {
                    UpdateGameInformation(result.LandedSquare ?? CurrentGame.Board.GetSquareAtPosition(player.Position), player);
                    Printer.EndPlayerTurnInfo(player, CurrentGame.Players);
                }
            }

            if (CurrentGame.Winner is Player winner)
            {
                Printer.PrintTextWaitForInput($"{winner.Name} wins the game! Press Enter to continue.");
            }
        }
        finally
        {
            ConsoleEventHandler.UnsubscribeFromEvents(this);
            StartedGame = false;
        }
    }

    [Obsolete("Use StartConsoleGame instead.")]
    internal void StartGame() => StartConsoleGame();

    internal void UpdateGameInformation(Square landedSquare, Player player)
    {
        Printer.PrintGameBoard(TablePieces, CurrentGame.Players);
        Printer.DisplayPlayersInformation(player, CurrentGame.Players);
        CardPrinter.PrepareAndPrintSquareCard(landedSquare.Position);
    }
}
