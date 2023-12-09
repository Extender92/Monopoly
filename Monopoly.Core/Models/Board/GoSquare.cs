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
            if (Game.Rules.DoubleOnGo)
                Game.Transaction.PlayerGetSalary(player);
        }
    }
}