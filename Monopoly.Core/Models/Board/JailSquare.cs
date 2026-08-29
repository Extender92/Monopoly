using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

public class JailSquare : Square
{
    public JailSquare(int position, PresentationMetadata presentation)
        : base(position, presentation)
    {
    }

    internal JailSquare(int position, string name, string info, string inJailInfo)
        : this(position, LegacyPresentationFactory.Space(position, name, $"{info} || {inJailInfo}"))
    {
    }

    internal override void LandOn(Player player, Game game)
    {
    }
}
