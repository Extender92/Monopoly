using System.Reflection;
using Monopoly.Console.Builder;
using Monopoly.Console.GUI;
using Monopoly.Console.Models.Board;
using Monopoly.Core;
using Monopoly.Core.Presentation;
using Monopoly.Tests.CoreTests;

namespace Monopoly.Tests.ConsoleTests;

public sealed class ConsolePresentationResolverTests
{
    [Fact]
    public void KnownAndUnknownSemanticColorsUseLocalMappingAndNeutralFallback()
    {
        PresentationToken knownAccent = new("accent.flame");
        PresentationToken unknownAccent = new("accent.lantern");
        PresentationToken knownItem = new("space.known");
        PresentationToken unknownItem = new("space.unknown");
        PresentationToken plainItem = new("space.plain");
        ProfilePresentation catalog = new(
        [
            new PresentationMetadata(knownAccent),
            new PresentationMetadata(unknownAccent),
            new PresentationMetadata(knownItem, displayText: "Ember Walk", colorToken: knownAccent),
            new PresentationMetadata(unknownItem, displayText: "Lantern Vale", colorToken: unknownAccent),
            new PresentationMetadata(plainItem)
        ]);
        ConsolePresentationResolver resolver = new(catalog);

        Assert.Equal(ConsoleColor.DarkRed, resolver.GetColor(knownItem));
        Assert.Equal(ConsoleColor.White, resolver.GetColor(unknownItem));
        Assert.Equal(ConsoleColor.White, resolver.GetColor(plainItem));
        Assert.Equal("Lantern Vale", resolver.GetDisplayText(unknownItem));
        Assert.Equal("space.plain", resolver.GetDisplayText(plainItem));
    }

    [Fact]
    public void UnknownOrMissingLayoutKeepsCurrentPositionBasedRendering()
    {
        PresentationToken unknownLayout = new("layout.ribbon");
        PresentationToken unknownItem = new("space.ribbon");
        PresentationToken plainItem = new("space.plain");
        ProfilePresentation catalog = new(
        [
            new PresentationMetadata(unknownLayout),
            new PresentationMetadata(unknownItem, layoutToken: unknownLayout),
            new PresentationMetadata(plainItem)
        ]);
        ConsolePresentationResolver resolver = new(catalog);

        Assert.False(resolver.HasKnownLayout(unknownItem));
        Assert.False(resolver.HasKnownLayout(plainItem));
        Assert.Throws<KeyNotFoundException>(() => resolver.GetDisplayText(new PresentationToken("space.missing")));
    }

    [Fact]
    public void SquareCardBuilderUsesOnlyItsMatchPresentationAndHasNoStaticRulesState()
    {
        Game first = SyntheticGameFactory.Setup(new GameRules(2, 2, 6));
        ProfilePresentation variant = new(first.Presentation.Entries.Select(entry => new PresentationMetadata(
            entry.Token,
            displayText: entry.DisplayText is null ? null : $"Variant {entry.Token}",
            shortText: entry.ShortText,
            description: entry.Description,
            symbol: entry.Symbol,
            colorToken: entry.ColorToken,
            layoutToken: entry.LayoutToken)));
        Game second = new GameTestBuilder().WithPresentation(variant).Build();

        SquareCard firstCard = new SquareCardBuilder(first).BuildAllSquareCards().Single(card => card.BoardPosition == 0);
        SquareCard secondCard = new SquareCardBuilder(second).BuildAllSquareCards().Single(card => card.BoardPosition == 0);

        Assert.NotEqual(firstCard.Name, secondCard.Name);
        Assert.Empty(typeof(SquareCardBuilder).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
    }
}
