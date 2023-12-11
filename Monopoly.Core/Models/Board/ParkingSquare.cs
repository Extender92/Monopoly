namespace Monopoly.Core.Models.Board
{
    internal class ParkingSquare : Square
    {
        public ParkingSquare(int position, string name)
        {
            Position = position;
            Name = name;
        }

        public override void LandOn(Player player, Game game)
        {
            switch (game.Rules.FreeParking)
            {
                case GameRules.Parking.Classic:
                    // Nothing Happens In Classic
                    break;

                case GameRules.Parking.SetFee:
                    game.Transactions.GetMoneyFromBank(player, (int)GameRules.Parking.SetFee);
                    break;

                case GameRules.Parking.Fines:
                    game.Transactions.GetMoneyFromBank(player, game.Fines);
                    game.Fines = 0;
                    break;

                default:
                    throw new NotImplementedException($"Rule {game.Rules.FreeParking} is not implemented.");
            }
        }
    }
}