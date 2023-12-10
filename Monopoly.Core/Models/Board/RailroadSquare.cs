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

        public override void LandOn(Player player)
        {
            if (Owner == null)
            {
                if (Game.CanAffordWithAssets(player, Price))
                {
                    Game.Transaction.HandleCanBuySquare(player, this);
                }
            }
            else
            {
                HandleRentPayment(player);
            }
        }

        private void HandleRentPayment(Player player)
        {
            int rent = CalculateRent();

            while (!Game.Transaction.PayRentFromPlayerToPlayer(player, rent, Owner))
            {
                if (Game.IsPlayerBankrupt(player, rent))
                {
                    int restOfPlayerMoney = Game.HandlePlayerBankruptcyAndGetMoney(player);
                    Game.Transaction.GetMoneyFromBank(Owner, restOfPlayerMoney);
                    break;
                }

                GameEvents.InvokePlayerInsufficientFunds(player, Price);
            }
        }

        private int CalculateRent()
        {
            int ownedStations = Game.Board.Squares.OfType<RailroadSquare>()
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
