using Monopoly.Core.Presentation;

namespace Monopoly.Tests.TestDoubles;

internal sealed record TestCardSpec(string Id, IReadOnlyList<EffectDefinition> Effects);
internal sealed record TestDeckSpec(string Id, IReadOnlyList<TestCardSpec> Cards);

internal static class ExecutionProfileFactory
{
    internal static readonly ResourceId Credits = new("resource.credits");
    internal static readonly ResourceId Score = new("resource.score");

    internal static ValidatedGameProfile Create(
        int spaceCount = 3,
        IReadOnlyDictionary<int, IReadOnlyList<CapabilityDefinition>>? spaceCapabilities = null,
        IReadOnlyList<TestDeckSpec>? decks = null,
        int startingCredits = 20,
        int startingScore = 0,
        int diceCount = 1,
        int dieSides = 6,
        int passReward = 2,
        int roundLimit = 5,
        StartingPlayerPolicyKind startingPlayerPolicy = StartingPlayerPolicyKind.FixedOrder)
    {
        spaceCapabilities ??= new Dictionary<int, IReadOnlyList<CapabilityDefinition>>();
        decks ??= [];
        GameTrack track = new(Enumerable.Range(0, spaceCount).Select(index => new SpaceId($"space.execution-{index}")));
        List<PresentationMetadata> presentation =
        [
            new(new PresentationToken("profile.execution"), "Execution Profile"),
            new(new PresentationToken("resource.credits"), "Credits"),
            new(new PresentationToken("resource.score"), "Score")
        ];

        SpaceDefinition[] spaces = track.SpaceIds.Select((id, index) =>
        {
            PresentationToken token = new($"space.execution-{index}");
            presentation.Add(new PresentationMetadata(token, $"Execution Space {index}"));
            return new SpaceDefinition(
                id,
                token,
                new CapabilitySet(spaceCapabilities.TryGetValue(index, out IReadOnlyList<CapabilityDefinition>? entries) ? entries : []));
        }).ToArray();

        DeckDefinition[] definitions = decks.Select(deck =>
        {
            DeckId deckId = new(deck.Id);
            PresentationToken deckToken = new(deck.Id);
            presentation.Add(new PresentationMetadata(deckToken, deck.Id));
            CardDefinition[] cards = deck.Cards.Select(card =>
            {
                PresentationToken cardToken = new(card.Id);
                presentation.Add(new PresentationMetadata(cardToken, card.Id));
                return new CardDefinition(new CardId(card.Id), cardToken, new EffectSequence(card.Effects));
            }).ToArray();
            return new DeckDefinition(deckId, deckToken, cards);
        }).ToArray();

        GameProfileDefinition definition = new(
            GameProfileSchema.Version1,
            new ProfileId("profile.execution"),
            new ProfileRevision(1),
            new PresentationToken("profile.execution"),
            new ProfilePresentation(presentation),
            [
                new ProfileResourceDefinition(Credits, new PresentationToken("resource.credits")),
                new ProfileResourceDefinition(Score, new PresentationToken("resource.score"))
            ],
            new ProfileSetupDefinition(
                1,
                6,
                diceCount,
                dieSides,
                track.SpaceIds[0],
                [new ResourceAmount(Credits, startingCredits), new ResourceAmount(Score, startingScore)],
                startingPlayerPolicy),
            track,
            new CapabilitySet([new MoveCapabilityDefinition()]),
            spaces,
            definitions,
            [],
            new ProfilePolicySet(
                new ResourceAmount(Credits, passReward),
                PurchaseDeclinePolicyKind.LeaveUnowned,
                new RoundLimitedScorePolicy(roundLimit, Score, MatchTieBreakPolicy.LowestPlayerId)));
        return GameProfileValidator.Validate(definition);
    }
}
