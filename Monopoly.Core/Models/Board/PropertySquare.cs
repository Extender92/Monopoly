using Monopoly.Core.Events;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    public class PropertySquare : Square
    {
        public ConsoleColor Color { get; private set; }
        public PropertyGroup Group { get; }
        public int Rent { get; }
        public int RentWithColorGroup { get; }
        public int RentOneHouse { get; }
        public int RentTwoHouses { get; }
        public int RentThreeHouses { get; }
        public int RentFourHouses { get; }
        public int RentHotel { get; }
        public int BuildHouseCost { get; }
        public int BuildHotelCost { get; }
        public int Houses { get; private set; }


        public PropertySquare(PropertyGroup group, string name, int rent, int rentWithColorGroup,
               int rentOneHouse, int rentTwoHouses, int rentThreeHouses, int rentFourHouses,
               int rentHotel, int buildHouseCost, int buildHotelCost, int price, int mortgageValue, int position)
        {

            Group = group;
            Color = ToLegacyConsoleColor(group);
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

        // Compatibility overload for the existing console/test surface. New frontends should use PropertyGroup.
        public PropertySquare(ConsoleColor color, string name, int rent, int rentWithColorGroup,
               int rentOneHouse, int rentTwoHouses, int rentThreeHouses, int rentFourHouses,
               int rentHotel, int buildHouseCost, int buildHotelCost, int price, int mortgageValue, int position)
            : this(ToPropertyGroup(color), name, rent, rentWithColorGroup, rentOneHouse, rentTwoHouses,
                rentThreeHouses, rentFourHouses, rentHotel, buildHouseCost, buildHotelCost, price, mortgageValue, position)
        {
            Color = color;
        }

        private static PropertyGroup ToPropertyGroup(ConsoleColor color) => color switch
        {
            ConsoleColor.DarkGray => PropertyGroup.Brown,
            ConsoleColor.DarkCyan => PropertyGroup.LightBlue,
            ConsoleColor.Magenta => PropertyGroup.Pink,
            ConsoleColor.DarkYellow => PropertyGroup.Orange,
            ConsoleColor.DarkRed => PropertyGroup.Red,
            ConsoleColor.Yellow => PropertyGroup.Yellow,
            ConsoleColor.DarkGreen => PropertyGroup.Green,
            ConsoleColor.DarkBlue => PropertyGroup.DarkBlue,
            _ => PropertyGroup.Brown
        };

        private static ConsoleColor ToLegacyConsoleColor(PropertyGroup group) => group switch
        {
            PropertyGroup.Brown => ConsoleColor.DarkGray,
            PropertyGroup.LightBlue => ConsoleColor.DarkCyan,
            PropertyGroup.Pink => ConsoleColor.Magenta,
            PropertyGroup.Orange => ConsoleColor.DarkYellow,
            PropertyGroup.Red => ConsoleColor.DarkRed,
            PropertyGroup.Yellow => ConsoleColor.Yellow,
            PropertyGroup.Green => ConsoleColor.DarkGreen,
            PropertyGroup.DarkBlue => ConsoleColor.DarkBlue,
            _ => ConsoleColor.DarkGray
        };

        internal override void LandOn(Player player, Game game)
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
                HandleRentPayment(player, game);
            }
        }

        private void HandleRentPayment(Player player, Game game)
        {
            int rent = CalculateRent(game.Board.GetAllPropertySquares());

            game.Handler.TryResolvePayment(player, rent, Owner, $"Could not afford rent of {rent}");
        }

        private int CalculateRent(IReadOnlyList<PropertySquare> propertySquares)
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

                case 0 when OwnerHasColorGroup(propertySquares):
                    return RentWithColorGroup;

                default:
                    return Rent;
            }
        }

        public bool OwnerHasColorGroup(IReadOnlyList<PropertySquare> propertySquares)
        {
            var propertiesInColorGroup = propertySquares
                .Where(property => property.Group == Group);

            return propertiesInColorGroup.All(property => property.Owner == Owner);
        }

        public string GetHouseCountAsString()
        {
            const int hotelThreshold = 5;
            if (Houses == 0)
            {
                return "no Houses or Hotels";
            }
            else if (Houses == 1)
            {
                return "1 House";
            }
            else if (Houses == hotelThreshold)
            {
                return "1 Hotel";
            }
            return $"{Houses} Houses";
        }

        internal void AddBuilding()
        {
            if (Owner is null || IsMortgage || Houses >= 5)
                throw new InvalidOperationException("The property cannot receive another building.");
            Houses++;
        }

        internal void RemoveBuilding()
        {
            if (Houses <= 0)
                throw new InvalidOperationException("The property has no building to remove.");
            Houses--;
        }

        internal void ClearBuildings() => Houses = 0;

        internal override void RestoreState(Player? owner, bool isMortgage, int houses)
        {
            if (houses is < 0 or > 5)
                throw new ArgumentOutOfRangeException(nameof(houses));
            if (owner is null && (isMortgage || houses > 0))
                throw new ArgumentException("Mortgages and buildings require an owner.", nameof(owner));
            if (isMortgage && houses > 0)
                throw new ArgumentException("A mortgaged property cannot contain buildings.", nameof(isMortgage));
            base.RestoreState(owner, isMortgage, 0);
            Houses = houses;
        }
    }
}
