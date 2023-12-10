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
    internal class Transaction
    {
        internal void PlayerGetSalary(Player player)
        {
            player.Money += Game.Rules.Salary;
        }

        public bool BuyPurchasableSquare(Player player, Square square)
        {
            if (player.Money >= square.Price)
            {
                player.Money -= square.Price;
                square.Owner = player;
                return true;
            }
            return false;
        }

        public bool PayRentFromPlayerToPlayer(Player fromPlayer, int rent, Player toPlayer)
        {
            if (fromPlayer.Money < rent)
            {
                fromPlayer.Money -= rent;
                toPlayer.Money += rent;
                return true;
            }
            return false;
        }

        public void MortgageProperty()
        {

        }

        public bool RepayMortgageProperty()
        {
            return false;
        }

        internal void HandleCanBuySquare(Player player, Square square)
        {
            if (square.Price > player.Money && AskToManagePropertiesForBuyingSquare(player, square))
            {
                BuyPurchasableSquare(player, square);
            }
            else if (GameEvents.InvokeAskPlayerToBuyPurchasableSquare(square, new SquareEventArgs(square)))
            {
                BuyPurchasableSquare(player, square);
            }
        }

        private bool AskToManagePropertiesForBuyingSquare(Player player, Square square)
        {
            while (square.Price > player.Money)
            {
                if (!GameEvents.InvokeAskPlayerToBuyPurchasableSquare(square, new SquareEventArgs(square)))
                {
                    return false;
                }
                // Event to tell the player they can't afford and ask if they want to manage properties to afford
                GameEvents.InvokePlayerInsufficientFunds(player, square.Price);
            }
            return true;
        }

        internal void GetMoneyFromBank(Player player, int sum)
        {
            player.Money += sum;
        }

        internal bool PayTax(Player player, int sum)
        {
            if (sum > player.Money)
            {
                player.Money -= sum;
                return true;
            }
            return false;
        }

        internal bool PayFines(Player player, int sum)
        {
            if (sum > player.Money)
            {
                if (Game.Rules.FreeParking == GameRules.Parking.Fines)
                {
                    Game.Fines += sum;
                }
                player.Money -= sum;
                return true;
            }
            return false;
        }
    }
}
