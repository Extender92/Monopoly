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
        private static ConsoleGame CurrentGame;

        public static void SubscribeToEvents(ConsoleGame consoleGame)
        {
            CurrentGame = consoleGame;
            GameEvents.PlayerInsufficientFundsEvent += HandlePlayerInsufficientFunds;
            GameEvents.AskPlayerToBuyPurchasableSquareEvent += HandleAskIfPlayerWantsBuyPurchasableSquare;
            GameEvents.AskPlayerToBuyOutOfJailEvent += HandleAskPlayerToBuyOutOfJail;
            GameEvents.LogAddedEvent += LogAdded;
            GameEvents.ChanceCardDrawnEvent += DrawChanceCard;
            GameEvents.CommunityChestCardDrawnEvent += DrawCommunityChestCard;
            GameEvents.OpenPlayerActionMenuEvent += OpenPlayerActionMenu;
            //GameEvents.LandOnSquareEvent += LandOnSquare;
        }

        private static void HandlePlayerInsufficientFunds(object sender, PlayerEventArgs e)
        {
            Player player = e.Player;
            int targetSum = e.TargetSum;
            while (player.Money < targetSum)
            {
                string message = $"{player.Name} you dont have enough money, you need {targetSum}{CurrentGame.CurrentGame.Rules.CurrencySymbol}";
                CurrentGame.Printer.PrintText(message);
                new PlayerActionMenu(CurrentGame.CurrentGame, player).DisplayPlayerActionRealEstateMenu(true);
            }
        }

        private static bool HandleAskIfPlayerWantsBuyPurchasableSquare(object sender, SquareEventArgs e)
        {
            Square square = e.Square;
            string message = $"Do you want to buy {square.Name} for {square.Price}{CurrentGame.CurrentGame.Rules.CurrencySymbol}?";
            CurrentGame.Printer.PrintText(message);
            return CurrentGame.PlayerInput.GetUserConfirmation();
        }

        private static bool HandleAskPlayerToBuyOutOfJail(object sender, PlayerEventArgs e)
        {
            Player player = e.Player;
            string message = $"{player.Name} do you want to buy yourself out from prison for 50{CurrentGame.CurrentGame.Rules.CurrencySymbol}?";
            CurrentGame.Printer.PrintText(message);
            return CurrentGame.PlayerInput.GetUserConfirmation();
        }

        private static void LogAdded(object sender, LogEventArgs e)
        {
            // Print the newest logs when a new log is added
            CurrentGame.LogPrinter.PrintNewestLogs(10, e.LogList);
        }

        private static void DrawChanceCard(object sender, DrawChanceCardArgs e)
        {
            int position = CurrentGame.CurrentGame.CurrentPlayer.Position;
            CurrentGame.CardPrinter.PrepareAndPrintSquareCard(position, e.ChanceCard);
        }

        private static void DrawCommunityChestCard(object sender, DrawCommunityChestCardArgs e)
        {
            int position = CurrentGame.CurrentGame.CurrentPlayer.Position;
            CurrentGame.CardPrinter.PrepareAndPrintSquareCard(position, null, e.CommunityChestCard);
        }

        private static void OpenPlayerActionMenu(object sender, OpenPlayerActionMenuArgs e)
        {
            PlayerActionMenu PlayerActionMenu = new PlayerActionMenu(CurrentGame.CurrentGame, CurrentGame.CurrentGame.CurrentPlayer);
            PlayerActionMenu.DisplayPlayerActionMainMenu();
        }

        private static void LandOnSquare(object sender, Square e)
        {
            //Square square = e.Square;
        }
    }
}
