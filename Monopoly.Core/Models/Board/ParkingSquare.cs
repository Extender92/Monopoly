namespace Monopoly.Core.Models.Board
{
    internal class ParkingSquare : Square
    {
        public ParkingSquare(int position, string info)
        {
            Position = position;
            Info = info;
        }

        public override void LandOn(Player player)
        {
            // Logic for when a player lands on
        }
    }
}