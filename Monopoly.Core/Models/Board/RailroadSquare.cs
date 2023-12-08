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
        public Player Owner { get; set; }   
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
            var currentLocation = this;
            if (player != null)
            {
                RailroadSquare landedStation = Data.Data.GetRailroadSquareData(null)
                    .FirstOrDefault(station => station.Position == player.Position);

                if (landedStation != null)
                {
                    Player owner = landedStation.Owner;
                    if (owner != null && owner != player)
                    {
                        int ownedStations = CountOwnedStations(owner);
                        int rent = CalculateRent(ownedStations);
                        player.Money -= rent;
                    }
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(player), "Player cannot be null.");
            }
        }

        private int CountOwnedStations(Player player)
        {
            Data.Data.GetRailroadSquareData(null).ForEach(station => player.LandOnSquare(station));
            return Data.Data.GetRailroadSquareData(null).Count(station => station.Owner == player);
        }

        private int CalculateRent(int ownedStations)
        {
            switch (ownedStations)
            {
                case 1:
                    return 25;
                case 2:
                    return 50;
                case 3:
                    return 100;
                case 4:
                    return 200;
                default:
                    return 0; 
            }
        }
    }
}
