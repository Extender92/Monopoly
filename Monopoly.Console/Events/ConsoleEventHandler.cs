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
        private static ConsoleGame _consoleGame;

        public static void SubscribeToEvents(ConsoleGame consoleGame)
        {
            _consoleGame = consoleGame;
            GameEvents.PlayerInsufficientFunds += HandlePlayerInsufficientFunds;
            GameEvents.AskPlayerToBuyPurchasableSquare += HandleAskIfPlayerWantsBuyPurchasableSquare;
            GameEvents.AskPlayerToBuyOutOfJail += HandleAskPlayerToBuyOutOfJail;
            GameEvents.LogAdded += LogAdded;
            GameEvents.ChanceCardDrawn += DrawChanceCard;
            GameEvents.CommunityChestCardDrawn += DrawCommunityChestCard;
        }

        private static void HandlePlayerInsufficientFunds(object sender, PlayerEventArgs e)
        {
            Player player = e.Player;
            int targetSum = e.TargetSum;
            string message = $"{player.Name} you dont have enough money, you need {targetSum}{_consoleGame.CurrentGame.Rules.CurrencySymbol}";
            _consoleGame.Printer.PrintText(message);
            new PlayerActionMenu(_consoleGame.CurrentGame, player).DisplayPlayerActionRealEstateMenu();
        }

        private static bool HandleAskIfPlayerWantsBuyPurchasableSquare(object sender, SquareEventArgs e)
        {
            Square square = e.Square;
            string message = $"Do you want to buy {square.Name} for {square.Price}{_consoleGame.CurrentGame.Rules.CurrencySymbol}?";
            _consoleGame.Printer.PrintText(message);
            return _consoleGame.PlayerInput.GetUserConfirmation();
        }

        private static bool HandleAskPlayerToBuyOutOfJail(object sender, PlayerEventArgs e)
        {
            Player player = e.Player;
            string message = $"{player.Name} do you want to buy yourself out from prison?";
            _consoleGame.Printer.PrintText(message);
            return _consoleGame.PlayerInput.GetUserConfirmation();
        }

        private static void LogAdded(object sender, LogEventArgs e)
        {
            // Print the newest logs when a new log is added
            _consoleGame.LogPrinter.PrintNewestLogs(10, e.LogList);
        }

        private static void DrawChanceCard(object sender, DrawChanceCardArgs e)
        {
            int position = _consoleGame.CurrentGame.CurrentPlayer.Position;
            _consoleGame.CardPrinter.PrepareAndPrintSquareCard(position, e.ChanceCard);
        }

        private static void DrawCommunityChestCard(object sender, DrawCommunityChestCardArgs e)
        {
            int position = _consoleGame.CurrentGame.CurrentPlayer.Position;
            _consoleGame.CardPrinter.PrepareAndPrintSquareCard(position, null, e.CommunityChestCard);
        }
    }
}
