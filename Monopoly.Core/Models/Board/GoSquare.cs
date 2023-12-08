namespace Monopoly.Core.Models.Board
{
    internal class GoSquare : Square
    {

        public GoSquare(int position, string info)
        {
            Position = position;
            Info = info;
        }
        public override void LandOn(Player player)
        {
            
            // Logic for when a player lands on a GoSquare
        }
    }
}