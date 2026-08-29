using System.Reflection;
using System.Text.Json;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models.FortuneCard;
using Monopoly.Core.Notifications;
using Monopoly.Core.Persistence;
using Monopoly.Core.Presentation;

namespace Monopoly.Tests.CoreTests;

public sealed class ProfileRuleContractTests
{
    private static readonly ResourceId Credits = new("resource.credits");

    [Fact]
    public void ProfileAndRuleIdentifiersAreValidatedStronglyTypedValues()
    {
        ProfileId profile = new("profile.lantern-vale");
        CapabilityId capability = new("profile.lantern-vale");
        EffectKindId effect = new("profile.lantern-vale");
        ResourceId resource = new("profile.lantern-vale");
        StatusId status = new("profile.lantern-vale");
        DecisionKindId decisionKind = new("profile.lantern-vale");
        DecisionOptionId decisionOption = new("profile.lantern-vale");

        Assert.True(profile.IsValid);
        Assert.True(capability.IsValid);
        Assert.True(effect.IsValid);
        Assert.True(resource.IsValid);
        Assert.True(status.IsValid);
        Assert.True(decisionKind.IsValid);
        Assert.True(decisionOption.IsValid);
        Assert.False(default(ProfileId).IsValid);
        Assert.False(default(CapabilityId).IsValid);
        Assert.NotEqual(typeof(ProfileId), typeof(ResourceId));
        Assert.Throws<ArgumentException>(() => new ProfileId("Profile Demo"));
        Assert.Throws<ArgumentException>(() => new StatusId("status_demo"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResourceAmount(Credits, -1));
        Assert.False(default(ResourceAmount).IsValid);
        Assert.Throws<ArgumentException>(() => new PurchasableCapabilityDefinition(default));
        Assert.Throws<ArgumentException>(() => new UsageFeeCapabilityDefinition(default));
    }

    [Fact]
    public void RevisionAndFingerprintRequireCanonicalValues()
    {
        ProfileRevision revision = new(3);
        ProfileFingerprint fingerprint = new(new string('a', ProfileFingerprint.HexLength));

        Assert.Equal(3, revision.Value);
        Assert.Equal(new string('a', 64), fingerprint.Value);
        Assert.False(default(ProfileRevision).IsValid);
        Assert.False(default(ProfileFingerprint).IsValid);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProfileRevision(0));
        Assert.Throws<ArgumentException>(() => new ProfileFingerprint(new string('A', 64)));
        Assert.Throws<ArgumentException>(() => new ProfileFingerprint(new string('a', 63)));
    }

    [Fact]
    public void CapabilitySetIsOrdinalImmutableAndRejectsDuplicates()
    {
        CapabilityDefinition[] source =
        [
            new UsageFeeCapabilityDefinition(new ResourceAmount(Credits, 3)),
            new OwnableCapabilityDefinition(),
            new PurchasableCapabilityDefinition(new ResourceAmount(Credits, 12))
        ];
        CapabilitySet set = new(source);
        source[0] = new DrawCapabilityDefinition(new DeckId("deck.changed"));

        Assert.Equal(["ownable", "purchasable", "usage-fee"], set.Entries.Select(entry => entry.Id.Value));
        Assert.IsType<PurchasableCapabilityDefinition>(set.ById[CapabilityKinds.Purchasable]);
        Assert.Throws<NotSupportedException>(() => ((IList<CapabilityDefinition>)set.Entries).Clear());
        ProfileContractException duplicate = Assert.Throws<ProfileContractException>(() => new CapabilitySet(
            [new OwnableCapabilityDefinition(), new OwnableCapabilityDefinition()]));
        Assert.Equal(ProfileContractErrorKind.DuplicateDefinition, duplicate.Kind);
    }

