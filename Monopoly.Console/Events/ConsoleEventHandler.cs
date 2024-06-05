using Monopoly.Console.GUI;
using Monopoly.Core;
using Monopoly.Core.Events;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models.FortuneCard;
using System.Numerics;
using System.Reflection;

namespace Monopoly.Console.Events
{
    internal static class ConsoleEventHandler
    {
        private static ConsoleGame CurrentConsoleGame;

        public static void SubscribeToEvents(ConsoleGame consoleGame)
        {
            CurrentConsoleGame = consoleGame;
            GameEvents.AskPlayerToBuyPurchasableSquareEvent += HandleAskIfPlayerWantsBuyPurchasableSquare;
            GameEvents.AskPlayerToBuyOutOfJailEvent += HandleAskPlayerToBuyOutOfJail;
            GameEvents.PlayerInsufficientFundsEvent += HandlePlayerInsufficientFunds;
            GameEvents.LogAddedEvent += LogAdded;
            GameEvents.ChanceCardDrawnEvent += DrawChanceCard;
            GameEvents.CommunityChestCardDrawnEvent += DrawCommunityChestCard;
            GameEvents.OpenPlayerActionMenuEvent += OpenPlayerActionMenu;
            GameEvents.LandOnSquareEvent += LandOnSquare;
            GameEvents.UpdateGameBoard += UpdateGameBoard;
            GameEvents.UpdatePlayerInformation += UpdatePlayerInformation;
        }

        private static bool HandleAskIfPlayerWantsBuyPurchasableSquare(object sender, SquareEventArgs e)
        {
            Square square = e.Square;
            string message = $"Do you want to buy {square.Name} for {square.Price}{CurrentConsoleGame.CurrentGame.Rules.CurrencySymbol}?";
            CurrentConsoleGame.Printer.PrintText(message);
            return CurrentConsoleGame.PlayerInput.GetUserConfirmation();
        }

        private static bool HandleAskPlayerToBuyOutOfJail(object sender, PlayerEventArgs e)
        {
            Player player = e.Player;
            string message = $"{player.Name} do you want to buy yourself out from prison for 50{CurrentConsoleGame.CurrentGame.Rules.CurrencySymbol}?";
            CurrentConsoleGame.Printer.PrintText(message);
            return CurrentConsoleGame.PlayerInput.GetUserConfirmation();
        }

        private static void HandlePlayerInsufficientFunds(object sender, PlayerEventArgs e)
        {
            Player player = e.Player;
            int targetSum = e.TargetSum;
            while (player.Money < targetSum)
            {
                string message = $"{player.Name} you dont have enough money, you need {targetSum}{CurrentConsoleGame.CurrentGame.Rules.CurrencySymbol}";
                CurrentConsoleGame.Printer.PrintText(message);
                new PlayerActionMenu(CurrentConsoleGame.CurrentGame, player).DisplayPlayerActionRealEstateMenu(true);
            }
        }

        private static void LogAdded(object sender, EventArgs e)
        {
            // Print the newest logs when a new log is added
            CurrentConsoleGame.LogPrinter.PrintNewestLogs(10, CurrentConsoleGame.CurrentGame.Logs.LogList);
        }

        private static void DrawChanceCard(object sender, DrawChanceCardArgs e)
        {
            int position = CurrentConsoleGame.CurrentGame.CurrentPlayer.Position;
            CurrentConsoleGame.CardPrinter.PrepareAndPrintSquareCard(position, e.ChanceCard);
        }

        private static void DrawCommunityChestCard(object sender, DrawCommunityChestCardArgs e)
        {
            int position = CurrentConsoleGame.CurrentGame.CurrentPlayer.Position;
            CurrentConsoleGame.CardPrinter.PrepareAndPrintSquareCard(position, null, e.CommunityChestCard);
        }

        private static void OpenPlayerActionMenu(object sender, EventArgs e)
        {
            PlayerActionMenu PlayerActionMenu = new PlayerActionMenu(CurrentConsoleGame.CurrentGame, CurrentConsoleGame.CurrentGame.CurrentPlayer);
            PlayerActionMenu.DisplayPlayerActionMainMenu();
        }

        private static void LandOnSquare(object sender, SquareEventArgs e)
        {
            Square square = e.Square;
            CurrentConsoleGame.CardPrinter.PrepareAndPrintSquareCard(square.Position);
        }

        private static void UpdateGameBoard(object sender, EventArgs e)
        {
            CurrentConsoleGame.Printer.PrintGameBoard(CurrentConsoleGame.TablePieces, CurrentConsoleGame.CurrentGame.Players);
        }

        private static void UpdatePlayerInformation(object sender, EventArgs e)
        {
            CurrentConsoleGame.Printer.DisplayPlayersInformation(CurrentConsoleGame.CurrentGame.CurrentPlayer, CurrentConsoleGame.CurrentGame.Players);
        }
    }
}
