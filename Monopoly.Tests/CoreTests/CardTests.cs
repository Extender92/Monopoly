using Monopoly.Core;
using Monopoly.Core.Data;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models.FortuneCard;
using Monopoly.Core.Presentation;

namespace Monopoly.Tests.CoreTests;

public class CardTests
{
    [Theory]
    [InlineData("group.lantern", "Lantern Vale", 500, 100, 152, 150, 200, 600, 600, 1000, 2000, 4000, 2050, 5)]
    [InlineData("group.harbor", "Copper Harbor", 10, 150, 200, 300, 450, 500, 600, 1000, 2000, 3000, 2100, 4)]
    [InlineData("group.garden", "Moss Garden", 20, 150, 200, 302, 400, 500, 600, 1000, 2000, 3000, 2200, 2)]
    [InlineData("group.market", "Moon Market", 120, 150, 200, 350, 420, 500, 600, 1000, 2000, 3000, 2250, 1)]
    public void PropertySquare_preserves_authoritative_values(
        string group,
        string displayText,
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
    {
        PresentationMetadata groupPresentation = new(new PresentationToken($"presentation.{group}"), displayText: group);
        PresentationMetadata presentation = new(new PresentationToken($"space.{position}"), displayText: displayText);

        PropertySquare square = new(
            new GroupId(group), groupPresentation, presentation, rent, rentWithGroup,
            rentOneHouse, rentTwoHouses, rentThreeHouses, rentFourHouses, rentHotel,
            buildHouseCost, buildHotelCost, price, mortgageValue, position);

        Assert.Equal(new GroupId(group), square.GroupId);
        Assert.Equal(groupPresentation.Token, square.GroupPresentationToken);
        Assert.Equal(presentation.Token, square.PresentationToken);
        Assert.Equal(rent, square.Rent);
        Assert.Equal(rentWithGroup, square.RentWithGroup);
        Assert.Equal(rentOneHouse, square.RentOneHouse);
        Assert.Equal(rentTwoHouses, square.RentTwoHouses);
        Assert.Equal(rentThreeHouses, square.RentThreeHouses);
        Assert.Equal(rentFourHouses, square.RentFourHouses);
        Assert.Equal(rentHotel, square.RentHotel);
        Assert.Equal(buildHouseCost, square.BuildHouseCost);
        Assert.Equal(buildHotelCost, square.BuildHotelCost);
        Assert.Equal(price, square.Price);
        Assert.Equal(mortgageValue, square.MortgageValue);
    }

    [Fact]
    public void Legacy_property_data_remains_complete_during_transition()
    {
        List<PropertySquare> squares = Data.GetPropertySquareData(new GameRules(2, 2, 6));

        Assert.Equal(22, squares.Count);
        Assert.All(squares, square =>
        {
            Assert.True(square.Rent > 0);
            Assert.True(square.PresentationToken.IsValid);
            Assert.True(square.GroupId.IsValid);
        });
    }

    [Fact]
    public void Chance_card_exposes_stable_presentation_token()
    {
        UKChanceCard card = new("Lantern keeper grants a release pass", UKChanceCard.UKChanceCardType.GetOutOfJailFree);

        Assert.Equal("Lantern keeper grants a release pass", card.Presentation.Description);
        Assert.True(card.PresentationToken.IsValid);
    }

    [Fact]
    public void Both_legacy_primary_decks_have_complete_presentation_metadata()
    {
        Assert.All(Data.GetChanceCardData(new GameRules(2, 2, 6, GameRules.Language.UK)), AssertCard);
        Assert.All(Data.GetChanceCardData(new GameRules(2, 2, 6, GameRules.Language.US)), AssertCard);
    }

    [Fact]
    public void Both_legacy_secondary_decks_have_complete_presentation_metadata()
    {
        Assert.All(Data.GetCommunityChestCardData(new GameRules(2, 2, 6, GameRules.Language.UK)), AssertCard);
        Assert.All(Data.GetCommunityChestCardData(new GameRules(2, 2, 6, GameRules.Language.US)), AssertCard);
    }

    private static void AssertCard(IChanceCard card)
    {
        Assert.NotNull(card.Presentation.Description);
        Assert.True(card.PresentationToken.IsValid);
    }

    private static void AssertCard(ICommunityChestCard card)
    {
        Assert.NotNull(card.Presentation.Description);
        Assert.True(card.PresentationToken.IsValid);
    }
}
