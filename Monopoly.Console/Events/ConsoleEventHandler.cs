using Monopoly.Console.GUI;
using Monopoly.Core;
using Monopoly.Core.Events;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models.FortuneCard;

namespace Monopoly.Console.Events
{
    internal static class ConsoleEventHandler
    {
        private static ConsoleGame? CurrentConsoleGame;

        public static void SubscribeToEvents(ConsoleGame consoleGame)
        {
            if (CurrentConsoleGame is not null)
                UnsubscribeFromEvents(CurrentConsoleGame);

            CurrentConsoleGame = consoleGame;
            GameEvents.LogAddedEvent += LogAdded;
            GameEvents.ChanceCardDrawnEvent += DrawChanceCard;
            GameEvents.CommunityChestCardDrawnEvent += DrawCommunityChestCard;
            GameEvents.OpenPlayerActionMenuEvent += OpenPlayerActionMenu;
            GameEvents.LandOnSquareEvent += LandOnSquare;
            GameEvents.UpdateGameBoard += UpdateGameBoard;
            GameEvents.UpdatePlayerInformation += UpdatePlayerInformation;
        }

        public static void UnsubscribeFromEvents(ConsoleGame consoleGame)
        {
            GameEvents.LogAddedEvent -= LogAdded;
            GameEvents.ChanceCardDrawnEvent -= DrawChanceCard;
            GameEvents.CommunityChestCardDrawnEvent -= DrawCommunityChestCard;
            GameEvents.OpenPlayerActionMenuEvent -= OpenPlayerActionMenu;
            GameEvents.LandOnSquareEvent -= LandOnSquare;
            GameEvents.UpdateGameBoard -= UpdateGameBoard;
            GameEvents.UpdatePlayerInformation -= UpdatePlayerInformation;

            if (ReferenceEquals(CurrentConsoleGame, consoleGame))
                CurrentConsoleGame = null;
        }

        private static void LogAdded(object? sender, EventArgs e)
        {
            if (!IsCurrentGame(sender)) return;
            // Print the newest logs when a new log is added
            CurrentConsoleGame!.LogPrinter.PrintNewestLogs(10, CurrentConsoleGame.CurrentGame.Logs.LogList);
        }

        private static void DrawChanceCard(object? sender, DrawChanceCardArgs e)
        {
            if (!IsCurrentGame(sender)) return;
            int position = CurrentConsoleGame!.CurrentGame.CurrentPlayer.Position;
            CurrentConsoleGame.CardPrinter.PrepareAndPrintSquareCard(position, e.ChanceCard);
        }

        private static void DrawCommunityChestCard(object? sender, DrawCommunityChestCardArgs e)
        {
            if (!IsCurrentGame(sender)) return;
            int position = CurrentConsoleGame!.CurrentGame.CurrentPlayer.Position;
            CurrentConsoleGame!.CardPrinter.PrepareAndPrintSquareCard(position, null, e.CommunityChestCard);
        }

        private static void OpenPlayerActionMenu(object? sender, EventArgs e)
        {
            if (!IsCurrentGame(sender)) return;
            PlayerActionMenu PlayerActionMenu = new PlayerActionMenu(CurrentConsoleGame!.CurrentGame, CurrentConsoleGame.CurrentGame.CurrentPlayer);
            PlayerActionMenu.DisplayPlayerActionMainMenu();
        }

        private static void LandOnSquare(object? sender, SquareEventArgs e)
        {
            if (!IsCurrentGame(sender)) return;
            Square square = e.Square;
            CurrentConsoleGame!.CardPrinter.PrepareAndPrintSquareCard(square.Position);
        }

        private static void UpdateGameBoard(object? sender, EventArgs e)
        {
            if (!IsCurrentGame(sender)) return;
            CurrentConsoleGame!.Printer.PrintGameBoard(CurrentConsoleGame.TablePieces, CurrentConsoleGame.CurrentGame.Players);
        }

        private static void UpdatePlayerInformation(object? sender, EventArgs e)
        {
            if (!IsCurrentGame(sender)) return;
            CurrentConsoleGame!.Printer.DisplayPlayersInformation(CurrentConsoleGame.CurrentGame.CurrentPlayer, CurrentConsoleGame.CurrentGame.Players);
        }

        private static bool IsCurrentGame(object? sender) =>
            sender is Game game && CurrentConsoleGame is not null && ReferenceEquals(game, CurrentConsoleGame.CurrentGame);
    }
}
