using Monopoly.Core.Interface;
using Monopoly.Core.Presentation;
using Monopoly.Core.Randomness;

namespace Monopoly.Core.Models.Board
{
    internal class UtilitySquare : Square
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

        internal void LandOn(Player player, Game game, bool maxPay = false, DiceRoll? rentRoll = null)
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
                HandleRentPayment(player, game, maxPay, rentRoll);
            }
        }

        private void HandleRentPayment(Player player, Game game, bool maxPay, DiceRoll? rentRoll)
        {
            int rent = CalculateRent(game, maxPay, rentRoll ?? game.LastDiceRoll);

            game.Handler.TryResolvePayment(player, rent, Owner, $"Could not afford rent of {rent}");
        }

        private int CalculateRent(Game game, bool maxPay, DiceRoll? rentRoll)
        {
            int ownedUtility = game.Board.Squares.OfType<UtilitySquare>()
                             .Count(square => square.Owner == Owner);

            if (rentRoll is null)
                throw new InvalidOperationException("A committed dice roll is required to calculate service rent.");
            if (maxPay && rentRoll.Purpose != RandomPurpose.DedicatedRuleDice)
                throw new InvalidOperationException("The service-rent override requires a dedicated rule roll.");
            if (!maxPay && rentRoll.Purpose is not (RandomPurpose.TurnDice or RandomPurpose.DetentionDice))
                throw new InvalidOperationException("Normal service rent requires the movement roll that reached the space.");

            if (maxPay) return rentRoll.Sum * 10;

            switch (ownedUtility)
            {
                case 1:
                    return rentRoll.Sum * RentOneUtility;

                case 2:
                    return rentRoll.Sum * RentTwoUtility;

                default:
                    throw new InvalidOperationException($"Invalid number of utility owned: {ownedUtility}");
            }
        }
    }
}
