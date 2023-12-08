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

        public Player Owner { get; set; }

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
        }

        public override void LandOn(Player player)
        {
            // Logic for when a player lands on a property square
        }
    }

}
