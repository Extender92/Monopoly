using Monopoly.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    public class TaxSquare : Square
    {
        public TaxSquare(int position, int tax, string name, string info)
        {
            Position = position;
            Name = name;
            Info = info;
            Price = tax;
        }

        internal override void LandOn(Player player, Game game)
        {
            game.Handler.TryResolvePayment(player, Price, null, $"Could not afford tax of {Price}");
        }
    }
}
