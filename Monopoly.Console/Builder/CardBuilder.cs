using Monopoly.Console.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.Builder
{
    internal class CardBuilder
    {
        private static List<DrawCards> DrawCards;
        internal static List<Card> GetCards() 
        {
            return BuildCards();
        }
        private static List<Card> BuildCards()
        {
            var cards = new List<Card>();     
            return cards;
        }
    }
}
