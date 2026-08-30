using Monopoly.Console.GUI;
using Monopoly.Console.Models.Board;
using Monopoly.Core;
using Monopoly.Core.Models.Board;

namespace Monopoly.Console.Builder;

internal sealed class SquareCardBuilder
{
    private readonly Game _game;
    private readonly ConsolePresentationResolver _presentation;

    internal SquareCardBuilder(Game game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _presentation = new ConsolePresentationResolver(game.Presentation);
    }

    internal List<SquareCard> BuildAllSquareCards()
    {
        List<SquareCard> cards = [];
        foreach (Square square in _game.Board.Squares)
        {
            cards.Add(square switch
            {
                PropertySquare property => BuildProperty(property),
                DrawSquare draw => Basic<DrawSquareCard>(draw),
                GoSquare go => BuildGo(go),
                GoToJailSquare goToJail => BuildGoToJail(goToJail),
                JailSquare jail => Basic<JailSquareCard>(jail),
                ParkingSquare parking => BuildParking(parking),
                RailroadSquare railroad => BuildRailroad(railroad),
                TaxSquare tax => BuildTax(tax),
                UtilitySquare utility => BuildUtility(utility),
                _ => throw new InvalidOperationException($"Unsupported square type '{square.GetType().Name}'.")
            });
        }

        return cards;
    }

    private PropertySquareCard BuildProperty(PropertySquare property) => new()
    {
        Name = Name(property),
        BoardPosition = property.Position,
        BorderColor = _presentation.GetColor(property.GroupPresentationToken),
        Prop =
        [
            "Property Price",
            "Rent",
            "Rent with group",
            "Rent with 1 house",
            "Rent with 2 houses",
            "Rent with 3 houses",
            "Rent with 4 houses",
            "Rent with hotel"
        ],
        Rent =
        [
            Amount(property.Price),
            Amount(property.Rent),
            Amount(property.RentWithGroup),
            Amount(property.RentOneHouse),
            Amount(property.RentTwoHouses),
            Amount(property.RentThreeHouses),
            Amount(property.RentFourHouses),
            Amount(property.RentHotel)
        ],
        Info = $"Mortgage Value: {Amount(property.MortgageValue)}. Houses Cost {Amount(property.BuildHouseCost)} each. " +
            $"Hotels Cost {Amount(property.BuildHotelCost)} plus 4 houses"
    };

    private GoSquareCard BuildGo(GoSquare square) => new()
    {
        BoardPosition = square.Position,
        Name = Name(square),
        Info = $"Collect {Amount(_game.Rules.Salary)} salary as you pass {Name(square)}"
    };

    private GoToJailSquareCard BuildGoToJail(GoToJailSquare square) => new()
    {
        BoardPosition = square.Position,
        Name = Name(square),
        Info = $"Move to the detention space. Do not collect {Amount(_game.Rules.Salary)}"
    };

    private ParkingSquareCard BuildParking(ParkingSquare square)
    {
        string info = _game.Rules.FreeParking switch
        {
            GameRules.Parking.SetFee => $"Collect {Amount((int)GameRules.Parking.SetFee)}",
            GameRules.Parking.Fines => "Collect fines",
            _ => Description(square)
        };

        return new ParkingSquareCard { Name = Name(square), BoardPosition = square.Position, Info = info };
    }

    private RailroadSquareCard BuildRailroad(RailroadSquare square) => new()
    {
        Name = Name(square),
        BoardPosition = square.Position,
        Prop = ["Property Price", "Rent", "If 2 are owned", "If 3 are owned", "If 4 are owned"],
        Rent =
        [
            Amount(square.Price),
            Amount(square.RentOneStation),
            Amount(square.RentTwoStation),
            Amount(square.RentThreeStation),
            Amount(square.RentFourStation)
        ],
        Info = $"Mortgage Value: {Amount(square.MortgageValue)}"
    };

    private TaxSquareCard BuildTax(TaxSquare square) => new()
    {
        Name = Name(square),
        BoardPosition = square.Position,
        Info = $"Pay {Amount(square.Price)}"
    };

    private UtilitySquareCard BuildUtility(UtilitySquare square) => new()
    {
        Name = Name(square),
        BoardPosition = square.Position,
        Info = $"With one service property, the fee is {square.RentOneUtility} times the dice total; with two, " +
            $"it is {square.RentTwoUtility} times the dice total. Mortgage value: {Amount(square.MortgageValue)}"
    };

    private T Basic<T>(Square square) where T : SquareCard, new() => new()
    {
        Name = Name(square),
        BoardPosition = square.Position,
        Info = Description(square)
    };

    private string Name(Square square) => _presentation.GetDisplayText(square.PresentationToken);
    private string Description(Square square) => _presentation.GetDescription(square.PresentationToken);
    private string Amount(int value) => _presentation.FormatAmount(value, _game.Rules.PrimaryResourcePresentationToken);
}
