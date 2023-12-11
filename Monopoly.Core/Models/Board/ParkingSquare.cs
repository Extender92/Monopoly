namespace Monopoly.Core.Models.Board
{
    internal class ParkingSquare : Square
    {
        public ParkingSquare(int position, string name)
        {
            Position = position;
            Name = name;
        }

        public override void LandOn(Player player)
        {
            switch (Game.Rules.FreeParking)
            {
                case GameRules.Parking.Classic:
                    // Nothing Happens In Classic
                    break;

                case GameRules.Parking.SetFee:
                    Game.Transactions.GetMoneyFromBank(player, (int)GameRules.Parking.SetFee);
                    break;

                case GameRules.Parking.Fines:
                    Game.Transactions.GetMoneyFromBank(player, Game.Fines);
                    Game.Fines = 0;
                    break;

                default:
                    throw new NotImplementedException($"Rule {Game.Rules.FreeParking} is not implemented.");
            }
        }
    }
}