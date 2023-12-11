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
            int rent = CalculateRent(game);

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

        private int CalculateRent(Game game)
        {
            int ownedUtility = game.Board.Squares.OfType<UtilitySquare>()
                             .Count(square => square.Owner == Owner);

            int diceSum = game.Dice.Sum(die => die.GetDieResult());

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
