using Monopoly.Core.Events;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    internal class Jail
    {
        public Game TheGame { get; set; }
        private int JailPosition { get; set; }
        public int Fine { get; set; }
        public int MaxTurnsInJail { get; set; }

        public Jail(Game game)
        {
            TheGame = game;
            Fine = game.Rules.JailFine;
            JailPosition = game.Board.Squares.First(s => s.Name == "Jail").Position;
            MaxTurnsInJail = game.Rules.MaxTurnsInJail;
        }

        private class JailStatus
        {
            public int TurnsInJail { get; set; }

            public JailStatus()
            {
                TurnsInJail = 0;
            }
        }

        private Dictionary<Player, JailStatus> playersInJail = new Dictionary<Player, JailStatus>();


        internal void PlayerGoToJail(Player player, string reason = "")
        {
            player.Position = JailPosition;
            playersInJail[player] = new JailStatus();
            string jailedReason = string.IsNullOrEmpty(reason) ? "" : $" ({reason})";
            TheGame.Logs.CreateLog($"{player.Name} has been sent to jail{jailedReason}.");
        }

        //internal void TakeTurnInJail(Player player)
        //{
        //    if (playersInJail.TryGetValue(player, out JailStatus jailInfo))
        //    {
        //        bool playerWantsToBuyOut = false;

        //        if (player.NumberOfGetOutOFJailCards > 0 || TheGame.Handler.CanAffordWithAssets(player, Fine))
        //            playerWantsToBuyOut = GameEvents.InvokeAskPlayerToBuyOutOfJail(player, new PlayerEventArgs(player));

        //        TheGame.Handler.RollDice(player);

        //        if (playerWantsToBuyOut)
        //        {
        //            BuyOutPlayerFromJail(player);
        //            return;
        //        }

        //        if (TheGame.Handler.IsDiceDouble())
        //        {
        //            TheGame.Dice[0].ScrambleDie();
        //            ReleasePlayerFromJail(player);
        //            return;
        //        }

        //        jailInfo.TurnsInJail++;

        //        if (!(jailInfo.TurnsInJail < MaxTurnsInJail))
        //        {
        //            if (TheGame.Handler.IsPlayerBankrupt(player, Fine))
        //            {
        //                TheGame.Handler.HandlePlayerBankruptcy(player);
        //                return;
        //            }
        //            BuyOutPlayerFromJail(player);
        //        }
        //    }
        //    else
        //    {
        //        throw new InvalidOperationException($"{player.Name} is not in jail!");
        //    }
        //}

        internal void TakeTurnInJail(Player player)
        {
            ValidatePlayerInJail(player);

            var jailInfo = GetJailInfo(player);
            bool playerWantsToBuyOut = TryPlayerBuyOut(player);

            HandleRollDice(player);

            if (playerWantsToBuyOut)
            {
                BuyOutPlayerFromJail(player);
                return;
            }

            if (IsPlayerDiceDouble(player))
            {
                ReleasePlayerFromJail(player);
                return;
            }

            IncrementTurnsInJail(player, jailInfo);

            if (ReachedMaxTurnsInJail(player, jailInfo))
            {
                HandleMaxTurnsInJail(player);
            }
        }

        public void ValidatePlayerInJail(Player player)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player), "Player cannot be null.");
            }
            if (!playersInJail.ContainsKey(player))
            {
                throw new InvalidOperationException($"{player.Name} is not in jail!");
            }
        }

        private JailStatus GetJailInfo(Player player)
        {
            playersInJail.TryGetValue(player, out JailStatus jailInfo);
            return jailInfo;
        }

        private bool TryPlayerBuyOut(Player player)
        {
            return player.NumberOfGetOutOFJailCards > 0 || TheGame.Handler.CanAffordWithAssets(player, Fine)
                ? GameEvents.InvokeAskPlayerToBuyOutOfJail(player, new PlayerEventArgs(player))
                : false;
        }

        private void HandleRollDice(Player player)
        {
            TheGame.Handler.RollDice(player);
        }

        private bool IsPlayerDiceDouble(Player player)
        {
            return TheGame.Handler.IsDiceDouble();
        }

        private void IncrementTurnsInJail(Player player, JailStatus jailInfo)
        {
            jailInfo.TurnsInJail++;
        }

        private bool ReachedMaxTurnsInJail(Player player, JailStatus jailInfo)
        {
            return jailInfo.TurnsInJail >= MaxTurnsInJail;
        }

        private void HandleMaxTurnsInJail(Player player)
        {
            if (TheGame.Handler.IsPlayerBankrupt(player, Fine))
            {
                TheGame.Handler.HandlePlayerBankruptcy(player);
            }
            else
            {
                BuyOutPlayerFromJail(player);
            }
        }

        private void BuyOutPlayerFromJail(Player player)
        {
            string reason;
            if (player.NumberOfGetOutOFJailCards > 0)
            {
                player.NumberOfGetOutOFJailCards--;
                reason = $"used a Get Out of Jail Free card and have {player.NumberOfGetOutOFJailCards} left";
            }
            else
            {
                while (!TheGame.Transactions.PayFines(player, Fine))
                {
                    GameEvents.InvokePlayerInsufficientFunds(player, Fine);
                }
                reason = "paid the fine to get out of jail";
            }
            ReleasePlayerFromJail(player, reason);
        }

        private void ReleasePlayerFromJail(Player player, string reason = "")
        {
            playersInJail.Remove(player);
            string releaseReason = string.IsNullOrEmpty(reason) ? "" : $" ({reason})";
            CreateJailLog(player, $"{player.Name} has been released from jail{releaseReason}.");
        }

        private void CreateJailLog(Player player, string log)
        {
            var jailInfo = GetJailInfo(player);
            TheGame.Logs.CreateLog($"JailTurn {jailInfo.TurnsInJail}: {log}");
        }
    }
}
