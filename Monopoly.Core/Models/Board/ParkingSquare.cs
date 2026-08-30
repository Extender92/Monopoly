using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

    internal class ParkingSquare : Square
{
    public ParkingSquare(int position, PresentationMetadata presentation)
        : base(position, presentation)
    {
    }

    internal override void LandOn(Player player, Game game)
    {
        switch (game.Rules.FreeParking)
        {
            case GameRules.Parking.None:
                break;
            case GameRules.Parking.SetFee:
                game.Transactions.GetMoneyFromBank(player, (int)GameRules.Parking.SetFee);
                break;
            case GameRules.Parking.Fines:
                game.Transactions.GetMoneyFromBank(player, game.TakeFines());
                break;
            default:
                throw new NotImplementedException($"Rule {game.Rules.FreeParking} is not implemented.");
        }
    }
}
