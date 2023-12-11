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
    internal class Transaction
    {
        internal void PlayerGetSalary(Player player)
        {
            player.Money += Game.Rules.Salary;
            Game.Logs.CreateLog($"{player.Name} collected salary {Game.Rules.Salary}{Game.Rules.CurrencySymbol}");
        }

        public bool BuyPurchasableSquare(Player player, Square square)
        {
            if (player.Money >= square.Price)
            {
                player.Money -= square.Price;
                square.Owner = player;
                Game.Logs.CreateLog($"{player.Name} bought {square.Name} for {square.Price}{Game.Rules.CurrencySymbol}");
                return true;
            }
            return false;
        }

        public bool PayRentFromPlayerToPlayer(Player fromPlayer, int rent, Player toPlayer)
        {
            if (fromPlayer.Money >= rent)
            {
                fromPlayer.Money -= rent;
                toPlayer.Money += rent;
                Game.Logs.CreateLog($"{fromPlayer.Name} payed rent {rent}{Game.Rules.CurrencySymbol} to {toPlayer.Name}");
                return true;
            }
            return false;
        }

        public void MortgageProperty(Player player, Square square)
        {
            GetMoneyFromBank(player, square.MortgageValue);
            square.IsMortgage = true;
            Game.Logs.CreateLog($"{player.Name} mortgage {square.Name} for {square.MortgageValue}{Game.Rules.CurrencySymbol}");
        }

        public bool RepayMortgageProperty(Player player, Square square)
        {
            int interestRate = Game.Rules.MortgageInterestRate;
            int sumToPay = (int)(square.MortgageValue * (1 + interestRate / 100.0));
            if (sumToPay <= player.Money)
            {
                player.Money -= sumToPay;
                square.IsMortgage = false;
                Game.Logs.CreateLog($"{player.Name} repayed mortgage {sumToPay}{Game.Rules.CurrencySymbol} for {square.Name}");
                return true;
            }
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
            Game.Logs.CreateLog($"{player.Name} collected money from bank {sum}{Game.Rules.CurrencySymbol}");
        }

        internal bool PayTax(Player player, int sum)
        {
            if (sum <= player.Money)
            {
                player.Money -= sum;
                return true;
            }
            return false;
        }

        internal bool PayFines(Player player, int fines)
        {
            if (fines <= player.Money)
            {
                if (Game.Rules.FreeParking == GameRules.Parking.Fines)
                {
                    Game.Fines += fines;
                }
                player.Money -= fines;
                Game.Logs.CreateLog($"{player.Name} payed fines {fines}{Game.Rules.CurrencySymbol}");
                return true;
            }
            return false;
        }
    }
}
