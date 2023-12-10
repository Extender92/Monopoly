namespace Monopoly.Core.Models.Board
{
    internal class ParkingSquare : Square
    {
        public ParkingSquare(int position, string info)
        {
            Position = position;
            Info = info;
        }

        public override void LandOn(Player player)
        {
            switch (Game.Rules.FreeParking)
            {
                case GameRules.Parking.Classic:
                    // Nothing Happens In Classic
                    break;

                case GameRules.Parking.SetFee:
                    Game.Transaction.GetMoneyFromBank(player, (int)GameRules.Parking.SetFee);
                    break;

                case GameRules.Parking.Fines:
                    Game.Transaction.GetMoneyFromBank(player, Game.Fines);
                    Game.Fines = 0;
                    break;

                default:
                    throw new NotImplementedException($"Rule {Game.Rules.FreeParking} is not implemented.");
            }
        }
    }
}