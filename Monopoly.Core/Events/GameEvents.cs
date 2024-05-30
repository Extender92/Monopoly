using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models.FortuneCard;
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
        public static event EventHandler<DrawChanceCardArgs> ChanceCardDrawn;
        public static event EventHandler<DrawCommunityChestCardArgs> CommunityChestCardDrawn;
        public static event EventHandler<OpenPlayerActionMenuArgs> OpenPlayerActionMenu;


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

        public static void InvokeLogAdded(object sender, List<Log> logInfo)
        {
            LogAdded?.Invoke(sender, new LogEventArgs(logInfo));
        }

        public static void InvokeDrawChanceCard(object sender, IChanceCard chanceCard)
        {
            ChanceCardDrawn?.Invoke(sender, new DrawChanceCardArgs(chanceCard));
        }

        public static void InvokeDrawCommunityChestCard(object sender, ICommunityChestCard communityChest)
        {
            CommunityChestCardDrawn?.Invoke(sender, new DrawCommunityChestCardArgs(communityChest));
        }

        public static void InvokeOpenPlayerActionMenu(object sender)
        {
            OpenPlayerActionMenu?.Invoke(sender, new OpenPlayerActionMenuArgs());
        }
    }
}
