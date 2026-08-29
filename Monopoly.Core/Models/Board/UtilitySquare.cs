using Monopoly.Core.Interface;
using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board
{
    public class UtilitySquare : Square
    {
        public int RentOneUtility { get; }
        public int RentTwoUtility { get; }


        public UtilitySquare(int position, PresentationMetadata presentation, int price, int rentOneUtility, int rentTwoUtility, int mortgageValue)
            : base(position, presentation)
        {
            Price = price;
            RentOneUtility = rentOneUtility;
            RentTwoUtility = rentTwoUtility;
            MortgageValue = mortgageValue;
        }

        internal UtilitySquare(int position, string name, int price, int rentOneUtility, int rentTwoUtility, int mortgageValue)
            : this(position, LegacyPresentationFactory.Space(position, name), price, rentOneUtility, rentTwoUtility, mortgageValue)
        {
        }

        internal override void LandOn(Player player, Game game)
        {
            LandOn(player, game, false);
        }

        internal void LandOn(Player player, Game game, bool maxPay = false)
        {
            if (Owner == null)
            {
                if (game.Handler.CanAffordWithAssets(player, Price))
                {
                    game.RequestPropertyPurchase(player, this);
                }
            }
            else if (!IsMortgage && Owner != player)
            {
                HandleRentPayment(player, game, maxPay);
            }
        }

        private void HandleRentPayment(Player player, Game game, bool maxPay = false)
        {
            int rent = CalculateRent(game, maxPay);

            game.Handler.TryResolvePayment(player, rent, Owner, $"Could not afford rent of {rent}");
        }

        private int CalculateRent(Game game, bool maxPay = false)
        {
            int ownedUtility = game.Board.Squares.OfType<UtilitySquare>()
                             .Count(square => square.Owner == Owner);

            int diceSum = game.Dice.Sum(die => die.GetDieResult());

            if (maxPay) return diceSum * 10;

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
