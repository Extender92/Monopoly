using Monopoly.Core.Events;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Monopoly.Core
{
    public sealed class Jail
    {
        private readonly Game CurrentGame;
        private readonly Dictionary<Player, JailStatus> _playersInJail = new();
        private readonly ReadOnlyDictionary<Player, JailStatus> _playersInJailView;
        public int JailPosition { get; }
        public IReadOnlyDictionary<Player, JailStatus> PlayersInJail => _playersInJailView;

        internal Jail(Game game, int jailPosition)
        {
            CurrentGame = game ?? throw new ArgumentNullException(nameof(game));
            if (jailPosition < 0) throw new ArgumentOutOfRangeException(nameof(jailPosition));
            JailPosition = jailPosition;
            _playersInJailView = new ReadOnlyDictionary<Player, JailStatus>(_playersInJail);
        }

        public sealed class JailStatus
        {
            public int TurnsInJail { get; private set; }

            internal JailStatus(int turnsInJail = 0)
            {
                if (turnsInJail < 0) throw new ArgumentOutOfRangeException(nameof(turnsInJail));
                TurnsInJail = turnsInJail;
            }

            internal void Increment() => TurnsInJail++;
        }

        /// <summary>Attempts to get the current jail status for a player.</summary>
        /// <param name="player">The player whose status should be queried.</param>
        /// <param name="jailStatus">
        /// The stored status when the player is in jail; otherwise null.
        /// </param>
        /// <returns>True when the player has a jail entry; otherwise false.</returns>
        public bool TryGetJailInfo(Player player, [NotNullWhen(true)] out JailStatus? jailStatus)
        {
            ValidatePlayer(player);
            return _playersInJail.TryGetValue(player, out jailStatus);
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
            if (turnsInJail < 0 || turnsInJail > CurrentGame.Rules.MaxTurnsInJail)
                throw new ArgumentOutOfRangeException(nameof(turnsInJail));
            player.MoveTo(JailPosition);
            _playersInJail[player] = new JailStatus(turnsInJail);
        }


        internal void PlayerGoToJail(Player player, string reason = "")
        {
            ValidatePlayer(player);
            CurrentGame.Handler.MovePlayerAndInvokeEvent(player, JailPosition);
            _playersInJail[player] = new JailStatus();
            string jailedReason = string.IsNullOrEmpty(reason) ? "" : $" {reason}";
            CurrentGame.LogWriter.CreateLog($"{player.Name} has been sent to jail{jailedReason}.");
        }

        public bool IsPlayerInJail(Player player)
        {
            return TryGetJailInfo(player, out _);
        }

        private void ValidatePlayer(Player player)
        {
            if (player is null)
                throw new ArgumentNullException(nameof(player), "Player cannot be null.");
            if (!CurrentGame.ContainsPlayer(player))
                throw new ArgumentException("The player does not belong to this game.", nameof(player));
        }

        internal void IncrementTurnsInJail(Player player)
        {
            var jailInfo = GetJailInfo(player);
            jailInfo.Increment();
        }

        internal bool PlayerReachedMaxTurnsInJail(Player player)
        {
            return TryGetJailInfo(player, out JailStatus? jailInfo) &&
                jailInfo.TurnsInJail >= CurrentGame.Rules.MaxTurnsInJail;
        }

        internal void HandleMaxTurnsInJail(Player player)
        {
            _ = GetJailInfo(player);
            if (CurrentGame.Handler.IsPlayerBankrupt(player, CurrentGame.Rules.JailFine))
            {
                string reason = $", {player.Name} Could not afford to pay Jail Fine of {CurrentGame.Rules.JailFine}{CurrentGame.Rules.CurrencySymbol}";
                _playersInJail.Remove(player);
                CurrentGame.Handler.HandlePlayerBankruptcy(player, reason);
            }
            else
            {
                string reason = BuyOutPlayerFromJail(player);
                ReleasePlayerFromJail(player, reason);
            }
        }

        internal string BuyOutPlayerFromJail(Player player)
        {
            _ = GetJailInfo(player);
            string reason;
            if (player.NumberOfGetOutOFJailCards > 0)
            {
                player.TryUseJailCard();
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

        internal void ReleasePlayerFromJail(Player player, string reason = "")
        {
            JailStatus jailInfo = GetJailInfo(player);
            string releaseReason = $"{player.Name} has been released from jail" + (string.IsNullOrEmpty(reason) ? "" : $"{reason}");
            CreateJailLog(jailInfo, releaseReason);
            _playersInJail.Remove(player);
        }

        private void CreateJailLog(JailStatus jailInfo, string log)
        {
            CurrentGame.LogWriter.CreateLog($"JailTurn {jailInfo.TurnsInJail}: {log}.");
        }
    }
}
