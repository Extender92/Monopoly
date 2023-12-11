namespace Monopoly.Core.Models.Board
{
    internal class GoSquare : Square
    {

        public GoSquare(int position ,string name,  string info)
        {
            Position = position;
            Name = name;
            Info = info;
        }

        public override void LandOn(Player player)
        {
            if (Game.Rules.DoubleOnGo)
                Game.Transactions.PlayerGetSalary(player);
        }
    }
}