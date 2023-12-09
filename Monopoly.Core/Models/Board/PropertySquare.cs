using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class PropertySquare : Square
    {
        public ConsoleColor Color { get; set; }
        public string Name { get; set; }
        public int Rent { get; set; }
        public int RentWithColorGroup { get; set; }
        public int RentOneHouse { get; set; }
        public int RentTwoHouses { get; set; }
        public int RentThreeHouses { get; set; }
        public int RentFourHouses { get; set; }
        public int RentHotel { get; set; }
        public int BuildHouseCost { get; set; }
        public int BuildHotelCost { get; set; }
        public int Price { get; set; }
        public int MortgageValue { get; set; }
        private int Houses {  get; set; }


        public PropertySquare(ConsoleColor color, string name, int rent, int rentWithColorGroup,
               int rentOneHouse, int rentTwoHouses, int rentThreeHouses, int rentFourHouses,
               int rentHotel, int buildHouseCost, int buildHotelCost, int price, int mortgageValue, int position)
        {

            Color = color;
            Name = name;
            Rent = rent;
            RentWithColorGroup = rentWithColorGroup;
            RentOneHouse = rentOneHouse;
            RentTwoHouses = rentTwoHouses;
            RentThreeHouses = rentThreeHouses;
            RentFourHouses = rentFourHouses;
            RentHotel = rentHotel;
            BuildHouseCost = buildHouseCost;
            BuildHotelCost = buildHotelCost;
            Price = price;
            MortgageValue = mortgageValue;
            Position = position;
            Houses = 0;
        }

        public override void LandOn(Player player)
        {
            if (Owner == null)
            {

            }
            else
            {
                int rent = CalculateRent();
                if (Game.Transaction.PayRentFromPlayerToPlayer(player, rent, Owner))
                {

                }
            }
        }

        public enum LandOnOutcome
        {
            PaidSuccessfully,
            InsufficientFunds,
            PlayerBuysProperty,
        }

        private int CalculateRent()
        {
            switch (Houses)
            {
                case 1:
                    return RentOneHouse;

                case 2:
                    return RentTwoHouses;

                case 3:
                    return RentThreeHouses;

                case 4:
                    return RentFourHouses;

                case 5:
                    return RentHotel;

                case 0 when OwnerHasColorGroup():
                    return RentWithColorGroup;

                default:
                    return Rent;
            }
        }

        private bool OwnerHasColorGroup()
        {
            var propertiesInColorGroup = Game.Board.Squares
                .OfType<PropertySquare>()
                .Where(property => property.Color == Color);

            return propertiesInColorGroup.All(property => property.Owner == Owner);
        }

        internal string GetHouseCountAsString()
        {
            const int hotelThreshold = 5;
            if (Houses == 0)
            {
                return "there is no house";
            }
            else if (Houses == 1)
            {
                return "1 house";
            }
            else if (Houses == hotelThreshold)
            {
                return "1 hotel";
            }
            return $"{Houses} houses";
        }
    }
}
