using Monopoly.Core.Events;
using Monopoly.Core.Interface;
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
        private IGame CurrentGame;

        // Constructor to initialize the Game reference
        internal Transaction(IGame game)
        {
            CurrentGame = game;
        }

        internal void PlayerGetSalary(Player player)
        {
            player.Money += CurrentGame.Rules.Salary;
            CurrentGame.Logs.CreateLog($"{player.Name} collected salary {CurrentGame.Rules.Salary}{CurrentGame.Rules.CurrencySymbol}.");
        }

        internal bool BuyPurchasableSquare(Player player, Square square)
        {
            if (player.Money >= square.Price)
            {
                player.Money -= square.Price;
                square.Owner = player;
                CurrentGame.Logs.CreateLog($"{player.Name} bought {square.Name} for {square.Price}{CurrentGame.Rules.CurrencySymbol}.");
                return true;
            }
            return false;
        }

        internal bool PayRentFromPlayerToPlayer(Player fromPlayer, int rent, Player toPlayer)
        {
            if (fromPlayer.Money >= rent)
            {
                fromPlayer.Money -= rent;
                toPlayer.Money += rent;
                CurrentGame.Logs.CreateLog($"{fromPlayer.Name} payed rent {rent}{CurrentGame.Rules.CurrencySymbol} to {toPlayer.Name}.");
                return true;
            }
            return false;
        }

        internal bool PayPlayerFromPlayer(Player fromPlayer, int sumToPay, Player toPlayer)
        {
            if (fromPlayer.Money >= sumToPay)
            {
                fromPlayer.Money -= sumToPay;
                toPlayer.Money += sumToPay;
                CurrentGame.Logs.CreateLog($"{fromPlayer.Name} payed {sumToPay}{CurrentGame.Rules.CurrencySymbol} to {toPlayer.Name}.");
                return true;
            }
            return false;
        }

        internal void MortgageProperty(Player player, Square square)
        {
            GetMoneyFromBank(player, square.MortgageValue);
            square.IsMortgage = true;
            CurrentGame.Logs.CreateLog($"{player.Name} mortgage {square.Name} for {square.MortgageValue}{CurrentGame.Rules.CurrencySymbol}.");
        }

        internal bool RepayMortgageProperty(Player player, Square square)
        {
            int interestRate = CurrentGame.Rules.MortgageInterestRate;
            int sumToPay = (int)(square.MortgageValue * (1 + interestRate / 100.0));
            if (sumToPay <= player.Money)
            {
                player.Money -= sumToPay;
                square.IsMortgage = false;
                CurrentGame.Logs.CreateLog($"{player.Name} repayed mortgage {sumToPay}{CurrentGame.Rules.CurrencySymbol} for {square.Name}.");
                return true;
            }
            return false;
        }

        internal bool BuyPropertyHouse(Player player, PropertySquare property)
        {
            int sumToPay = (property.Houses == 4 ? property.BuildHotelCost : property.BuildHouseCost);
            if (property.Houses > 4 || sumToPay > player.Money) return false;

            player.Money -= sumToPay;
            property.Houses++;

            string purchasedItem = property.Houses == 5 ? "Hotel" : "House";
            string houseCountStr = property.GetHouseCountAsString();
            CurrentGame.Logs.CreateLog($"{player.Name} bought a {purchasedItem} for {sumToPay}{CurrentGame.Rules.CurrencySymbol} and now has {houseCountStr} on {property.Name}.");
            return true;
        }

        internal void SellPropertyHouse(Player player, PropertySquare property)
        {
            if (property.Houses <= 0)
                throw new InvalidOperationException("Cannot sell a house when there are no houses on the property.");

            int sumToGet = (property.Houses == 5 ? property.BuildHotelCost : property.BuildHouseCost) / 2;
            player.Money += sumToGet;

            string soldItem = property.Houses == 5 ? "Hotel" : "House";
            property.Houses--;

            string houseCountStr = property.GetHouseCountAsString();
            CurrentGame.Logs.CreateLog($"{player.Name} sold a {soldItem} for {sumToGet}{CurrentGame.Rules.CurrencySymbol} and now has {houseCountStr} on {property.Name}.");
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
            CurrentGame.Logs.CreateLog($"{player.Name} collected money from bank {sum}{CurrentGame.Rules.CurrencySymbol}.");
        }

        internal void PayMoneyToBank(Player player, int sum)
        {
            if (sum < 0) return;
            player.Money -= sum;
            CurrentGame.Logs.CreateLog($"{player.Name} payed {sum}{CurrentGame.Rules.CurrencySymbol} to the Bank.");
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
                if (CurrentGame.Rules.FreeParking == GameRules.Parking.Fines)
                {
                    CurrentGame.Fines += fines;
                }
                player.Money -= fines;
                CurrentGame.Logs.CreateLog($"{player.Name} payed fines of {fines}{CurrentGame.Rules.CurrencySymbol}.");
                return true;
            }
            return false;
        }
    }
}
