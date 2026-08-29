using Monopoly.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace Monopoly.Core.Models.Board
{
    public sealed class GameBoard
    {
        private readonly List<Square> _squares;
        private readonly ReadOnlyCollection<Square> _squaresView;
        public IReadOnlyList<Square> Squares => _squaresView;

        public GameBoard(GameRules gameRules)
        {
            ArgumentNullException.ThrowIfNull(gameRules);
            _squares = SquareBuilder.GetBoardSquares(gameRules);
            _squaresView = _squares.AsReadOnly();
        }

        internal void HandlePlayerLandingOnSquare(Player player, Game game)
        {
            Squares.First(s => s.Position == player.Position).LandOn(player, game);
        }

        public Square GetSquareAtPosition(int position)
        {
            return Squares.First(s => s.Position == position);
        }

        public IReadOnlyList<T> GetAllSquaresOfType<T>() where T : Square
        {
            return Squares.OfType<T>().ToList().AsReadOnly();
        }

        public IReadOnlyList<PropertySquare> GetAllPropertySquares()
        {
            return GetAllSquaresOfType<PropertySquare>();
        }

        public IReadOnlyList<PropertySquare> GetAllPlayerOwnedPropertySquares(Player player)
        {
            IReadOnlyList<PropertySquare> propertySquares = GetAllPropertySquares();
            return propertySquares.Where(s => s.Owner == player).ToList().AsReadOnly();
        }

        public IReadOnlyList<PropertySquare> GetAllPropertySquaresPlayerCanBuyHousesIn(Player player)
        {
            IReadOnlyList<PropertySquare> playerOwnedPropertySquares = GetAllPlayerOwnedPropertySquares(player);
            IReadOnlyList<PropertySquare> propertySquares = GetAllPropertySquares();

            return playerOwnedPropertySquares
                .Where(property => property.OwnerHasGroup(propertySquares))
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyList<PropertySquare> GetAllPropertySquaresPlayerCanSellHousesIn(Player player)
        {
            IReadOnlyList<PropertySquare> playerOwnedPropertySquares = GetAllPlayerOwnedPropertySquares(player);

            return playerOwnedPropertySquares
                .Where(property => property.Houses > 0)
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyList<Square> GetAllMortgageableSquares()
        {
            IReadOnlyList<PropertySquare> propertySquares = GetAllSquaresOfType<PropertySquare>();
            IReadOnlyList<RailroadSquare> railroadSquares = GetAllSquaresOfType<RailroadSquare>();
            IReadOnlyList<UtilitySquare> utilitySquares = GetAllSquaresOfType<UtilitySquare>();
            List<Square> allMortgageableSquares = new List<Square>();
            allMortgageableSquares.AddRange(propertySquares);
            allMortgageableSquares.AddRange(railroadSquares);
            allMortgageableSquares.AddRange(utilitySquares);
            return allMortgageableSquares.AsReadOnly();
        }

        public IReadOnlyList<Square> GetAllMortgageableSquaresForPlayer(Player player)
        {
            IReadOnlyList<Square> allMortgageableSquares = GetAllMortgageableSquares();
            return allMortgageableSquares.Where(s => s.Owner == player).ToList().AsReadOnly();
        }

        public IReadOnlyList<Square> GetPlayerMortgageableSquares(Player player)
        {
            IReadOnlyList<Square> playerOwnedSquares = GetAllMortgageableSquaresForPlayer(player);

            var playerMortgageableSquares = playerOwnedSquares
                .OfType<PropertySquare>()
                .Where(property => property.Houses <= 0 &&
                                   playerOwnedSquares.OfType<PropertySquare>()
                                   .Where(p => p.GroupId == property.GroupId)
                                   .All(p => p.Houses <= 0))
                .Cast<Square>()
                .Concat(playerOwnedSquares.Where(s => !(s is PropertySquare)))
                .ToList()
                .AsReadOnly();

            return playerMortgageableSquares;
        }

        public IReadOnlyList<Square> GetPlayerMortgagedSquares(Player player)
        {
            IReadOnlyList<Square> playerOwnedSquares = GetAllMortgageableSquaresForPlayer(player);
            return playerOwnedSquares.Where(s => s.IsMortgage).ToList().AsReadOnly();
        }

        public IReadOnlyList<Square> GetPlayerUnmortgagedSquares(Player player)
        {
            IReadOnlyList<Square> playerMortgageableSquares = GetPlayerMortgageableSquares(player);
            return playerMortgageableSquares.Where(s => !s.IsMortgage).ToList().AsReadOnly();
        }
    }
}