    [Fact]
    public void EffectSequencePreservesDeclaredOrderAndTypedParameters()
    {
        EffectDefinition[] source =
        [
            new ResourceChangeEffectDefinition(Credits, 4),
            new MoveEffectDefinition(new RelativeMoveTarget(-2), PassOriginPolicy.Ignore, true),
            new StatusEffectDefinition(new StatusId("status.focused"), StatusEffectOperation.Remove)
        ];
        EffectSequence sequence = new(source);
        source[0] = new ResourceChangeEffectDefinition(Credits, 99);

        Assert.Equal(["resource-change", "move", "status"], sequence.Entries.Select(effect => effect.Kind.Value));
        Assert.Equal(-2, Assert.IsType<RelativeMoveTarget>(Assert.IsType<MoveEffectDefinition>(sequence.Entries[1]).Target).Offset);
        Assert.Throws<NotSupportedException>(() => ((IList<EffectDefinition>)sequence.Entries).Clear());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void RuleGraphComposesDifferentTrackAndDeckStructuresDeterministically(int deckCount)
    {
        GameTrack track = Track(3 + deckCount);
        DeckDefinition[] decks = Enumerable.Range(0, deckCount).Select(Deck).Reverse().ToArray();
        SpaceDefinition[] spaces = track.SpaceIds.Select((id, index) => new SpaceDefinition(
            id,
            new PresentationToken($"space.synthetic-{index}"),
            index == 0 && deckCount > 0
                ? new CapabilitySet([new DrawCapabilityDefinition(new DeckId("deck.synthetic-0"))])
                : new CapabilitySet([]))).Reverse().ToArray();

        ProfileRuleGraph graph = new(
            track,
            [Credits],
            new CapabilitySet([new MoveCapabilityDefinition()]),
            spaces,
            decks,
            []);

        Assert.Equal(track.SpaceIds, graph.Spaces.Select(space => space.Id));
        Assert.Equal(Enumerable.Range(0, deckCount).Select(index => $"deck.synthetic-{index}"), graph.Decks.Select(deck => deck.Id.Value));
        Assert.Throws<NotSupportedException>(() => ((IList<SpaceDefinition>)graph.Spaces).Clear());
    }

    [Fact]
    public void RuleGraphRejectsBrokenReferencesAndInvalidCombinationsWithTypedErrors()
    {
        GameTrack track = Track(2);
        SpaceDefinition plain = Space(track.SpaceIds[0]);

        AssertContractError(ProfileContractErrorKind.BrokenReference, () => Graph(
            track,
            [plain],
            [],
            []));

        SpaceDefinition purchaseWithoutOwner = new(
            track.SpaceIds[1],
            new PresentationToken("space.purchase"),
            new CapabilitySet([new PurchasableCapabilityDefinition(new ResourceAmount(Credits, 5))]));
        AssertContractError(ProfileContractErrorKind.InvalidCombination, () => Graph(
            track,
            [plain, purchaseWithoutOwner],
            [],
            []));

        SpaceDefinition missingDeck = new(
            track.SpaceIds[1],
            new PresentationToken("space.draw"),
            new CapabilitySet([new DrawCapabilityDefinition(new DeckId("deck.missing"))]));
        AssertContractError(ProfileContractErrorKind.BrokenReference, () => Graph(
            track,
            [plain, missingDeck],
            [],
            []));
    }

    [Fact]
    public void RuleGraphRejectsDuplicateResourcesSpacesDecksCardsAndStatuses()
    {
        GameTrack track = Track(1);
        SpaceDefinition space = Space(track.SpaceIds[0]);
        StatusDefinition status = new(new StatusId("status.focused"), new PresentationToken("status.focused"), 2);

        AssertContractError(ProfileContractErrorKind.DuplicateDefinition, () => new ProfileRuleGraph(
            track, [Credits, Credits], new CapabilitySet([new MoveCapabilityDefinition()]), [space], [], []));
        AssertContractError(ProfileContractErrorKind.DuplicateDefinition, () => new ProfileRuleGraph(
            track, [Credits], new CapabilitySet([new MoveCapabilityDefinition()]), [space, space], [], []));

        DeckDefinition deck = Deck(0);
        AssertContractError(ProfileContractErrorKind.DuplicateDefinition, () => new ProfileRuleGraph(
            track, [Credits], new CapabilitySet([new MoveCapabilityDefinition()]), [space], [deck, deck], []));
        AssertContractError(ProfileContractErrorKind.DuplicateDefinition, () => new ProfileRuleGraph(
            track,
            [Credits],
            new CapabilitySet([new MoveCapabilityDefinition()]),
            [space],
            [deck, new DeckDefinition(
                new DeckId("deck.other"),
                new PresentationToken("deck.other"),
                [new CardDefinition(deck.Cards[0].Id, new PresentationToken("card.other"), new EffectSequence([]))])],
            []));
        AssertContractError(ProfileContractErrorKind.DuplicateDefinition, () => new ProfileRuleGraph(
            track, [Credits], new CapabilitySet([new MoveCapabilityDefinition()]), [space], [], [status, status]));
    }

    [Fact]
    public void RuleGraphValidatesEffectSpaceResourceAndStatusReferences()
    {
        GameTrack track = Track(1);
        StatusDefinition status = new(new StatusId("status.focused"), new PresentationToken("status.focused"), 2);

        AssertContractError(ProfileContractErrorKind.BrokenReference, () => Graph(
            track,
            [Space(track.SpaceIds[0])],
            [Deck(0, [new MoveEffectDefinition(new AbsoluteMoveTarget(new SpaceId("space.missing")), PassOriginPolicy.Ignore, true)])],
            [status]));
        AssertContractError(ProfileContractErrorKind.BrokenReference, () => Graph(
            track,
            [Space(track.SpaceIds[0])],
            [Deck(0, [new ResourceChangeEffectDefinition(new ResourceId("resource.missing"), 1)])],
            [status]));
        AssertContractError(ProfileContractErrorKind.BrokenReference, () => Graph(
            track,
            [Space(track.SpaceIds[0])],
            [Deck(0, [new StatusEffectDefinition(new StatusId("status.missing"), StatusEffectOperation.Apply, 1)])],
            [status]));
        AssertContractError(ProfileContractErrorKind.InvalidCombination, () => Graph(
            track,
            [Space(track.SpaceIds[0])],
            [Deck(0, [new StatusEffectDefinition(status.Id, StatusEffectOperation.Apply, 3)])],
            [status]));
    }

    [Fact]
    public void UnknownCapabilityAndEffectKindsAreRejected()
    {
        ProfileContractException capability = Assert.Throws<ProfileContractException>(() =>
            CapabilityKinds.EnsureKnown(new CapabilityId("unknown-capability")));
        ProfileContractException effect = Assert.Throws<ProfileContractException>(() =>
            EffectKinds.EnsureKnown(new EffectKindId("unknown-effect")));

        Assert.Equal(ProfileContractErrorKind.UnknownComponent, capability.Kind);
        Assert.Equal(ProfileContractErrorKind.UnknownComponent, effect.Kind);
    }

    [Fact]
    public void ExportedDefinitionsCannotCarryExecutableOrOpenPayloads()
    {
        Type[] definitions =
        [
            typeof(CapabilityDefinition), typeof(EffectDefinition), typeof(SpaceDefinition),
            typeof(CardDefinition), typeof(DeckDefinition), typeof(StatusDefinition), typeof(ProfileRuleGraph)
        ];

        foreach (Type definition in definitions)
        {
            Assert.True(definition.IsSealed || definition.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length == 0);
            IEnumerable<Type> exposed = definition.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.PropertyType)
                .Concat(definition.GetConstructors().SelectMany(constructor => constructor.GetParameters()).Select(parameter => parameter.ParameterType));
            Assert.DoesNotContain(exposed, type => type == typeof(object) || typeof(Delegate).IsAssignableFrom(type) || type == typeof(Type) || type == typeof(Assembly));
        }
    }

