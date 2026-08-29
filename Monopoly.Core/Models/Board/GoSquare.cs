using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

    internal class GoSquare : Square
{
    public GoSquare(int position, PresentationMetadata presentation)
        : base(position, presentation)
    {
    }

    internal GoSquare(int position, string name, string info)
        : this(position, LegacyPresentationFactory.Space(position, name, info))
    {
    }

    internal override void LandOn(Player player, Game game)
    {
        if (game.Rules.DoubleOnGo)
            game.Transactions.PlayerGetSalary(player);
    }
}
