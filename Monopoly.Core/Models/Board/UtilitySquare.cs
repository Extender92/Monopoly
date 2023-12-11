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
        public int RentOneUtitlty { get; set; }
        public int RentTwoUtitlty {  get; set; }


        public UtilitySquare(int position, string name, int price, int rentOneUtitlty, int rentTwoUtitlty, int mortgageValue)
        {
            Position = position;
            Name = name;
            Price = price;
            RentOneUtitlty = rentOneUtitlty;
            RentTwoUtitlty = rentTwoUtitlty;
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
            else if (!IsMortgage)
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
                    int restOfPlayerMoney = Game.GetMoneyFromBancruptPlayerAndBankruptPlayer(player);
                    Game.Transaction.GetMoneyFromBank(Owner, restOfPlayerMoney);
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
                    return diceSum * RentOneUtitlty;

                case 2:
                    return diceSum * RentTwoUtitlty;

                default:
                    throw new InvalidOperationException($"Invalid number of utility owned: {ownedUtility}");
            }
        }
    }
}
