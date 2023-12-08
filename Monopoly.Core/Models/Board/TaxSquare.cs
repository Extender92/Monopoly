using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class TaxSquare : Square
    {
        public int Price { get; set; }
        public TaxSquare(int position, int price, string info)
        {
            Position = position;
            Info = info;
            Price = price;
        }

        public override void LandOn(Player player)
        {
            player.Money -= this.Price;
          
        }
    }
}
