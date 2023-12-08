using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monopoly.Core.Models.Board;

namespace Monopoly.Core.Models
{
    internal class Player(string name,int id)
    {
        public int Id { get; set; } = id;
        public string Name { get; set; } = name;
        public int Money { get; set; } = 3000;
        public int Position { get; set; } = 0;
        public bool InJail { get; set; } = false;
        //public Die DieSum { get; set; }

        public void Buy(Square square)
        {
            //Money -= square.Price;
            //square.Owner = this;
        }

        public void PayRent(Player player , int rent)
        {
            Money -= rent;
            player.Money += rent;
        }

        public void Sell(PropertySquare street)
        {
            if (street.Owner == this)
            {
                int refundAmount = street.Price;
                street.Owner = null;
                Money += refundAmount;
            }
            else
                Console.WriteLine("You do not own this street and cannot sell it.");

        }

        public void LandOnSquare(Square square)
        {
            square.LandOn(this);
        }
    }
}
