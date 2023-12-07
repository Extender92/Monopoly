namespace Monopoly.Core.Models.Board
{
    internal class GoSquare : Square
    {

        public GoSquare()
        {
            Position = 0;
            Info = "Go";
        }
        public override void LandOn(Player player)
        {
            
            // Logic for when a player lands on a GoSquare
        }
    }
}