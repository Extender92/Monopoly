using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Data
{
    internal class SquareBuilder
    {
        internal static List<Square> GetBoardSquares(GameRules gameRules)
        {
            List<ChanceSquare> chanceSquareList = Data.GetChanceSquareData(gameRules);
            List<ComunityChestSquare> comunityChestSquareList = Data.GetComunityChestSquareData(gameRules);
            List<GoSquare> goSquareList = Data.GetGoSquareData(gameRules);
            List<GoToJailSquare> goToJailSquareList = Data.GetGoToJailSquareData(gameRules);
            List<JailSquare> jailSquareList = Data.GetJailSquareData(gameRules);
            List<ParkingSquare> parkingSquareList = Data.GetParkingSquareData(gameRules);
            List<PropertySquare> propertySquareList = Data.GetPropertySquareData(gameRules);
            List<RailroadSquare> railroadSquareList = Data.GetRailroadSquareData(gameRules);
            List<TaxSquare> taxSquareList = Data.GetTaxSquareData(gameRules);
            List<UtilitySquare> utilitySquareList = Data.GetUtilitySquareData(gameRules);

            List<Square> board =
            [
                // Add all squares to the board list
                .. chanceSquareList,
                .. comunityChestSquareList,
                .. goSquareList,
                .. goToJailSquareList,
                .. jailSquareList,
                .. parkingSquareList,
                .. propertySquareList,
                .. railroadSquareList,
                .. taxSquareList,
                .. utilitySquareList,
            ];

            // Sort the board list based on the Position property
            board = board.OrderBy(square => square.Position).ToList();

            return board;
        }
    }
}
