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
        public int RentWithColor { get; set; }
        public int RentOneHouses { get; set; }
        public int RentTwoHouses { get; set; }
        public int RentThreeHouses { get; set; }
        public int RentFourHouses { get; set; }
        public int RentHotels { get; set; }
        public int HousesCost { get; set; }
        public int HotelsCost { get; set; }
        public int Price { get; set; }
        public int MortgageValue { get; set; }

        public Player Owner { get; set; }

        public PropertySquare(ConsoleColor color, string name, int rent, int rentWithColor,
               int rentOneHouse, int rentTwoHouses, int rentThreeHouses, int rentFourHouses,
               int rentHotels, int housesCost, int hotelsCost, int price, int mortgageValue, int position)
        {

            Color = color;
            Name = name;
            Rent = rent;
            RentWithColor = rentWithColor;
            RentOneHouses = rentOneHouse;
            RentTwoHouses = rentTwoHouses;
            RentThreeHouses = rentThreeHouses;
            RentFourHouses = rentFourHouses;
            RentHotels = rentHotels;
            HousesCost = housesCost;
            HotelsCost = hotelsCost;
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
