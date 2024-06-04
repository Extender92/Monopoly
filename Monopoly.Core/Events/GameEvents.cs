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
        public static event PlayerEventHandler AskPlayerToBuyOutOfJailEvent;
        public static event SquareEventHandler AskPlayerToBuyPurchasableSquareEvent;
        public static event EventHandler<PlayerEventArgs> PlayerInsufficientFundsEvent;
        public static event EventHandler<EventArgs> LogAddedEvent;
        public static event EventHandler<DrawChanceCardArgs> ChanceCardDrawnEvent;
        public static event EventHandler<DrawCommunityChestCardArgs> CommunityChestCardDrawnEvent;
        public static event EventHandler<EventArgs> OpenPlayerActionMenuEvent;
        public static event EventHandler<SquareEventArgs> LandOnSquareEvent;
        public static event EventHandler<EventArgs> UpdateGameBoard;
        public static event EventHandler<EventArgs> UpdatePlayerInformation;

        public static bool InvokeAskPlayerToBuyPurchasableSquare(object sender, Square square)
        {
            return AskPlayerToBuyPurchasableSquareEvent?.Invoke(sender, new SquareEventArgs(square)) ?? false;
        }

        public static bool InvokeAskPlayerToBuyOutOfJail(object sender, Player player)
        {
            return AskPlayerToBuyOutOfJailEvent?.Invoke(sender, new PlayerEventArgs(player)) ?? false;
        }

        public static void InvokePlayerInsufficientFunds(object sender, Player player, int targetSum)
        {
            PlayerInsufficientFundsEvent?.Invoke(sender, new PlayerEventArgs(player, targetSum));
        }

        public static void InvokeLogAdded(object sender)
        {
            LogAddedEvent?.Invoke(sender, EventArgs.Empty);
        }

        public static void InvokeDrawChanceCard(object sender, IChanceCard chanceCard)
        {
            ChanceCardDrawnEvent?.Invoke(sender, new DrawChanceCardArgs(chanceCard));
        }

        public static void InvokeDrawCommunityChestCard(object sender, ICommunityChestCard communityChestCard)
        {
            CommunityChestCardDrawnEvent?.Invoke(sender, new DrawCommunityChestCardArgs(communityChestCard));
        }

        public static void InvokeOpenPlayerActionMenu(object sender)
        {
            OpenPlayerActionMenuEvent?.Invoke(sender, EventArgs.Empty);
        }

        public static void InvokeLandOnSquare(object sender, Square square)
        {
            LandOnSquareEvent?.Invoke(sender, new SquareEventArgs(square));
        }

        public static void InvokeUpdateGameBoard(object sender)
        {
            UpdateGameBoard?.Invoke(sender, EventArgs.Empty);
        }

        public static void InvokeUpdatePlayerInformation(object sender)
        {
            UpdatePlayerInformation?.Invoke(sender, EventArgs.Empty);
        }
    }
}
