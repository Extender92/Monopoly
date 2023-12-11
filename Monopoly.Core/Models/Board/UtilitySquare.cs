using Monopoly.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class UtilitySquare : Square
    {
        public int RentOneUtility { get; set; }
        public int RentTwoUtility {  get; set; }


        public UtilitySquare(int position, string name, int price, int rentOneUtility, int rentTwoUtility, int mortgageValue)
        {
            Position = position;
            Name = name;
            Price = price;
            RentOneUtility = rentOneUtility;
            RentTwoUtility = rentTwoUtility;
            MortgageValue = mortgageValue;
        }

        public override void LandOn(Player player)
        {
            if (Owner == null)
            {
                if (Game.Handler.CanAffordWithAssets(player, Price))
                {
                    Game.Transactions.HandleCanBuySquare(player, this);
                }
            }
            else if (!IsMortgage)
            {
                HandleRentPayment(player);
            }
        }

        private void HandleRentPayment(Player player)
        {
            int rent = CalculateRent();

            while (!Game.Transactions.PayRentFromPlayerToPlayer(player, rent, Owner))
            {
                if (Game.Handler.IsPlayerBankrupt(player, rent))
                {
                    int restOfPlayerMoney = Game.Handler.GetMoneyFromBankruptPlayerAndBankruptPlayer(player);
                    Game.Transactions.GetMoneyFromBank(Owner, restOfPlayerMoney);
                    break;
                }

                GameEvents.InvokePlayerInsufficientFunds(player, Price);
            }
        }

        private int CalculateRent()
        {
            int ownedUtility = Game.Board.Squares.OfType<UtilitySquare>()
                             .Count(square => square.Owner == Owner);

            int diceSum = Game.Dice.Sum(die => die.GetDieResult());

            switch (ownedUtility)
            {
                case 1:
                    return diceSum * RentOneUtility;

                case 2:
                    return diceSum * RentTwoUtility;

                default:
                    throw new InvalidOperationException($"Invalid number of utility owned: {ownedUtility}");
            }
        }
    }
}
