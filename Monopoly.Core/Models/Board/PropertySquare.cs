using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

public class PropertySquare : Square
{
    public PropertySquare(
        GroupId groupId,
        PresentationMetadata groupPresentation,
        PresentationMetadata presentation,
        int rent,
        int rentWithGroup,
        int rentOneHouse,
        int rentTwoHouses,
        int rentThreeHouses,
        int rentFourHouses,
        int rentHotel,
        int buildHouseCost,
        int buildHotelCost,
        int price,
        int mortgageValue,
        int position)
        : base(position, presentation)
    {
        if (!groupId.IsValid) throw new ArgumentException("The property group ID is invalid.", nameof(groupId));
        GroupPresentation = groupPresentation ?? throw new ArgumentNullException(nameof(groupPresentation));
        GroupId = groupId;
        Rent = rent;
        RentWithGroup = rentWithGroup;
        RentOneHouse = rentOneHouse;
        RentTwoHouses = rentTwoHouses;
        RentThreeHouses = rentThreeHouses;
        RentFourHouses = rentFourHouses;
        RentHotel = rentHotel;
        BuildHouseCost = buildHouseCost;
        BuildHotelCost = buildHotelCost;
        Price = price;
        MortgageValue = mortgageValue;
    }

    internal PropertySquare(
        LegacyPropertyGroup group,
        string name,
        int rent,
        int rentWithGroup,
        int rentOneHouse,
        int rentTwoHouses,
        int rentThreeHouses,
        int rentFourHouses,
        int rentHotel,
        int buildHouseCost,
        int buildHotelCost,
        int price,
        int mortgageValue,
        int position)
        : this(
            LegacyPresentationFactory.Group(group).Id,
            LegacyPresentationFactory.Group(group).Presentation,
            LegacyPresentationFactory.Space(position, name, colorToken: LegacyPresentationFactory.Group(group).Presentation.ColorToken),
            rent,
            rentWithGroup,
            rentOneHouse,
            rentTwoHouses,
            rentThreeHouses,
            rentFourHouses,
            rentHotel,
            buildHouseCost,
            buildHotelCost,
            price,
            mortgageValue,
            position)
    {
    }

    public GroupId GroupId { get; }
    public PresentationToken GroupPresentationToken => GroupPresentation.Token;
    internal PresentationMetadata GroupPresentation { get; }
    public int Rent { get; }
    public int RentWithGroup { get; }
    public int RentOneHouse { get; }
    public int RentTwoHouses { get; }
    public int RentThreeHouses { get; }
    public int RentFourHouses { get; }
    public int RentHotel { get; }
    public int BuildHouseCost { get; }
    public int BuildHotelCost { get; }
    public int Houses { get; private set; }

    internal override void LandOn(Player player, Game game)
    {
        if (Owner is null)
        {
            if (game.Handler.CanAffordWithAssets(player, Price))
                game.RequestPropertyPurchase(player, this);
        }
        else if (!IsMortgage && Owner != player)
        {
            HandleRentPayment(player, game);
        }
    }

    private void HandleRentPayment(Player player, Game game)
    {
        int rent = CalculateRent(game.Board.GetAllPropertySquares());
        game.Handler.TryResolvePayment(player, rent, Owner, $"Could not afford rent of {rent}");
    }

    private int CalculateRent(IReadOnlyList<PropertySquare> propertySquares) => Houses switch
    {
        1 => RentOneHouse,
        2 => RentTwoHouses,
        3 => RentThreeHouses,
        4 => RentFourHouses,
        5 => RentHotel,
        0 when OwnerHasGroup(propertySquares) => RentWithGroup,
        _ => Rent
    };

    public bool OwnerHasGroup(IReadOnlyList<PropertySquare> propertySquares)
    {
        ArgumentNullException.ThrowIfNull(propertySquares);
        IEnumerable<PropertySquare> propertiesInGroup = propertySquares.Where(property => property.GroupId == GroupId);
        return propertiesInGroup.All(property => property.Owner == Owner);
    }

    public string GetHouseCountAsString() => Houses switch
    {
        0 => "no Houses or Hotels",
        1 => "1 House",
        5 => "1 Hotel",
        _ => $"{Houses} Houses"
    };

    internal void AddBuilding()
    {
        if (Owner is null || IsMortgage || Houses >= 5)
            throw new InvalidOperationException("The property cannot receive another building.");
        Houses++;
    }

    internal void RemoveBuilding()
    {
        if (Houses <= 0)
            throw new InvalidOperationException("The property has no building to remove.");
        Houses--;
    }

    internal void ClearBuildings() => Houses = 0;

    internal override void RestoreState(Player? owner, bool isMortgage, int houses)
    {
        if (houses is < 0 or > 5)
            throw new ArgumentOutOfRangeException(nameof(houses));
        if (owner is null && (isMortgage || houses > 0))
            throw new ArgumentException("Mortgages and buildings require an owner.", nameof(owner));
        if (isMortgage && houses > 0)
            throw new ArgumentException("A mortgaged property cannot contain buildings.", nameof(isMortgage));
        base.RestoreState(owner, isMortgage, 0);
        Houses = houses;
    }
}
