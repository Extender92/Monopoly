namespace Monopoly.Core.Models.Board
{
    internal class ParkingSquare : Square
    {
        public ParkingSquare()
        {
            Position = 20;
            Info = "Free Parking";
        }

        public override void LandOn(Player player)
        {
            // Logic for when a player lands on
        }
    }
}