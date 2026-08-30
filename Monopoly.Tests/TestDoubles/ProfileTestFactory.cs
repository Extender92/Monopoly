using Monopoly.Core.Presentation;

namespace Monopoly.Tests.TestDoubles;

internal static class ProfileTestFactory
{
    internal static GameProfileDefinition Create(
        int spaceCount = 3,
        int deckCount = 0,
        int cardsPerDeck = 1,
        int effectsPerCard = 0,
        string? profileDisplayText = "Synthetic Profile")
    {
        ProfileId profileId = new("profile.synthetic");
        PresentationToken profileToken = new("profile.synthetic");
        ResourceId credits = new("resource.credits");
        ResourceId score = new("resource.score");
        GameTrack track = new(Enumerable.Range(0, spaceCount).Select(index => new SpaceId($"space.synthetic-{index}")));

        List<PresentationMetadata> presentation =
        [
            new(profileToken, profileDisplayText),
            new(new PresentationToken("resource.credits"), "Credits"),
            new(new PresentationToken("resource.score"), "Score")
        ];
        SpaceDefinition[] spaces = track.SpaceIds.Select((id, index) =>
        {
            PresentationToken token = new($"space.synthetic-{index}");
            presentation.Add(new PresentationMetadata(token, $"Space {index}"));
            return new SpaceDefinition(id, token, new CapabilitySet([]));
        }).ToArray();

        DeckDefinition[] decks = Enumerable.Range(0, deckCount).Select(deckIndex =>
        {
            DeckId deckId = new($"deck.synthetic-{deckIndex}");
            PresentationToken deckToken = new($"deck.synthetic-{deckIndex}");
            presentation.Add(new PresentationMetadata(deckToken, $"Deck {deckIndex}"));
            CardDefinition[] cards = Enumerable.Range(0, cardsPerDeck).Select(cardIndex =>
            {
                CardId cardId = new($"card.synthetic-{deckIndex}-{cardIndex}");
                PresentationToken cardToken = new($"card.synthetic-{deckIndex}-{cardIndex}");
                presentation.Add(new PresentationMetadata(cardToken, $"Card {deckIndex}-{cardIndex}"));
                EffectDefinition[] effects = Enumerable.Range(0, effectsPerCard)
                    .Select(_ => (EffectDefinition)new ResourceChangeEffectDefinition(score, 1))
                    .ToArray();
                return new CardDefinition(cardId, cardToken, new EffectSequence(effects));
            }).ToArray();
            return new DeckDefinition(deckId, deckToken, cards);
        }).ToArray();

        return new GameProfileDefinition(
            GameProfileSchema.Version1,
            profileId,
            new ProfileRevision(1),
            profileToken,
            new ProfilePresentation(presentation),
            [
                new ProfileResourceDefinition(credits, new PresentationToken("resource.credits")),
                new ProfileResourceDefinition(score, new PresentationToken("resource.score"))
            ],
            new ProfileSetupDefinition(
                1,
                6,
                2,
                6,
                track.SpaceIds[0],
                [new ResourceAmount(credits, 20), new ResourceAmount(score, 0)],
                StartingPlayerPolicyKind.FixedOrder),
            track,
            new CapabilitySet([new MoveCapabilityDefinition()]),
            spaces,
            decks,
            [],
            new ProfilePolicySet(
                new ResourceAmount(credits, 2),
                PurchaseDeclinePolicyKind.LeaveUnowned,
                new RoundLimitedScorePolicy(5, score, MatchTieBreakPolicy.LowestPlayerId)));
    }
}