    [Fact]
    public void PublicRuntimeSurfaceExposesGenericSpacesStatusesAndDecisionsOnly()
    {
        Type[] hiddenLegacyTypes =
        [
            typeof(Square), typeof(PropertySquare), typeof(RailroadSquare), typeof(UtilitySquare),
            typeof(TaxSquare), typeof(ChanceSquare), typeof(CommunityChestSquare), typeof(GoSquare),
            typeof(GoToJailSquare), typeof(JailSquare), typeof(ParkingSquare), typeof(Jail),
            typeof(StatusDecision), typeof(UKChanceCard), typeof(USChanceCard),
            typeof(UKCommunityChestCard), typeof(USCommunityChestCard)
        ];

        Assert.All(hiddenLegacyTypes, type => Assert.False(type.IsVisible, $"{type.FullName} is publicly visible."));
        Assert.Null(typeof(Player).GetProperty("NumberOfGetOutOFJailCards"));
        Assert.Null(typeof(IGame).GetProperty("TheJail"));
        Assert.DoesNotContain(typeof(IGame).GetMethods(), method =>
            method.Name is "TryBuyHouse" or "TrySellHouse" or "TryMortgageProperty" or "TryRepayMortgage");
        Assert.Null(typeof(TurnResult).GetProperty("LandedSquare"));
        Assert.Null(typeof(TurnResult).GetProperty("WasSentToJail"));
        Assert.Null(typeof(TurnResult).GetProperty("WasReleasedFromJailByDouble"));
        Assert.Equal(typeof(SpaceView), typeof(TurnResult).GetProperty(nameof(TurnResult.LandedSpace))!.PropertyType);
        Assert.Equal(typeof(SpaceView), typeof(SpaceReachedNotification).GetProperty(nameof(SpaceReachedNotification.Space))!.PropertyType);
        Assert.Equal(typeof(StatusCollection), typeof(IGame).GetProperty(nameof(IGame.Statuses))!.PropertyType);
        Assert.Equal(typeof(SpaceView), typeof(GameBoard).GetMethod(nameof(GameBoard.GetSpace))!.ReturnType);
        Assert.Null(typeof(Game).Assembly.GetType("Monopoly.Core.DecisionKind"));
        Assert.Null(typeof(Game).Assembly.GetType("Monopoly.Core.DecisionOption"));
        Assert.DoesNotContain(
            typeof(Game).Assembly.GetExportedTypes(),
            type => type.Namespace?.StartsWith("Monopoly.Console", StringComparison.Ordinal) ?? false);
    }

