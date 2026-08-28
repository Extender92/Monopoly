using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    internal sealed class Transaction
    {
        private readonly Game CurrentGame;

        internal Transaction(Game game)
        {
            CurrentGame = game ?? throw new ArgumentNullException(nameof(game));
        }

        internal void PlayerGetSalary(Player player)
        {
            ValidatePlayer(player);
            player.Credit(CurrentGame.Rules.Salary);
            CurrentGame.LogWriter.CreateLog($"{player.Name} collected salary {CurrentGame.Rules.Salary}{CurrentGame.Rules.CurrencySymbol}.");
            CurrentGame.PublishNotification(new PlayerInformationChangedNotification());
        }

        internal bool BuyPurchasableSquare(Player player, Square square)
        {
            ValidatePlayer(player);
            ValidateSquare(square);
            if (!player.IsBankrupt && square.Owner is null && square.Price >= 0 && player.Money >= square.Price)
            {
                player.TryDebit(square.Price);
                square.AssignOwner(player);
                CurrentGame.LogWriter.CreateLog($"{player.Name} bought {square.Name} for {square.Price}{CurrentGame.Rules.CurrencySymbol}.");
                CurrentGame.PublishNotification(new PlayerInformationChangedNotification());
                return true;
            }
            return false;
        }

        internal bool PayRentFromPlayerToPlayer(Player fromPlayer, int rent, Player toPlayer)
        {
            ValidatePayment(fromPlayer, toPlayer, rent);
            if (fromPlayer.Money >= rent && toPlayer.Money <= int.MaxValue - rent)
            {
                fromPlayer.TryDebit(rent);
                toPlayer.Credit(rent);
                CurrentGame.LogWriter.CreateLog($"{fromPlayer.Name} payed rent {rent}{CurrentGame.Rules.CurrencySymbol} to {toPlayer.Name}.");
                CurrentGame.PublishNotification(new PlayerInformationChangedNotification());
                return true;
            }
            return false;
        }

        internal bool PayPlayerFromPlayer(Player fromPlayer, int sumToPay, Player toPlayer)
        {
            ValidatePayment(fromPlayer, toPlayer, sumToPay);
            if (fromPlayer.Money >= sumToPay && toPlayer.Money <= int.MaxValue - sumToPay)
            {
                fromPlayer.TryDebit(sumToPay);
                toPlayer.Credit(sumToPay);
                CurrentGame.LogWriter.CreateLog($"{fromPlayer.Name} payed {sumToPay}{CurrentGame.Rules.CurrencySymbol} to {toPlayer.Name}.");
                CurrentGame.PublishNotification(new PlayerInformationChangedNotification());
                return true;
            }
            return false;
        }

        internal bool TryMortgageProperty(Player player, Square square)
        {
            ValidatePlayer(player);
            ValidateSquare(square);
            if (player.IsBankrupt || square.Owner != player || square.IsMortgage || square.MortgageValue < 0)
                return false;
            if (!CurrentGame.Board.GetPlayerUnmortgagedSquares(player).Contains(square))
                return false;
            if (player.Money > int.MaxValue - square.MortgageValue)
                return false;

            player.Credit(square.MortgageValue);
            square.PlaceMortgage();
            CurrentGame.LogWriter.CreateLog($"{player.Name} collected money from bank {square.MortgageValue}{CurrentGame.Rules.CurrencySymbol}.");
            CurrentGame.PublishNotification(new PlayerInformationChangedNotification());
            CurrentGame.LogWriter.CreateLog($"{player.Name} mortgage {square.Name} for {square.MortgageValue}{CurrentGame.Rules.CurrencySymbol}.");
            CurrentGame.PublishNotification(new PlayerInformationChangedNotification());
            return true;
        }

        internal bool TryRepayMortgageProperty(Player player, Square square)
        {
            ValidatePlayer(player);
            ValidateSquare(square);
            if (player.IsBankrupt || square.Owner != player || !square.IsMortgage)
                return false;

            int interestRate = CurrentGame.Rules.MortgageInterestRate;
            int sumToPay = (int)(square.MortgageValue * (1 + interestRate / 100.0));
            if (sumToPay <= player.Money)
            {
                player.TryDebit(sumToPay);
                square.RepayMortgage();
                CurrentGame.LogWriter.CreateLog($"{player.Name} repayed mortgage {sumToPay}{CurrentGame.Rules.CurrencySymbol} for {square.Name}.");
                CurrentGame.PublishNotification(new PlayerInformationChangedNotification());
                return true;
            }
            return false;
        }

        internal bool TryBuyPropertyHouse(Player player, PropertySquare property)
        {
            ValidatePlayer(player);
            ValidateSquare(property);
            if (player.IsBankrupt || property.Owner != player || property.IsMortgage ||
                !CurrentGame.Board.GetAllPropertySquaresPlayerCanBuyHousesIn(player).Contains(property))
                return false;

            int sumToPay = (property.Houses == 4 ? property.BuildHotelCost : property.BuildHouseCost);
            if (property.Houses > 4 || sumToPay > player.Money) return false;

            player.TryDebit(sumToPay);
            property.AddBuilding();

            string purchasedItem = property.Houses == 5 ? "Hotel" : "House";
            string houseCountStr = property.GetHouseCountAsString();
            CurrentGame.LogWriter.CreateLog($"{player.Name} bought a {purchasedItem} for {sumToPay}{CurrentGame.Rules.CurrencySymbol} and now has {houseCountStr} on {property.Name}.");
            CurrentGame.PublishNotification(new PlayerInformationChangedNotification());
            return true;
        }

        internal bool TrySellPropertyHouse(Player player, PropertySquare property)
        {
            ValidatePlayer(player);
            ValidateSquare(property);
            if (player.IsBankrupt || property.Owner != player || property.Houses <= 0)
                return false;

            int sumToGet = (property.Houses == 5 ? property.BuildHotelCost : property.BuildHouseCost) / 2;
            if (player.Money > int.MaxValue - sumToGet)
                return false;
            player.Credit(sumToGet);

            string soldItem = property.Houses == 5 ? "Hotel" : "House";
            property.RemoveBuilding();

            string houseCountStr = property.GetHouseCountAsString();
            CurrentGame.LogWriter.CreateLog($"{player.Name} sold a {soldItem} for {sumToGet}{CurrentGame.Rules.CurrencySymbol} and now has {houseCountStr} on {property.Name}.");
            CurrentGame.PublishNotification(new PlayerInformationChangedNotification());
            return true;
        }

        internal bool TryBuyPurchasableSquareAfterDecision(Player player, Square square)
        {
            ValidatePlayer(player);
            ValidateSquare(square);
            if (player.IsBankrupt || square.Owner is not null || square.Price < 0 ||
                !CurrentGame.Handler.CanAffordWithAssets(player, square.Price))
                return false;

            if (square.Price > player.Money && !AskToManagePropertiesForBuyingSquare(player, square))
                return false;

            return BuyPurchasableSquare(player, square);
        }

        private bool AskToManagePropertiesForBuyingSquare(Player player, Square square)
        {
            while (square.Price > player.Money)
            {
                int moneyBefore = player.Money;
                if (!CurrentGame.Decisions.ResolveInsufficientFunds((Game)CurrentGame, player, square.Price))
                    return false;
                if (player.Money <= moneyBefore)
                    return false;
            }
            return true;
        }

        internal void GetMoneyFromBank(Player player, int sum)
        {
            ValidatePlayer(player);
            if (sum < 0) throw new ArgumentOutOfRangeException(nameof(sum));
            player.Credit(sum);
            CurrentGame.LogWriter.CreateLog($"{player.Name} collected money from bank {sum}{CurrentGame.Rules.CurrencySymbol}.");
            CurrentGame.PublishNotification(new PlayerInformationChangedNotification());
        }

        internal void PayMoneyToBank(Player player, int sum)
        {
            ValidatePlayer(player);
            if (sum < 0) throw new ArgumentOutOfRangeException(nameof(sum));
            if (!player.TryDebit(sum)) return;
            CurrentGame.LogWriter.CreateLog($"{player.Name} payed {sum}{CurrentGame.Rules.CurrencySymbol} to the Bank.");
            CurrentGame.PublishNotification(new PlayerInformationChangedNotification());
        }

        internal bool PayTax(Player player, int sum)
        {
            ValidatePlayer(player);
            if (sum < 0) throw new ArgumentOutOfRangeException(nameof(sum));
            if (player.TryDebit(sum))
            {
                CurrentGame.PublishNotification(new PlayerInformationChangedNotification());
                return true;
            }
            return false;
        }

        internal bool PayFines(Player player, int fines)
        {
            ValidatePlayer(player);
            if (fines < 0) throw new ArgumentOutOfRangeException(nameof(fines));
            if (fines <= player.Money)
            {
                if (CurrentGame.Rules.FreeParking == GameRules.Parking.Fines)
                {
                    CurrentGame.AddFines(fines);
                }
                player.TryDebit(fines);
                CurrentGame.LogWriter.CreateLog($"{player.Name} payed fines of {fines}{CurrentGame.Rules.CurrencySymbol}.");
                CurrentGame.PublishNotification(new PlayerInformationChangedNotification());
                return true;
            }
            return false;
        }

        private void ValidatePlayer(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            if (!CurrentGame.ContainsPlayer(player))
                throw new ArgumentException("The player does not belong to this game.", nameof(player));
        }

        private void ValidateSquare(Square square)
        {
            ArgumentNullException.ThrowIfNull(square);
            if (!CurrentGame.ContainsSquare(square))
                throw new ArgumentException("The square does not belong to this game.", nameof(square));
        }

        private void ValidatePayment(Player fromPlayer, Player toPlayer, int amount)
        {
            ValidatePlayer(fromPlayer);
            ValidatePlayer(toPlayer);
            if (ReferenceEquals(fromPlayer, toPlayer))
                throw new ArgumentException("A player cannot pay itself.", nameof(toPlayer));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        }
    }
}
