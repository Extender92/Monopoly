using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board
{
    internal class TaxSquare : Square
    {
        public TaxSquare(int position, int tax, PresentationMetadata presentation)
            : base(position, presentation)
        {
            Price = tax;
        }

        internal override void LandOn(Player player, Game game)
        {
            game.Handler.TryResolvePayment(player, Price, null, $"Could not afford tax of {Price}");
        }
    }
}
