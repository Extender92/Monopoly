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
        public static event EventHandler<PlayerEventArgs> PlayerInsufficientFundsEvent;
        public static event PlayerEventHandler AskPlayerToBuyOutOfJailEvent;
        public static SquareEventHandler AskPlayerToBuyPurchasableSquareEvent;
        public static event EventHandler<LogEventArgs> LogAddedEvent;
        public static event EventHandler<DrawChanceCardArgs> ChanceCardDrawnEvent;
        public static event EventHandler<DrawCommunityChestCardArgs> CommunityChestCardDrawnEvent;
        public static EventHandler<OpenPlayerActionMenuArgs> OpenPlayerActionMenuEvent;


        public static bool InvokeAskPlayerToBuyPurchasableSquare(object sender, SquareEventArgs e)
        {
            return AskPlayerToBuyPurchasableSquareEvent?.Invoke(sender, e) ?? false;
        }

        public static bool InvokeAskPlayerToBuyOutOfJail(object sender, PlayerEventArgs e)
        {
            return AskPlayerToBuyOutOfJailEvent?.Invoke(sender, e) ?? false;
        }

        public static void InvokePlayerInsufficientFunds(Player player, int targetSum)
        {
            PlayerInsufficientFundsEvent?.Invoke(player, new PlayerEventArgs(player, targetSum));
        }

        public static void InvokeLogAdded(object sender, List<Log> logInfo)
        {
            LogAddedEvent?.Invoke(sender, new LogEventArgs(logInfo));
        }

        public static void InvokeDrawChanceCard(object sender, IChanceCard chanceCard)
        {
            ChanceCardDrawnEvent?.Invoke(sender, new DrawChanceCardArgs(chanceCard));
        }

        public static void InvokeDrawCommunityChestCard(object sender, ICommunityChestCard communityChest)
        {
            CommunityChestCardDrawnEvent?.Invoke(sender, new DrawCommunityChestCardArgs(communityChest));
        }

        public static void InvokeOpenPlayerActionMenu(object sender)
        {
            OpenPlayerActionMenuEvent?.Invoke(sender, new OpenPlayerActionMenuArgs());
        }
    }
}
