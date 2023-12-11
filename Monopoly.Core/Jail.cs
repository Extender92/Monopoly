using Monopoly.Core.Events;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    internal class Jail
    {
        private Dictionary<Player, PlayerJailInfo> playersInJail = new Dictionary<Player, PlayerJailInfo>();

        public int MaxTurnsInJail { get; } = 3;
        public int Fine { get; } = 50;
        private const int JailPosition = 10;

        internal void PlayerGoToJail(Player player)
        {
            player.Position = JailPosition;
            player.InJail = true;
            playersInJail[player] = new PlayerJailInfo();
            Game.Logs.CreateLog($"{player.Name} went to jail");
        }

        internal void TakeTurnInJail(Player player)
        {
            if (playersInJail.TryGetValue(player, out PlayerJailInfo jailInfo))
            {
                bool playerWantsToBuyOut = false;

                if (player.NumberOfGetOutOFJailCards > 0 || Game.CanAffordWithAssets(player, Fine))
                    playerWantsToBuyOut = GameEvents.InvokeAskPlayerToBuyOutOfJail(player, new PlayerEventArgs(player));

                Game.RollDice(player);

                if (playerWantsToBuyOut)
                {
                    BuyOutPlayerFromJail(player);
                    return;
                }

                if (Game.IsDiceDouble())
                {
                    Game.Dice[0].ScrambleDie();
                    ReleasePlayerFromJail(player);
                    return;
                }

                jailInfo.TurnsInJail++;

                if (!(jailInfo.TurnsInJail < MaxTurnsInJail))
                {
                    if (Game.IsPlayerBankrupt(player, Fine))
                    {
                        Game.HandlePlayerBankruptcy(player);
                        return;
                    }
                    BuyOutPlayerFromJail(player);
                }
            }
            else
            {
                throw new InvalidOperationException($"{player.Name} is not in jail!");
            }
        }

        private void BuyOutPlayerFromJail(Player player)
        {
            if (player.NumberOfGetOutOFJailCards > 0)
            {
                player.NumberOfGetOutOFJailCards--;
            }
            else
            {
                while (!Game.Transactions.PayFines(player, Fine))
                {
                    GameEvents.InvokePlayerInsufficientFunds(player, Fine);
                }
            }
            ReleasePlayerFromJail(player);
        }

        private void ReleasePlayerFromJail(Player player)
        {
            // Remove the player from the playersInJail dictionary
            playersInJail.Remove(player);

            player.InJail = false;
            Game.Logs.CreateLog($"{player.Name} was released from jail");

            player.Position += Game.CalculateDiceSum();
        }

        private class PlayerJailInfo
        {
            public int TurnsInJail { get; set; }
        }
    }
}
