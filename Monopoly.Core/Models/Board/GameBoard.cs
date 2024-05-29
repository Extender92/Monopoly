using Monopoly.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class GameBoard
    {
        internal List<Square> Squares; // Array or list of all squares on the board

        internal GameBoard(GameRules gameRules)
        {
            Squares = SquareBuilder.GetBoardSquares(gameRules);
        }

        internal void HandlePlayerLandingOnSquare(Player player, Game game)
        {
            Squares.First(s => s.Position == player.Position).LandOn(player, game);
        }

        internal Square GetSquareAtPosition(int position)
        {
            return Squares.First(s => s.Position == position);
        }

        internal List<T> GetAllSquaresOfType<T>() where T : Square
        {
            return Squares.OfType<T>().ToList();
        }

        internal List<PropertySquare> GetAllPropertySquares()
        {
            return GetAllSquaresOfType<PropertySquare>();
        }

        internal List<PropertySquare> GetAllPlayerOwnedPropertySquares(Player player)
        {
            List<PropertySquare> propertySquares = GetAllPropertySquares();
            return propertySquares.Where(s => s.Owner == player).ToList();
        }

        internal List<PropertySquare> GetAllPropertySquaresPlayerCanBuyHousesIn(Player player)
        {
            List<PropertySquare> playerOwnedPropertySquares = GetAllPlayerOwnedPropertySquares(player);
            List<PropertySquare> propertySquares = GetAllPropertySquares();

            return playerOwnedPropertySquares
                .Where(property => property.OwnerHasColorGroup(propertySquares))
                .ToList();
        }

        internal List<PropertySquare> GetAllPropertySquaresPlayerCanSellHousesIn(Player player)
        {
            List<PropertySquare> playerOwnedPropertySquares = GetAllPlayerOwnedPropertySquares(player);

            return playerOwnedPropertySquares
                .Where(property => property.Houses > 0)
                .ToList();
        }

        internal List<Square> GetAllMortgageableSquares()
        {
            List<PropertySquare> propertySquares = GetAllSquaresOfType<PropertySquare>();
            List<RailroadSquare> railroadSquares = GetAllSquaresOfType<RailroadSquare>();
            List<UtilitySquare> utilitySquares = GetAllSquaresOfType<UtilitySquare>();
            List<Square> allMortgageableSquares = new List<Square>();
            allMortgageableSquares.AddRange(propertySquares);
            allMortgageableSquares.AddRange(railroadSquares);
            allMortgageableSquares.AddRange(utilitySquares);
            return allMortgageableSquares;
        }

        internal List<Square> GetAllMortgageableSquaresForPlayer(Player player)
        {
            List<Square> allMortgageableSquares = GetAllMortgageableSquares();
            return allMortgageableSquares.Where(s => s.Owner == player).ToList();
        }

        internal List<Square> GetPlayerMortgageableSquares(Player player)
        {
            List<Square> playerOwnedSquares = GetAllMortgageableSquaresForPlayer(player);

            var playerMortgageableSquares = playerOwnedSquares
                .OfType<PropertySquare>()
                .Where(property => property.Houses <= 0 &&
                                   playerOwnedSquares.OfType<PropertySquare>()
                                   .Where(p => p.Color == property.Color)
                                   .All(p => p.Houses <= 0))
                .Cast<Square>()
                .Concat(playerOwnedSquares.Where(s => !(s is PropertySquare)))
                .ToList();

            return playerMortgageableSquares;
        }

        internal List<Square> GetPlayerMortgagedSquares(Player player)
        {
            List<Square> playerOwnedSquares = GetAllMortgageableSquaresForPlayer(player);
            return playerOwnedSquares.Where(s => s.IsMortgage).ToList();
        }

        internal List<Square> GetPlayerUnmortgagedSquares(Player player)
        {
            List<Square> playerMortgageableSquares = GetPlayerMortgageableSquares(player);
            return playerMortgageableSquares.Where(s => !s.IsMortgage).ToList();
        }
    }
}