    [Fact]
    public void RuntimeSpaceAndStatusViewsAreDetachedImmutableSnapshots()
    {
        Game game = new GameTestBuilder()
            .WithPlayerInJail(0, turnsInJail: 2)
            .Build();

        SpaceView space = game.Board.GetSpace(game.Board.Track.GetSpaceIdAt(0));
        StatusCollection statuses = game.Statuses;
        PlayerStatusView status = Assert.Single(statuses.Entries);

        Assert.Equal(0, space.Index);
        Assert.Equal(game.Board.Spaces[0], space);
        Assert.Equal(game.CurrentPlayer.Id, status.PlayerId);
        Assert.Equal(LegacyStatusIds.Detained, status.Status.Id);
        Assert.Equal(2, status.Status.Value);
        Assert.Throws<NotSupportedException>(() => ((IList<SpaceView>)game.Board.Spaces).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<PlayerStatusView>)statuses.Entries).Clear());

        game.TheJail.IncrementTurnsInJail(game.CurrentPlayer);

        Assert.Equal(2, status.Status.Value);
        Assert.Equal(3, Assert.Single(game.Statuses.Entries).Status.Value);
    }

    [Fact]
    public void InvalidRuleGraphConstructionCannotMutateAnActiveMatch()
    {
        Game game = new GameTestBuilder().Build();
        string before = JsonSerializer.Serialize(GameStateV1Mapper.ToState(game));
        GameTrack track = Track(1);

        AssertContractError(ProfileContractErrorKind.InvalidCombination, () => new ProfileRuleGraph(
            track,
            [default(ResourceId)],
            new CapabilitySet([new MoveCapabilityDefinition()]),
            [Space(track.SpaceIds[0])],
            [],
            []));

        Assert.Equal(before, JsonSerializer.Serialize(GameStateV1Mapper.ToState(game)));
    }

    private static ProfileRuleGraph Graph(
        GameTrack track,
        IEnumerable<SpaceDefinition> spaces,
        IEnumerable<DeckDefinition> decks,
        IEnumerable<StatusDefinition> statuses) =>
        new(track, [Credits], new CapabilitySet([new MoveCapabilityDefinition()]), spaces, decks, statuses);

    private static GameTrack Track(int count) => new(
        Enumerable.Range(0, count).Select(index => new SpaceId($"space.synthetic-{index}")));

    private static SpaceDefinition Space(SpaceId id) => new(
        id,
        new PresentationToken($"presentation.{id.Value}"),
        new CapabilitySet([]));

    private static DeckDefinition Deck(int index) => Deck(index, []);

    private static DeckDefinition Deck(int index, IEnumerable<EffectDefinition> effects) => new(
        new DeckId($"deck.synthetic-{index}"),
        new PresentationToken($"deck.synthetic-{index}"),
        [new CardDefinition(
            new CardId($"card.synthetic-{index}"),
            new PresentationToken($"card.synthetic-{index}"),
            new EffectSequence(effects))]);

    private static void AssertContractError(ProfileContractErrorKind kind, Action action) =>
        Assert.Equal(kind, Assert.Throws<ProfileContractException>(action).Kind);
}
