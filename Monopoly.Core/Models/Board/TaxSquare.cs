using Monopoly.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class TaxSquare : Square
    {
        public TaxSquare(int position, int tax, string name, string info)
        {
            Position = position;
            Name = name;
            Info = info;
            Price = tax;
        }

        public override void LandOn(Player player)
        {
            while(!Game.Transaction.PayTax(player, Price))
            {
                if (Game.IsPlayerBankrupt(player, Price))
                {
                    Game.HandlePlayerBankruptcy(player);
                    return;
                }

                GameEvents.InvokePlayerInsufficientFunds(player, Price);
            }
        }
    }
}
