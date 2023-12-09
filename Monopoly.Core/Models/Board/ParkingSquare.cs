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
                    // Code for SetFee parking rules
                    break;

                case GameRules.Parking.Fines:
                    // Code for Fines parking rules
                    break;

                default:
                    throw new NotImplementedException($"Rule {Game.Rules.FreeParking} is not implemented.");
            }
        }
    }
}