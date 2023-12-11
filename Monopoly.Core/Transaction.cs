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
        // Add a private field to hold the reference to the Game instance
        private Game _game;

        // Constructor to initialize the Game reference
        public Transaction(Game game)
        {
            _game = game;
        }

        internal void PlayerGetSalary(Player player)
        {
            player.Money += _game.Rules.Salary;
            _game.Logs.CreateLog($"{player.Name} collected salary {_game.Rules.Salary}{_game.Rules.CurrencySymbol}");
        }

        public bool BuyPurchasableSquare(Player player, Square square)
        {
            if (player.Money >= square.Price)
            {
                player.Money -= square.Price;
                square.Owner = player;
                _game.Logs.CreateLog($"{player.Name} bought {square.Name} for {square.Price}{_game.Rules.CurrencySymbol}");
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
                _game.Logs.CreateLog($"{fromPlayer.Name} payed rent {rent}{_game.Rules.CurrencySymbol} to {toPlayer.Name}");
                return true;
            }
            return false;
        }

        public void MortgageProperty(Player player, Square square)
        {
            GetMoneyFromBank(player, square.MortgageValue);
            square.IsMortgage = true;
            _game.Logs.CreateLog($"{player.Name} mortgage {square.Name} for {square.MortgageValue}{_game.Rules.CurrencySymbol}");
        }

        public bool RepayMortgageProperty(Player player, Square square)
        {
            int interestRate = _game.Rules.MortgageInterestRate;
            int sumToPay = (int)(square.MortgageValue * (1 + interestRate / 100.0));
            if (sumToPay <= player.Money)
            {
                player.Money -= sumToPay;
                square.IsMortgage = false;
                _game.Logs.CreateLog($"{player.Name} repayed mortgage {sumToPay}{_game.Rules.CurrencySymbol} for {square.Name}");
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
            _game.Logs.CreateLog($"{player.Name} collected money from bank {sum}{_game.Rules.CurrencySymbol}");
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
                if (_game.Rules.FreeParking == GameRules.Parking.Fines)
                {
                    _game.Fines += fines;
                }
                player.Money -= fines;
                _game.Logs.CreateLog($"{player.Name} payed fines {fines}{_game.Rules.CurrencySymbol}");
                return true;
            }
            return false;
        }
    }
}
