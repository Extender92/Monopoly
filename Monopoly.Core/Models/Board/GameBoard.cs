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
        private readonly ReadOnlyDictionary<SpaceId, Square> _squaresById;
        internal IReadOnlyList<Square> Squares => _squaresView;
        public IReadOnlyList<SpaceView> Spaces => Array.AsReadOnly(_squares.Select(square => square.CreateView()).ToArray());
        public GameTrack Track { get; }

        public SpaceView GetSpace(SpaceId id) => GetSquare(id).CreateView();

        internal GameBoard(IEnumerable<Square> squares)
        {
            ArgumentNullException.ThrowIfNull(squares);
            _squares = squares.ToList();
            if (_squares.Count == 0 || _squares.Any(square => square is null))
                throw new ArgumentException("A game board requires at least one non-null space.", nameof(squares));

            for (int index = 0; index < _squares.Count; index++)
            {
                if (_squares[index].Position != index)
                    throw new ArgumentException(
                        "Space positions must be contiguous from zero and match the supplied track order.",
                        nameof(squares));
            }

            Track = new GameTrack(_squares.Select(square => square.Id));
            _squaresById = new ReadOnlyDictionary<SpaceId, Square>(
                _squares.ToDictionary(square => square.Id));
            _squaresView = _squares.AsReadOnly();
        }

        internal void HandlePlayerLandingOnSquare(Player player, Game game)
        {
            Squares.First(s => s.Position == player.Position).LandOn(player, game);
        }

        internal Square GetSquareAtPosition(int position)
        {
            return _squares[position];
        }

        internal Square GetSquare(SpaceId id) =>
            !id.IsValid
                ? throw new ArgumentException("The space ID is invalid.", nameof(id))
                : _squaresById.TryGetValue(id, out Square? square)
                ? square
                : throw new KeyNotFoundException($"Space ID '{id}' does not belong to this board.");

        internal IReadOnlyList<DeckId> ReferencedDeckIds =>
            Array.AsReadOnly(_squares.OfType<IDeckReferenceSpace>().Select(space => space.DeckId).ToArray());

        internal IReadOnlyList<T> GetAllSquaresOfType<T>() where T : Square
        {
            return Squares.OfType<T>().ToList().AsReadOnly();
        }

        internal IReadOnlyList<PropertySquare> GetAllPropertySquares()
        {
            return GetAllSquaresOfType<PropertySquare>();
        }

        internal IReadOnlyList<PropertySquare> GetAllPlayerOwnedPropertySquares(Player player)
        {
            IReadOnlyList<PropertySquare> propertySquares = GetAllPropertySquares();
            return propertySquares.Where(s => s.Owner == player).ToList().AsReadOnly();
        }

        internal IReadOnlyList<PropertySquare> GetAllPropertySquaresPlayerCanBuyHousesIn(Player player)
        {
            IReadOnlyList<PropertySquare> playerOwnedPropertySquares = GetAllPlayerOwnedPropertySquares(player);
            IReadOnlyList<PropertySquare> propertySquares = GetAllPropertySquares();

            return playerOwnedPropertySquares
                .Where(property => property.OwnerHasGroup(propertySquares))
                .ToList()
                .AsReadOnly();
        }

        internal IReadOnlyList<PropertySquare> GetAllPropertySquaresPlayerCanSellHousesIn(Player player)
        {
            IReadOnlyList<PropertySquare> playerOwnedPropertySquares = GetAllPlayerOwnedPropertySquares(player);

            return playerOwnedPropertySquares
                .Where(property => property.Houses > 0)
                .ToList()
                .AsReadOnly();
        }

        internal IReadOnlyList<Square> GetAllMortgageableSquares()
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

        internal IReadOnlyList<Square> GetAllMortgageableSquaresForPlayer(Player player)
        {
            IReadOnlyList<Square> allMortgageableSquares = GetAllMortgageableSquares();
            return allMortgageableSquares.Where(s => s.Owner == player).ToList().AsReadOnly();
        }

        internal IReadOnlyList<Square> GetPlayerMortgageableSquares(Player player)
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

        internal IReadOnlyList<Square> GetPlayerMortgagedSquares(Player player)
        {
            IReadOnlyList<Square> playerOwnedSquares = GetAllMortgageableSquaresForPlayer(player);
            return playerOwnedSquares.Where(s => s.IsMortgage).ToList().AsReadOnly();
        }

        internal IReadOnlyList<Square> GetPlayerUnmortgagedSquares(Player player)
        {
            IReadOnlyList<Square> playerMortgageableSquares = GetPlayerMortgageableSquares(player);
            return playerMortgageableSquares.Where(s => !s.IsMortgage).ToList().AsReadOnly();
        }
    }
}
