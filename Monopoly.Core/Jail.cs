using Monopoly.Core.Events;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using System.Diagnostics;

namespace Monopoly.Core
{
    internal class Jail
    {
        private readonly IGame CurrentGame;
        public int JailPosition { get; }

        internal Jail(IGame game, int jailPosition)
        {
            CurrentGame = game;
            JailPosition = jailPosition;
        }

        internal class JailStatus
        {
            public int TurnsInJail { get; set; }

            public JailStatus()
            {
                TurnsInJail = 0;
            }
        }

        internal Dictionary<Player, JailStatus> playersInJail = new Dictionary<Player, JailStatus>();

        public JailStatus GetJailInfo(Player player)
        {
            IsPlayerInJail(player);
            playersInJail.TryGetValue(player, out JailStatus jailInfo);
            return jailInfo;
        }


        public void PlayerGoToJail(Player player, string reason = "")
        {
            ValidatePlayer(player);
            player.Position = JailPosition;
            playersInJail[player] = new JailStatus();
            string jailedReason = string.IsNullOrEmpty(reason) ? "" : $" {reason}";
            CurrentGame.Logs.CreateLog($"{player.Name} has been sent to jail{jailedReason}.");
        }

        public bool IsPlayerInJail(Player player)
        {
            ValidatePlayer(player);
            return VaildatePlayerInJail(player);
        }

        private void ValidatePlayer(Player player)
        {
            if (player is null)
                throw new ArgumentNullException(nameof(player), "Player cannot be null.");
        }

        private bool VaildatePlayerInJail(Player player)
        {
            if (!playersInJail.ContainsKey(player))
                throw new InvalidOperationException($"{player.Name} is not in jail!");
            else return true;
        }

        public bool TryPlayerBuyOut(Player player)
        {
            IsPlayerInJail(player);
            return player.NumberOfGetOutOFJailCards > 0 || CurrentGame.Handler.CanAffordWithAssets(player, CurrentGame.Rules.JailFine)
                ? GameEvents.InvokeAskPlayerToBuyOutOfJail(player, new PlayerEventArgs(player))
                : false;
        }

        public void IncrementTurnsInJail(Player player)
        {
            IsPlayerInJail(player);
            var jailInfo = GetJailInfo(player);
            jailInfo.TurnsInJail++;
        }

        public bool PlayerReachedMaxTurnsInJail(Player player)
        {
            IsPlayerInJail(player);
            var jailInfo = GetJailInfo(player);
            return jailInfo.TurnsInJail >= CurrentGame.Rules.MaxTurnsInJail;
        }

        public void HandleMaxTurnsInJail(Player player)
        {
            IsPlayerInJail(player);
            if (CurrentGame.Handler.IsPlayerBankrupt(player, CurrentGame.Rules.JailFine))
            {
                string reason = $", {player.Name} Could not afford to pay Jail Fine of {CurrentGame.Rules.JailFine}{CurrentGame.Rules.CurrencySymbol}";
                playersInJail.Remove(player);
                CurrentGame.Handler.HandlePlayerBankruptcy(player, reason);
            }
            else
            {
                string reason = BuyOutPlayerFromJail(player);
                ReleasePlayerFromJail(player, reason);
            }
        }

        public string BuyOutPlayerFromJail(Player player)
        {
            IsPlayerInJail(player);
            string reason;
            if (player.NumberOfGetOutOFJailCards > 0)
            {
                player.NumberOfGetOutOFJailCards--;
                reason = $", {player.Name} used a Get Out of Jail For Free card and have {player.NumberOfGetOutOFJailCards} left";
            }
            else
            {
                while (!CurrentGame.Transactions.PayFines(player, CurrentGame.Rules.JailFine))
                {
                    GameEvents.InvokePlayerInsufficientFunds(player, CurrentGame.Rules.JailFine);
                }
                reason = $", {player.Name} paid the fine to get out of jail";
            }
            return reason;
        }

        public void ReleasePlayerFromJail(Player player, string reason = "")
        {
            IsPlayerInJail(player);
            string releaseReason = $"{player.Name} has been released from jail" + (string.IsNullOrEmpty(reason) ? "" : $"{reason}");
            CreateJailLog(player, releaseReason);
            playersInJail.Remove(player);
        }

        private void CreateJailLog(Player player, string log)
        {
            var jailInfo = GetJailInfo(player);
            CurrentGame.Logs.CreateLog($"JailTurn {jailInfo.TurnsInJail}: {log}.");
        }
    }
}
