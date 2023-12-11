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

        public override void LandOn(Player player, Game game)
        {
            if (game.Rules.DoubleOnGo)
                game.Transactions.PlayerGetSalary(player);
        }
    }
}