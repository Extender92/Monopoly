using Monopoly.Console.GUI;
using Monopoly.Core;
using Monopoly.Core.Events;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System.Numerics;

namespace Monopoly.Console.Events
{
    internal static class ConsoleEventHandler
    {
        public static void SubscribeToEvents()
        {
            GameEvents.PlayerInsufficientFunds += HandlePlayerInsufficientFunds;
            GameEvents.AskPlayerToBuyPurchasableSquare += HandleAskIfPlayerWantsBuyPurchasableSquare;
            GameEvents.AskPlayerToBuyOutOfJail += HandleAskPlayerToBuyOutOfJail;
            GameEvents.LogAdded += LogHandler_LogAdded;
        }

        private static void HandlePlayerInsufficientFunds(object sender, PlayerEventArgs e)
        {
            Player player = e.Player;
            int targetSum = e.TargetSum;
            string message = $"{player.Name} you dont have enough money, you need {targetSum}{Game.Rules.CurrencySymbol}";
            ConsoleGame.Printer.PrintText(message);
            // Menu SellAssets
        }

        private static bool HandleAskIfPlayerWantsBuyPurchasableSquare(object sender, SquareEventArgs e)
        {
            Square square = e.Square;
            string message = $"Do you want to buy {square.Name} for {square.Price}{Game.Rules.CurrencySymbol}?";
            ConsoleGame.Printer.PrintText(message);
            return ConsoleGame.PlayerInput.GetUserConfirmation();
        }

        private static bool HandleAskPlayerToBuyOutOfJail(object sender, PlayerEventArgs e)
        {
            Player player = e.Player;
            string message = $"{player.Name} do you want to buy yourself out from prison?";
            ConsoleGame.Printer.PrintText(message);
            return ConsoleGame.PlayerInput.GetUserConfirmation();
        }

        private static void LogHandler_LogAdded(object sender, LogEventArgs e)
        {
            // Print the newest logs when a new log is added
            ConsoleGame.Printer.PrintNewestLogs(10);
        }
    }
}
