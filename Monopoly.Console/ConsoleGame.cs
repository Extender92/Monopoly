using Monopoly.Console.Events;
using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Persistence;

namespace Monopoly.Console;

internal class ConsoleGame
{
    internal readonly Game CurrentGame;
    internal readonly ConsolePrinter Printer;
    internal readonly ConsoleLogPrinter LogPrinter;
    internal readonly ConsoleCardPrinter CardPrinter;
    internal readonly List<TablePiece> TablePieces;
    internal readonly Input PlayerInput;
    internal readonly IGameSaveStore SaveStore;
    internal readonly ConsolePlayerDecisionProvider DecisionProvider;

    internal bool StartedGame { get; private set; }

    public ConsoleGame(
        Game game,
        ConsolePrinter consolePrinter,
        List<TablePiece> tablePieces,
        Input input,
        ConsoleLogPrinter logPrinter,
        ConsoleCardPrinter cardPrinter,
        IGameSaveStore saveStore,
        ConsolePlayerDecisionProvider decisionProvider)
    {
        CurrentGame = game;
        Printer = consolePrinter;
        TablePieces = tablePieces;
        PlayerInput = input;
        LogPrinter = logPrinter;
        CardPrinter = cardPrinter;
        SaveStore = saveStore;
        DecisionProvider = decisionProvider ?? throw new ArgumentNullException(nameof(decisionProvider));
    }

    internal void StartConsoleGame()
    {
        StartedGame = true;
        ConsolePositions.SetGameBoardMenuPositions();
        using IDisposable notificationSubscription = ConsoleEventHandler.Subscribe(this);

        try
        {
            System.Console.Clear();
            Printer.PrintGameBoard(TablePieces, CurrentGame.Players);

            while (StartedGame &&
                   CurrentGame.Phase != GamePhase.GameOver &&
                   !CurrentGame.IsGameOver)
            {
                Player player = CurrentGame.CurrentPlayer;
                Printer.StartPlayerTurnInfo(player, CurrentGame.Players);

                PlayerActionMenu playerActionMenu = CreatePlayerActionMenu(player);
                playerActionMenu.DisplayPlayerActionMainMenu();

                if (playerActionMenu.LastActionResult is GameActionResult actionResult)
                {
                    actionResult = ResolvePendingDecisions(actionResult);
                    if (actionResult.Status == GameActionStatus.Rejected)
                        continue;

                    TurnResult result = actionResult.TurnResult
                        ?? throw new InvalidOperationException("A completed game action must contain a turn result.");
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

    internal PlayerActionMenu CreatePlayerActionMenu(Player player) =>
        new(CurrentGame, player, SaveStore);

    internal GameActionResult ResolvePendingDecisions(GameActionResult result)
    {
        while (true)
        {
            if (result.Status == GameActionStatus.Rejected)
            {
                Printer.PrintText($"The decision was rejected: {result.RejectionReason}.");
                if (CurrentGame.Phase != GamePhase.AwaitingDecision)
                    return result;
            }
            else if (result.Status != GameActionStatus.DecisionRequired)
            {
                return result;
            }

            PendingDecision decision = result.PendingDecision ?? CurrentGame.PendingDecision
                ?? throw new InvalidOperationException("A required decision must include its snapshot.");
            result = CurrentGame.SubmitDecision(DecisionProvider.GetResponse(decision));
        }
    }
}
