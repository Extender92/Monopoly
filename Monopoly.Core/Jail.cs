using Monopoly.Core.Events;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using System.Diagnostics.CodeAnalysis;

namespace Monopoly.Core
{
    public class Jail
    {
        private readonly IGame CurrentGame;
        public int JailPosition { get; }

        public Jail(IGame game, int jailPosition)
        {
            CurrentGame = game;
            JailPosition = jailPosition;
        }

        public class JailStatus
        {
            public int TurnsInJail { get; set; }

            public JailStatus()
            {
                TurnsInJail = 0;
            }
        }

        public Dictionary<Player, JailStatus> playersInJail = new Dictionary<Player, JailStatus>();

        /// <summary>Attempts to get the current jail status for a player.</summary>
        /// <param name="player">The player whose status should be queried.</param>
        /// <param name="jailStatus">
        /// The stored status when the player is in jail; otherwise null.
        /// </param>
        /// <returns>True when the player has a jail entry; otherwise false.</returns>
        public bool TryGetJailInfo(Player player, [NotNullWhen(true)] out JailStatus? jailStatus)
        {
            ValidatePlayer(player);
            return playersInJail.TryGetValue(player, out jailStatus);
        }

        /// <summary>
        /// Gets the current jail status for a player known to be in jail. A null player causes
        /// an ArgumentNullException; a player without a jail entry causes an InvalidOperationException.
        /// </summary>
        public JailStatus GetJailInfo(Player player)
        {
            if (TryGetJailInfo(player, out JailStatus? jailStatus))
                return jailStatus;

            throw new InvalidOperationException($"Player '{player.Name}' is not in jail.");
        }

        internal void RestorePlayerInJail(Player player, int turnsInJail)
        {
            ValidatePlayer(player);
            player.Position = JailPosition;
            playersInJail[player] = new JailStatus { TurnsInJail = turnsInJail };
        }


        public void PlayerGoToJail(Player player, string reason = "")
        {
            ValidatePlayer(player);
            CurrentGame.Handler.MovePlayerAndInvokeEvent(player, JailPosition);
            playersInJail[player] = new JailStatus();
            string jailedReason = string.IsNullOrEmpty(reason) ? "" : $" {reason}";
            CurrentGame.Logs.CreateLog($"{player.Name} has been sent to jail{jailedReason}.");
        }

        public bool IsPlayerInJail(Player player)
        {
            return TryGetJailInfo(player, out _);
        }

        private void ValidatePlayer(Player player)
        {
            if (player is null)
                throw new ArgumentNullException(nameof(player), "Player cannot be null.");
        }

        public bool TryPlayerBuyOut(Player player)
        {
            if (!IsPlayerInJail(player))
                return false;

            return player.NumberOfGetOutOFJailCards > 0 || CurrentGame.Handler.CanAffordWithAssets(player, CurrentGame.Rules.JailFine)
                ? GameEvents.InvokeAskPlayerToBuyOutOfJail(CurrentGame, player)
                : false;
        }

        public void IncrementTurnsInJail(Player player)
        {
            var jailInfo = GetJailInfo(player);
            jailInfo.TurnsInJail++;
        }

        public bool PlayerReachedMaxTurnsInJail(Player player)
        {
            return TryGetJailInfo(player, out JailStatus? jailInfo) &&
                jailInfo.TurnsInJail >= CurrentGame.Rules.MaxTurnsInJail;
        }

        public void HandleMaxTurnsInJail(Player player)
        {
            _ = GetJailInfo(player);
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
            _ = GetJailInfo(player);
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
                    int moneyBefore = player.Money;
                    GameEvents.InvokePlayerInsufficientFunds(CurrentGame, player, CurrentGame.Rules.JailFine);
                    if (player.Money <= moneyBefore)
                    {
                        CurrentGame.Handler.HandlePlayerBankruptcy(player, $", {player.Name} Could not afford to pay Jail Fine of {CurrentGame.Rules.JailFine}{CurrentGame.Rules.CurrencySymbol}");
                        break;
                    }
                }
                reason = $", {player.Name} paid the fine to get out of jail";
            }
            return reason;
        }

        public void ReleasePlayerFromJail(Player player, string reason = "")
        {
            JailStatus jailInfo = GetJailInfo(player);
            string releaseReason = $"{player.Name} has been released from jail" + (string.IsNullOrEmpty(reason) ? "" : $"{reason}");
            CreateJailLog(jailInfo, releaseReason);
            playersInJail.Remove(player);
        }

        private void CreateJailLog(JailStatus jailInfo, string log)
        {
            CurrentGame.Logs.CreateLog($"JailTurn {jailInfo.TurnsInJail}: {log}.");
        }
    }
}
