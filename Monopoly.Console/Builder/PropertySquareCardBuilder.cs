using Monopoly.Console.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.Builder
{
    internal class PropertySquareCardBuilder
    {
        private PropertySquareCard card;

        public PropertySquareCardBuilder()
        {
            card = new PropertySquareCard();
        }

        public PropertySquareCardBuilder SetName(string name)
        {
            card.Name = name;
            return this;
        }

        public PropertySquareCardBuilder SetProp(List<string> prop)
        {
            card.Prop = prop;
            return this;
        }

        public PropertySquareCardBuilder SetRent(List<string> rent)
        {
            card.Rent = rent;
            return this;
        }


        public PropertySquareCard Build()
        {
            return card;
        }
    }
}
