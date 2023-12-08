using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class TaxSquare : Square
    {
        public int TaxAmount { get; set; }
        public TaxSquare(int position, int tax, string info)
        {
            Position = position;
            Info = info;
            TaxAmount = tax;
        }

        public override void LandOn(Player player)
        {
            player.Money -= this.TaxAmount;
          
        }
    }
}
