using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models
{
    internal class Street(ConsoleColor color, string name, int rent, int rentWithColor,
                    int rentOneHouse, int rentTwoHouses, int rentThreeHouses, int rentFourHouses,
                    int rentHotels, int housesCost, int hotelsCost, int price, int mortgageValue) 
    {
        public ConsoleColor Color { get; set; } = color;
        public string Name { get; set; } = name;
        public int Rent { get; set; } = rent;
        public int RentWithColor { get; set; } = rentWithColor;
        public int RentOneHouses { get; set; } = rentOneHouse;
        public int RentTwoHouses { get; set; } = rentTwoHouses;
        public int RentThreeHouses { get; set; } = rentThreeHouses;
        public int RentFourHouses { get; set; } = rentFourHouses;
        public int RentHotels { get; set; } = rentHotels;
        public int HousesCost { get; set; } = housesCost;
        public int HotelsCost { get; set; } = hotelsCost;
        public int Price { get; set; } = price;
        public int MortgageValue { get; set; } = mortgageValue;
    }

}
