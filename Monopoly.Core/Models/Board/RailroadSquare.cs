using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class RailroadSquare : Square
    {
        public int Price { get; set; }
        public int RentOneStation {  get; set; }
        public int RentTwoStation {  get; set; }
        public int RentThreeStation {  get; set; }
        public int RentFourStation {  get; set; }
        public RailroadSquare(int position, string info, int price, int rentOneStation , int rentTwoStation, int rentThreeStation, int rentFourStation)
        {
            Position = position;
            Info = info;
            Price = price;
            RentOneStation = rentOneStation;
            RentTwoStation = rentTwoStation;
            RentThreeStation = rentThreeStation;
            RentFourStation = rentFourStation;
        }


        public override void LandOn(Player player)
        {
            if (Owner == null)
            {

            }
            else
            {
                int ownedStations = 0;

                // Iterate through all the RailroadSquare instances on the board
                foreach (Square square in Game.Board.Squares)
                {
                    if (square is RailroadSquare railroadSquare)
                        // Check if the current square has the same owner and is not the current square
                        if (railroadSquare.Owner != null && railroadSquare.Owner == Owner && railroadSquare != this)
                            ownedStations++;
                }


            }
        }
    }
}
