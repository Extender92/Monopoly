using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class UtilitySquare : Square
    {
        public int Price { get; set; }
        public int RentOneUtitlty { get; set; }
        public int RentTwoUtitlty {  get; set; }
        public int MortgageValue { get; set; }


        public UtilitySquare(int position, string info, int price, int rentOneUtitlty, int rentTwoUtitlty, int mortgageValue)
        {
            Position = position;
            Info = info;
            Price = price;
            RentOneUtitlty = rentOneUtitlty;
            RentTwoUtitlty = rentTwoUtitlty;
            MortgageValue = mortgageValue;
        }
        public override void LandOn(Player player)
        {
            if(this.Owner is null)
            {
                // buy utility method
            }
            else
            {
                int rent = 0;

                //Check utilitys owned 
                // one owned rent = DiceSum * OneUtitlty
                // Two owned rent = DiceSum * TwoUtitlty
                
              
            }
        }
    }
}
