using Monopoly.Core.Models.Board;

namespace Monopoly.Core.Presentation;

internal enum LegacyPropertyGroup
{
    Group1,
    Group2,
    Group3,
    Group4,
    Group5,
    Group6,
    Group7,
    Group8
}

internal static class LegacyPresentationFactory
{
    private static readonly PresentationToken[] AccentTokens =
    [
        new("accent.earth"),
        new("accent.mist"),
        new("accent.bloom"),
        new("accent.ember"),
        new("accent.flame"),
        new("accent.sun"),
        new("accent.grove"),
        new("accent.depth")
    ];

    internal static ProfilePresentation Create(GameRules rules, GameBoard board, FortuneCardHandler cards)
    {
        List<PresentationMetadata> entries =
        [
            .. AccentTokens.Select(token => new PresentationMetadata(token)),
            new(PresentationTokens.PrimaryResource, symbol: rules.GameLanguage == GameRules.Language.UK ? "£" : "$"),
            new(PresentationTokens.PrimaryDeck, displayText: "Primary event deck", colorToken: new PresentationToken("accent.flame")),
            new(PresentationTokens.SecondaryDeck, displayText: "Secondary event deck", colorToken: new PresentationToken("accent.depth")),
            new(PresentationTokens.PropertyPurchaseDecision, displayText: "Purchase space"),
            new(PresentationTokens.DetentionReleaseDecision, displayText: "Choose release method"),
            new(PresentationTokens.DetainedStatus, displayText: "Detained"),
            new(PresentationTokens.LogNotification),
            new(PresentationTokens.BoardNotification),
            new(PresentationTokens.PlayerInformationNotification)
        ];

        foreach (Square square in board.Squares)
        {
            entries.Add(square.Presentation);
        }

        entries.AddRange(board.Squares
            .OfType<PropertySquare>()
            .Select(property => property.GroupPresentation)
            .DistinctBy(metadata => metadata.Token));

        entries.AddRange(cards.AllPresentationMetadata.DistinctBy(metadata => metadata.Token));
        return new ProfilePresentation(entries);
    }

    internal static IReadOnlyList<PresentationToken> RequiredReferences(GameBoard board, FortuneCardHandler cards) =>
    [
        PresentationTokens.PrimaryResource,
        PresentationTokens.PrimaryDeck,
        PresentationTokens.SecondaryDeck,
        PresentationTokens.PropertyPurchaseDecision,
        PresentationTokens.DetentionReleaseDecision,
        PresentationTokens.DetainedStatus,
        PresentationTokens.LogNotification,
        PresentationTokens.BoardNotification,
        PresentationTokens.PlayerInformationNotification,
        .. board.Squares.Select(square => square.PresentationToken),
        .. board.Squares.OfType<PropertySquare>().Select(property => property.GroupPresentationToken),
        .. cards.AllPresentationMetadata.Select(metadata => metadata.Token)
    ];

    internal static PresentationMetadata Space(
        int position,
        string displayText,
        string? description = null,
        PresentationToken? colorToken = null,
        PresentationToken? layoutToken = null) =>
        new(
            new PresentationToken($"space.{position}"),
            displayText: displayText,
            description: description,
            colorToken: colorToken,
            layoutToken: layoutToken);

    internal static (GroupId Id, PresentationMetadata Presentation) Group(LegacyPropertyGroup group)
    {
        int index = (int)group + 1;
        PresentationToken token = new($"group.{index}");
        return (
            new GroupId($"legacy-group.{index}"),
            new PresentationMetadata(token, displayText: $"Group {index}", colorToken: AccentTokens[index - 1]));
    }

    internal static PresentationMetadata Card(string deck, int ordinal, string description) =>
        new(new PresentationToken($"card.{deck}.{ordinal}"), description: description);

}
