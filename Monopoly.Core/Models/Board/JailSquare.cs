using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

    internal class JailSquare : Square
{
    public JailSquare(int position, PresentationMetadata presentation)
        : base(position, presentation)
    {
    }

    internal override void LandOn(Player player, Game game)
    {
    }
}
