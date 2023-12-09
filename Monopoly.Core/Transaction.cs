using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    internal class Transaction
    {
        internal void PlayerGetSalary(Player player)
        {
            player.Money += Game.Rules.Salary;
        }

        public bool BuyPurchasableSquare(Square square)
        {
            //Money -= square.Price;
            //square.Owner = this;
            return false;
        }

        public bool PayRentFromPlayerToPlayer(Player fromPlayer, int rent, Player toPlayer)
        {
            if (fromPlayer.Money >= rent)
            {
                fromPlayer.Money -= rent;
                toPlayer.Money += rent;
                return true;
            }
            return false;
        }

        public void MortgageProperty()
        {

        }

        public bool RepayMortgageProperty()
        {
            return false;
        }
    }
}
