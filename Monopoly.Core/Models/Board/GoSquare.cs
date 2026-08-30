using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

    internal class GoSquare : Square
{
    public GoSquare(int position, PresentationMetadata presentation)
        : base(position, presentation)
    {
    }

    internal override void LandOn(Player player, Game game)
    {
        if (game.Rules.DoubleOnGo)
            game.Transactions.PlayerGetSalary(player);
    }
}
