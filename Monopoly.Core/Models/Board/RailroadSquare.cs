using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board
{
    public class RailroadSquare : Square
    {
        public int RentOneStation { get; }
        public int RentTwoStation { get; }
        public int RentThreeStation { get; }
        public int RentFourStation { get; }


        public RailroadSquare(int position, PresentationMetadata presentation, int price, int rentOneStation, int rentTwoStation, int rentThreeStation, int rentFourStation, int mortgageValue)
            : base(position, presentation)
        {
            Price = price;
            RentOneStation = rentOneStation;
            RentTwoStation = rentTwoStation;
            RentThreeStation = rentThreeStation;
            RentFourStation = rentFourStation;
            MortgageValue = mortgageValue;
        }

        internal RailroadSquare(int position, string name, int price, int rentOneStation, int rentTwoStation, int rentThreeStation, int rentFourStation, int mortgageValue)
            : this(position, LegacyPresentationFactory.Space(position, name), price, rentOneStation, rentTwoStation, rentThreeStation, rentFourStation, mortgageValue)
        {
        }

        internal override void LandOn(Player player, Game game)
        {
            LandOn(player, game, false);
        }

        internal void LandOn(Player player, Game game, bool doubleRent = false)
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
                HandleRentPayment(player, game, doubleRent);
            }
        }

        private void HandleRentPayment(Player player, Game game, bool doubleRent = false)
        {
            int rent = CalculateRent(game.Board.Squares);
            if (doubleRent) rent *= 2;

            game.Handler.TryResolvePayment(player, rent, Owner, $"Could not afford rent of {rent}");
        }

        private int CalculateRent(IReadOnlyList<Square> squares)
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
