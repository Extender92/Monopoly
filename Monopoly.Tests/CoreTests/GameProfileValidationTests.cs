using System.Reflection;
using System.Text.Json;
using Monopoly.Core.Presentation;

namespace Monopoly.Tests.CoreTests;

public sealed class GameProfileValidationTests
{
    [Fact]
    public void ValidatedProfileIsImmutableAndCarriesCanonicalIdentitySetupAndPolicies()
    {
        GameProfileDefinition definition = ProfileTestFactory.Create(deckCount: 2, cardsPerDeck: 2, effectsPerCard: 2);

        ValidatedGameProfile profile = GameProfileValidator.Validate(definition);

        Assert.Equal(GameProfileSchema.Version1, profile.SchemaVersion);
        Assert.Equal(new ProfileId("profile.synthetic"), profile.Id);
        Assert.True(profile.Fingerprint.IsValid);
        Assert.Equal(3, profile.RuleGraph.Track.Count);
        Assert.Equal(2, profile.RuleGraph.Decks.Count);
        Assert.Equal(StartingPlayerPolicyKind.FixedOrder, profile.Setup.StartingPlayerPolicy);
        Assert.Equal(PurchaseDeclinePolicyKind.LeaveUnowned, profile.Policies.PurchaseDecline);
        Assert.Throws<NotSupportedException>(() => ((IList<ProfileResourceDefinition>)profile.Resources).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<DeckDefinition>)profile.RuleGraph.Decks).Clear());
    }

    [Fact]
    public void ValidatorAcceptsEachExactStructuralLimitAndRejectsTheNextValue()
    {
        Assert.Equal(GameProfileSchema.MaximumSpaces, GameProfileValidator.Validate(
            ProfileTestFactory.Create(spaceCount: GameProfileSchema.MaximumSpaces)).RuleGraph.Track.Count);
        AssertLimit(() => GameProfileValidator.Validate(
            ProfileTestFactory.Create(spaceCount: GameProfileSchema.MaximumSpaces + 1)));

        Assert.Equal(GameProfileSchema.MaximumDecks, GameProfileValidator.Validate(
            ProfileTestFactory.Create(deckCount: GameProfileSchema.MaximumDecks)).RuleGraph.Decks.Count);
        AssertLimit(() => GameProfileValidator.Validate(
            ProfileTestFactory.Create(deckCount: GameProfileSchema.MaximumDecks + 1)));

        Assert.Equal(GameProfileSchema.MaximumCards, GameProfileValidator.Validate(
            ProfileTestFactory.Create(deckCount: 1, cardsPerDeck: GameProfileSchema.MaximumCards)).RuleGraph.Decks[0].Cards.Count);
        AssertLimit(() => GameProfileValidator.Validate(
            ProfileTestFactory.Create(deckCount: 1, cardsPerDeck: GameProfileSchema.MaximumCards + 1)));

        Assert.Equal(GameProfileSchema.MaximumEffectsPerCard, GameProfileValidator.Validate(
            ProfileTestFactory.Create(deckCount: 1, effectsPerCard: GameProfileSchema.MaximumEffectsPerCard))
            .RuleGraph.Decks[0].Cards[0].Effects.Count);
        AssertLimit(() => GameProfileValidator.Validate(
            ProfileTestFactory.Create(deckCount: 1, effectsPerCard: GameProfileSchema.MaximumEffectsPerCard + 1)));

        string exactText = string.Concat(Enumerable.Repeat("\U0001F4A1", GameProfileSchema.MaximumPresentationTextLength));
        Assert.True(GameProfileValidator.Validate(ProfileTestFactory.Create(profileDisplayText: exactText)).Fingerprint.IsValid);
        AssertLimit(() => GameProfileValidator.Validate(ProfileTestFactory.Create(profileDisplayText: exactText + "\U0001F4A1")));
    }

    [Fact]
    public void ValidatorRejectsMissingPresentationSetupAndPolicyReferencesWithTypedPaths()
    {
        GameProfileDefinition baseline = ProfileTestFactory.Create();
        ProfilePresentation incomplete = new(baseline.Presentation.Entries.Where(entry => entry.Token != baseline.PresentationToken));
        GameProfileDefinition missingPresentation = Copy(baseline, presentation: incomplete);

        ProfileValidationException presentationError = Assert.Throws<ProfileValidationException>(() =>
            GameProfileValidator.Validate(missingPresentation));
        Assert.Equal(ProfileValidationErrorKind.MissingPresentation, presentationError.Kind);
        Assert.Equal("profilePresentationToken", presentationError.Path);

        ProfileSetupDefinition badSetup = new(
            1, 2, 1, 6, new SpaceId("space.missing"), baseline.Setup.StartingResources, StartingPlayerPolicyKind.FixedOrder);
        ProfileValidationException setupError = Assert.Throws<ProfileValidationException>(() =>
            GameProfileValidator.Validate(Copy(baseline, setup: badSetup)));
        Assert.Equal(ProfileValidationErrorKind.BrokenReference, setupError.Kind);
        Assert.Equal("setup.startSpaceId", setupError.Path);

        ProfilePolicySet badPolicies = new(
            new ResourceAmount(new ResourceId("resource.missing"), 1),
            PurchaseDeclinePolicyKind.LeaveUnowned,
            baseline.Policies.MatchEnd);
        ProfileValidationException policyError = Assert.Throws<ProfileValidationException>(() =>
            GameProfileValidator.Validate(Copy(baseline, policies: badPolicies)));
        Assert.Equal(ProfileValidationErrorKind.BrokenReference, policyError.Kind);
        Assert.Equal("policies.passOriginReward.resourceId", policyError.Path);
    }

    [Fact]
    public void DefinitionCopiesInputCollectionsAndFailedValidationDoesNotMutateActiveGame()
    {
        GameProfileDefinition baseline = ProfileTestFactory.Create();
        ProfileResourceDefinition[] resources = baseline.Resources.ToArray();
        SpaceDefinition[] spaces = baseline.Spaces.ToArray();
        GameProfileDefinition copied = new(
            baseline.SchemaVersion, baseline.Id, baseline.Revision, baseline.PresentationToken,
            baseline.Presentation, resources, baseline.Setup, baseline.Track, baseline.ProfileCapabilities,
            spaces, baseline.Decks, baseline.Statuses, baseline.Policies);
        resources[0] = new ProfileResourceDefinition(new ResourceId("resource.changed"), new PresentationToken("resource.changed"));
        spaces[0] = spaces[1];

        ValidatedGameProfile valid = GameProfileValidator.Validate(copied);
        Assert.Equal(new ResourceId("resource.credits"), valid.Resources[0].Id);
        Assert.Equal(new SpaceId("space.synthetic-0"), valid.RuleGraph.Spaces[0].Id);

        Game game = new GameTestBuilder().Build();
        string before = GameTestSnapshot.Capture(game);
        GameProfileDefinition invalid = ProfileTestFactory.Create(spaceCount: GameProfileSchema.MaximumSpaces + 1);

        Assert.Throws<ProfileValidationException>(() => GameProfileValidator.Validate(invalid));
        Assert.Equal(before, GameTestSnapshot.Capture(game));
    }

    [Fact]
    public void PublicProfileContractsDoNotExposeJsonFilesystemOrExecutablePayloadTypes()
    {
        Type[] contracts =
        [
            typeof(GameProfileDefinition), typeof(ValidatedGameProfile), typeof(ProfileSetupDefinition),
            typeof(ProfilePolicySet), typeof(ProfileResourceDefinition), typeof(RoundLimitedScorePolicy)
        ];

        foreach (Type contract in contracts)
        {
            IEnumerable<Type> exposed = contract.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.PropertyType)
                .Concat(contract.GetConstructors().SelectMany(constructor => constructor.GetParameters()).Select(parameter => parameter.ParameterType));
            Assert.DoesNotContain(exposed, type =>
                type.Namespace?.StartsWith("System.Text.Json", StringComparison.Ordinal) == true ||
                type == typeof(FileInfo) || type == typeof(DirectoryInfo) || type == typeof(Stream) ||
                type == typeof(object) || type == typeof(Type) || type == typeof(Assembly) || typeof(Delegate).IsAssignableFrom(type));
        }
    }

    private static GameProfileDefinition Copy(
        GameProfileDefinition source,
        ProfilePresentation? presentation = null,
        ProfileSetupDefinition? setup = null,
        ProfilePolicySet? policies = null) => new(
            source.SchemaVersion,
            source.Id,
            source.Revision,
            source.PresentationToken,
            presentation ?? source.Presentation,
            source.Resources,
            setup ?? source.Setup,
            source.Track,
            source.ProfileCapabilities,
            source.Spaces,
            source.Decks,
            source.Statuses,
            policies ?? source.Policies);

    private static void AssertLimit(Action action) =>
        Assert.Equal(ProfileValidationErrorKind.LimitExceeded, Assert.Throws<ProfileValidationException>(action).Kind);
}
