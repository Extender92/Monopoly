using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Monopoly.Core.Events.PlayerEventArgs;
using static Monopoly.Core.Events.SquareEventArgs;

namespace Monopoly.Core.Events
{
    internal static class GameEvents
    {
        public static event EventHandler<PlayerEventArgs> PlayerInsufficientFunds;
        public static event PlayerEventHandler AskPlayerToBuyOutOfJail;
        public static event SquareEventHandler AskPlayerToBuyPurchasableSquare;
        public static event EventHandler<LogEventArgs> LogAdded;


        public static bool InvokeAskPlayerToBuyPurchasableSquare(object sender, SquareEventArgs e)
        {
            return AskPlayerToBuyPurchasableSquare?.Invoke(sender, e) ?? false;
        }

        public static bool InvokeAskPlayerToBuyOutOfJail(object sender, PlayerEventArgs e)
        {
            return AskPlayerToBuyOutOfJail?.Invoke(sender, e) ?? false;
        }

        public static void InvokePlayerInsufficientFunds(Player player, int targetSum)
        {
            PlayerInsufficientFunds?.Invoke(player, new PlayerEventArgs(player, targetSum));
        }

        public static void InvokeLogAdded(object sender, string logInfo)
        {
            LogAdded?.Invoke(sender, new LogEventArgs(logInfo));
        }
    }
}
