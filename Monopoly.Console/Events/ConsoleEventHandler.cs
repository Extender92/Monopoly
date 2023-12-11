using Monopoly.Console.GUI;
using Monopoly.Core;
using Monopoly.Core.Events;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;

namespace Monopoly.Console.Events
{
    internal static class ConsoleEventHandler
    {
        public static void SubscribeToEvents()
        {
            GameEvents.PlayerInsufficientFunds += HandlePlayerInsufficientFunds;
            GameEvents.AskPlayerToBuyPurchasableSquare += HandleAskIfPlayerWantsBuyPurchasableSquare;
            GameEvents.AskPlayerToBuyOutOfJail += HandleAskPlayerToBuyOutOfJail;
        }

        private static void HandlePlayerInsufficientFunds(object sender, PlayerEventArgs e)
        {
            Player player = e.Player;
            int targetSum = e.TargetSum;
            string message = $"You dont have enough money, you need {targetSum}{Game.Rules.CurrencySymbol}";
            ConsolePrinter.PrintText(message);
            // Menu SellAssets
        }

        private static bool HandleAskIfPlayerWantsBuyPurchasableSquare(object sender, SquareEventArgs e)
        {
            Square square = e.Square;
            string message = $"Do you want to buy {square.Name} for {square.Price}{Game.Rules.CurrencySymbol}?";
            ConsolePrinter.PrintText(message);
            return Input.GetUserConfirmation();
        }

        private static bool HandleAskPlayerToBuyOutOfJail(object sender, PlayerEventArgs e)
        {
            Player player = e.Player;
            string message = $"Do you want to buy?";
            ConsolePrinter.PrintText(message);
            return Input.GetUserConfirmation();
        }
    }
}
