using Monopoly.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class RailroadSquare : Square
    {
        public int RentOneStation {  get; set; }
        public int RentTwoStation {  get; set; }
        public int RentThreeStation {  get; set; }
        public int RentFourStation {  get; set; }


        public RailroadSquare(int position, string name, int price, int rentOneStation , int rentTwoStation, int rentThreeStation, int rentFourStation, int mortgageValue)
        {
            Position = position;
            Name = name;
            Price = price;
            RentOneStation = rentOneStation;
            RentTwoStation = rentTwoStation;
            RentThreeStation = rentThreeStation;
            RentFourStation = rentFourStation;
            MortgageValue = mortgageValue;
        }

        public override void LandOn(Player player, Game game)
        {
            if (Owner == null)
            {
                if (game.Handler.CanAffordWithAssets(player, Price))
                {
                    game.Transactions.HandleCanBuySquare(player, this);
                }
            }
            else if (!IsMortgage)
            {
                HandleRentPayment(player, game);
            }
        }

        private void HandleRentPayment(Player player, Game game)
        {
            int rent = CalculateRent(game.Board.Squares);

            while (!game.Transactions.PayRentFromPlayerToPlayer(player, rent, Owner))
            {
                if (game.Handler.IsPlayerBankrupt(player, rent))
                {
                    int restOfPlayerMoney = game.Handler.GetMoneyFromBankruptPlayerAndBankruptPlayer(player);
                    game.Transactions.GetMoneyFromBank(Owner, restOfPlayerMoney);
                    break;
                }

                GameEvents.InvokePlayerInsufficientFunds(player, Price);
            }
        }

        private int CalculateRent(List<Square> squares)
        {
            int ownedStations = squares.OfType<RailroadSquare>()
                         .Count(square => square.Owner == Owner);

            Dictionary<int, int> stationRents = new Dictionary<int, int>
            {
                { 1, RentOneStation },
                { 2, RentTwoStation },
                { 3, RentThreeStation },
                { 4, RentFourStation }
            };

            // Use TryGetValue to get the rent based on the number of owned stations
            if (stationRents.TryGetValue(ownedStations, out var rent))
            {
                return rent;
            }
            else
            {
                throw new InvalidOperationException($"Invalid number of stations owned: {ownedStations}");
            }
        }
    }
}
