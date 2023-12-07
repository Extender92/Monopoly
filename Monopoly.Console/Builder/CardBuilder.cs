using Monopoly.Console.Models;
using Monopoly.Console.Models.Board;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.Builder
{
    internal class CardBuilder
    {
        internal static List<SquareCard> BuildAllSquareCards(List<Square> squares)
        {
            List<SquareCard> squareCardList = new List<SquareCard>();

            foreach (Square square in squares)
            {

                if (square is PropertySquare property)
                {
                    squareCardList.Add(BuildPropertyCardFromProperty(property));
                }


            }


            return squareCardList;
        }

        private static PropertySquareCard BuildPropertyCardFromProperty(PropertySquare property)
        {
            var propertyCard = new PropertySquareCardBuilder()
            .SetName(property.Name)
            .SetProp(new List<string> { $"Color: {property.Color}", $"Houses Cost: {property.HousesCost}" })
            .SetRent(new List<string> { $"Rent: {property.Rent}", $"Rent with Color: {property.RentWithColor}" })
            // Add other setters as needed
            .Build();

            return propertyCard;
        }
    }
}
