using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

    internal class GoToJailSquare : Square
{
    public GoToJailSquare(int position, PresentationMetadata presentation)
        : base(position, presentation)
    {
    }

    internal override void LandOn(Player player, Game game) => game.TheJail.PlayerGoToJail(player);
}
