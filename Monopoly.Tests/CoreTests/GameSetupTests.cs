using System.Collections;
using System.Reflection;
using System.Text.Json;
using Infrastructure.Profiles;
using Monopoly.Core.Interface;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Notifications;
using Monopoly.Core.Presentation;

namespace Monopoly.Tests.CoreTests;

public sealed class GameSetupTests
{
    private static readonly string DemoPath = Path.Combine(
        AppContext.BaseDirectory,
        "TestData",
        "Profiles",
        "Demo",
        "lantern-vale-v1.json");

    [Fact]
    public void BundledDemoCreatesCompleteReadableInitialState()
    {
        ValidatedGameProfile profile = new JsonGameProfileParser().Parse(File.ReadAllBytes(DemoPath));
        PlayerSetup[] players = [new(14, "Aster"), new(3, "Bramble")];

        Game game = GameSetup.Create(profile, players, new MinimumMatchRandomSource());

        Assert.Same(profile, game.Profile);
        Assert.Equal(new ProfileId("profile.demo-001"), game.Profile!.Id);
        Assert.Equal(new ProfileRevision(1), game.Profile.Revision);
        Assert.Equal("7ba140a86da1a20222f2580b7419ca7e3f52d7a392bcadf9269ed1fe5a456c7d", game.Profile.Fingerprint.Value);
        Assert.Equal(27, game.Board.Track.Count);
        Assert.Equal(profile.RuleGraph.Track.SpaceIds, game.Board.Track.SpaceIds);
        Assert.Equal(1, game.Decks.Count);
        Assert.Equal(9, Assert.Single(game.Decks.Entries).Cards.Count);
        Assert.Equal([14, 3], game.Players.Select(player => player.Id));
        Assert.Same(game.Players[0], game.CurrentPlayer);
        Assert.All(game.Players, player =>
        {
            Assert.Equal(0, player.Position);
            Assert.Equal(new SpaceId("space.s001"), player.CurrentSpaceId);
            Assert.Equal(120, player.Resources[new ResourceId("resource.lumen")]);
            Assert.Equal(0, player.Resources[new ResourceId("resource.renown")]);
        });
        Assert.Equal(14, game.Ownership.Count);
        Assert.All(game.Ownership.Entries, entry => Assert.Null(entry.OwnerPlayerId));
        Assert.Empty(game.Statuses.Entries);
        Assert.Equal(game.Ownership.Entries, game.ModuleState.Ownership.Entries);
        Assert.Empty(game.ModuleState.Statuses.Entries);
        Assert.Equal(1, game.RoundNumber);
        Assert.Equal(1, game.CurrentTurn);
        Assert.Equal(GamePhase.ReadyForTurn, game.Phase);
        Assert.Null(game.PendingDecision);
        Assert.Null(game.Winner);
        Assert.Null(game.LastDiceRoll);
        Assert.False(game.IsGameOver);
        Assert.Empty(game.Logs.LogList);
        Assert.All(game.Board.Squares, square => Assert.IsType<ProfileSpace>(square));
        Assert.Throws<InvalidOperationException>(() => _ = game.Rules);
        Assert.Throws<InvalidOperationException>(() => _ = game.TheJail);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<ResourceId, int>)game.Players[0].Resources).Add(new ResourceId("resource.extra"), 1));
        Assert.Throws<NotSupportedException>(() => ((IList<SpaceOwnershipView>)game.Ownership.Entries).Clear());

        game.ValidateAuthoritativeState();
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(3, 1)]
    [InlineData(53, 3)]
    public void StructurallyDifferentProfilesCreateWithoutFixedTrackOrDeckCounts(int spaceCount, int deckCount)
    {
        ValidatedGameProfile profile = Validate(ProfileTestFactory.Create(
            spaceCount: spaceCount,
            deckCount: deckCount,
            cardsPerDeck: 2));

        Game game = GameSetup.Create(profile, [new PlayerSetup(7, "Solo")], new MinimumMatchRandomSource());

        Assert.Equal(spaceCount, game.Board.Track.Count);
        Assert.Equal(deckCount, game.Decks.Count);
        Assert.Empty(game.Ownership.Entries);
        Assert.Equal(profile.Setup.StartSpaceId, game.CurrentPlayer.CurrentSpaceId);
    }

    [Fact]
    public void PlayerRangeIdentityAndNamesAreValidatedWithoutRequiringUniqueNames()
    {
        ValidatedGameProfile profile = Validate(ProfileTestFactory.Create());

        Game minimum = GameSetup.Create(profile, [new PlayerSetup(10, "Same")]);
        Game maximum = GameSetup.Create(profile, Enumerable.Range(0, 6).Select(id => new PlayerSetup(id * 3, "Same")));
        GameSetupException duplicate = Assert.Throws<GameSetupException>(() => GameSetup.Create(
            profile,
            [new PlayerSetup(1, "One"), new PlayerSetup(1, "Two")]));
        GameSetupException tooMany = Assert.Throws<GameSetupException>(() => GameSetup.Create(
            profile,
            Enumerable.Range(0, 7).Select(id => new PlayerSetup(id, $"Player {id}"))));
        GameSetupException negative = Assert.Throws<GameSetupException>(() => new PlayerSetup(-1, "Name"));
        GameSetupException blank = Assert.Throws<GameSetupException>(() => new PlayerSetup(1, "  "));

        Assert.Single(minimum.Players);
        Assert.Equal(6, maximum.Players.Count);
        Assert.Equal(GameSetupErrorKind.DuplicatePlayer, duplicate.Kind);
        Assert.Equal("players[1].id", duplicate.Path);
        Assert.Equal(GameSetupErrorKind.InvalidPlayerCount, tooMany.Kind);
        Assert.Equal("players", tooMany.Path);
        Assert.Equal(GameSetupErrorKind.InvalidPlayer, negative.Kind);
        Assert.Equal("players.id", negative.Path);
        Assert.Equal(GameSetupErrorKind.InvalidPlayer, blank.Kind);
        Assert.Equal("players.name", blank.Path);
    }

    [Fact]
    public void MissingCapabilityEffectStatusAndPolicyRegistrationsFailWithTypedPaths()
    {
        ValidatedGameProfile plain = Validate(ProfileTestFactory.Create());
        ProfileComponentRegistry noCapabilities = Registry(capabilities: []);
        GameSetupException capability = Assert.Throws<GameSetupException>(() =>
            GameSetup.Create(plain, [new PlayerSetup(0, "One")], null, noCapabilities));

        ValidatedGameProfile withEffect = Validate(ProfileTestFactory.Create(deckCount: 1, effectsPerCard: 1));
        ProfileComponentRegistry noEffects = Registry(effects: []);
        GameSetupException effect = Assert.Throws<GameSetupException>(() =>
            GameSetup.Create(withEffect, [new PlayerSetup(0, "One")], null, noEffects));

        ValidatedGameProfile withStatus = ProfileWithStatus();
        GameSetupException status = Assert.Throws<GameSetupException>(() =>
            GameSetup.Create(withStatus, [new PlayerSetup(0, "One")]));

        ProfileComponentRegistry noPolicies = Registry(startingPolicies: [StartingPlayerPolicyKind.Random]);
        GameSetupException policy = Assert.Throws<GameSetupException>(() =>
            GameSetup.Create(plain, [new PlayerSetup(0, "One")], null, noPolicies));
        ProfileComponentRegistry noMatchEnd = Registry(supportsRoundLimitedScore: false);
        GameSetupException matchEnd = Assert.Throws<GameSetupException>(() =>
            GameSetup.Create(plain, [new PlayerSetup(0, "One")], null, noMatchEnd));

        Assert.Equal(GameSetupErrorKind.UnsupportedComponent, capability.Kind);
        Assert.Equal("profile.profileCapabilities[0].kind", capability.Path);
        Assert.Equal(GameSetupErrorKind.UnsupportedComponent, effect.Kind);
        Assert.Contains("effects[0].kind", effect.Path, StringComparison.Ordinal);
        Assert.Equal(GameSetupErrorKind.UnsupportedComponent, status.Kind);
        Assert.Equal("profile.statuses[0].id", status.Path);
        Assert.Equal(GameSetupErrorKind.UnsupportedPolicy, policy.Kind);
        Assert.Equal("profile.setup.startingPlayerPolicy", policy.Path);
        Assert.Equal(GameSetupErrorKind.UnsupportedPolicy, matchEnd.Kind);
        Assert.Equal("profile.policies.matchEnd", matchEnd.Path);
    }

    [Fact]
    public void DeckShuffleRunsBeforeRandomStartingPlayerWithSeparateSequences()
    {
        ValidatedGameProfile profile = WithStartingPolicy(
            ProfileTestFactory.Create(deckCount: 1, cardsPerDeck: 3),
            StartingPlayerPolicyKind.Random);
        ScriptedMatchRandomSource source = new(0, 0, 1);

        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(11, "First"), new PlayerSetup(22, "Second")],
            source);

        Assert.Equal(22, game.CurrentPlayer.Id);
        Assert.Equal(
            [RandomPurpose.DeckShuffle, RandomPurpose.DeckShuffle, RandomPurpose.SetupStartingPlayer],
            source.Requests.Select(request => request.Purpose));
        Assert.Equal([0, 1, 0], source.Requests.Select(request => request.SequenceIndex));
        Assert.Equal((0, 3), (source.Requests[0].MinimumInclusive, source.Requests[0].MaximumExclusive));
        Assert.Equal((0, 2), (source.Requests[1].MinimumInclusive, source.Requests[1].MaximumExclusive));
        Assert.Equal((0, 2), (source.Requests[2].MinimumInclusive, source.Requests[2].MaximumExclusive));
    }

    [Fact]
    public void FixedOrderConsumesNoStartingPlayerRandomness()
    {
        ValidatedGameProfile profile = Validate(ProfileTestFactory.Create());
        ScriptedMatchRandomSource source = new();

        Game game = GameSetup.Create(profile, [new PlayerSetup(8, "First")], source);

        Assert.Equal(8, game.CurrentPlayer.Id);
        Assert.Empty(source.Requests);
    }

    [Fact]
    public void HighestRollRerollsOnlyTiedLeadersInSeatOrder()
    {
        ValidatedGameProfile profile = WithStartingPolicy(ProfileTestFactory.Create(), StartingPlayerPolicyKind.HighestRoll);
        ScriptedMatchRandomSource source = new(
            3, 3,
            4, 2,
            1, 1,
            2, 2,
            5, 1);

        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(20, "First"), new PlayerSetup(9, "Second"), new PlayerSetup(31, "Third")],
            source);

        Assert.Equal(9, game.CurrentPlayer.Id);
        Assert.Equal(10, source.Requests.Count);
        Assert.All(source.Requests, request =>
        {
            Assert.Equal(RandomPurpose.SetupDice, request.Purpose);
            Assert.Equal(1, request.MinimumInclusive);
            Assert.Equal(7, request.MaximumExclusive);
        });
        Assert.Equal(Enumerable.Range(0, 10), source.Requests.Select(request => request.SequenceIndex));
    }

    [Fact]
    public void HighestRollStopsAfterTheBoundedNumberOfTiedRounds()
    {
        ValidatedGameProfile profile = WithStartingPolicy(ProfileTestFactory.Create(), StartingPlayerPolicyKind.HighestRoll);
        RecordingConstantRandomSource source = new(1);

        GameSetupException exception = Assert.Throws<GameSetupException>(() => GameSetup.Create(
            profile,
            [new PlayerSetup(0, "First"), new PlayerSetup(1, "Second")],
            source));

        Assert.Equal(GameSetupErrorKind.StartingPlayerTieLimitExceeded, exception.Kind);
        Assert.Equal("profile.setup.startingPlayerPolicy", exception.Path);
        Assert.Equal(512, source.Requests.Count);
        Assert.Equal(Enumerable.Range(0, 512), source.Requests.Select(request => request.SequenceIndex));
    }

    [Theory]
    [InlineData(RandomSourceErrorKind.Exhausted)]
    [InlineData(RandomSourceErrorKind.OutOfRange)]
    [InlineData(RandomSourceErrorKind.SourceFailure)]
    public void RandomSourceFailuresEscapeTypedWithoutReturningAPartialMatch(RandomSourceErrorKind kind)
    {
        ValidatedGameProfile profile = WithStartingPolicy(ProfileTestFactory.Create(), StartingPlayerPolicyKind.Random);
        IMatchRandomSource source = kind switch
        {
            RandomSourceErrorKind.Exhausted => new ScriptedMatchRandomSource(),
            RandomSourceErrorKind.OutOfRange => new RecordingConstantRandomSource(2),
            RandomSourceErrorKind.SourceFailure => new ThrowingRandomSource(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        RandomSourceException exception = Assert.Throws<RandomSourceException>(() => GameSetup.Create(
            profile,
            [new PlayerSetup(0, "First"), new PlayerSetup(1, "Second")],
            source));

        Assert.Equal(kind, exception.Kind);
        Assert.Equal(RandomPurpose.SetupStartingPlayer, exception.Request.Purpose);
    }

    [Fact]
    public void IdenticalProfileRosterAndScriptProduceEquivalentInitialState()
    {
        ValidatedGameProfile profile = WithStartingPolicy(
            ProfileTestFactory.Create(spaceCount: 27, deckCount: 2, cardsPerDeck: 3),
            StartingPlayerPolicyKind.Random);
        int[] script = [0, 1, 0, 1, 1];

        string first = Snapshot(GameSetup.Create(
            profile,
            [new PlayerSetup(12, "A"), new PlayerSetup(2, "B")],
            new ScriptedMatchRandomSource(script)));
        string second = Snapshot(GameSetup.Create(
            profile,
            [new PlayerSetup(12, "A"), new PlayerSetup(2, "B")],
            new ScriptedMatchRandomSource(script)));

        Assert.Equal(first, second);
    }

    [Fact]
    public void PlayTurnRejectsProfileMatchWithoutAnyMutation()
    {
        ValidatedGameProfile profile = Validate(ProfileTestFactory.Create());
        Game game = GameSetup.Create(profile, [new PlayerSetup(0, "Solo")]);
        List<GameNotification> notifications = [];
        using IDisposable subscription = game.Notifications.Subscribe(notifications.Add);
        string before = Snapshot(game);

        GameActionResult result = game.PlayTurn();

        Assert.Equal(GameActionStatus.Rejected, result.Status);
        Assert.Equal(GameActionRejectionReason.CapabilityExecutionUnavailable, result.RejectionReason);
        Assert.Equal(before, Snapshot(game));
        Assert.Equal(GamePhase.ReadyForTurn, game.Phase);
        Assert.Null(game.PendingDecision);
        Assert.Null(game.LastDiceRoll);
        Assert.Empty(game.Logs.LogList);
        Assert.Empty(notifications);
    }

    [Fact]
    public void PublicSetupBoundaryExposesNoTransportCallbacksOrLegacyFactories()
    {
        MethodInfo create = typeof(GameSetup).GetMethods(BindingFlags.Public | BindingFlags.Static).Single();
        Type[] publicSignature = create.GetParameters().Select(parameter => parameter.ParameterType)
            .Append(create.ReturnType)
            .ToArray();

        Assert.DoesNotContain(publicSignature, type =>
            type == typeof(Stream) ||
            type == typeof(FileInfo) ||
            type == typeof(DirectoryInfo) ||
            type == typeof(Delegate) ||
            type.Namespace?.StartsWith("System.Text.Json", StringComparison.Ordinal) == true);
        Assert.Null(typeof(GameSetup).GetProperty("DefaultProfile", BindingFlags.Public | BindingFlags.Static));
        Assert.Empty(typeof(Game).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(typeof(GameSetup).Assembly.GetType("Monopoly.Core.CoreGameSetup"));
    }

    private static ValidatedGameProfile Validate(GameProfileDefinition definition) =>
        GameProfileValidator.Validate(definition);

    private static ValidatedGameProfile WithStartingPolicy(
        GameProfileDefinition source,
        StartingPlayerPolicyKind policy)
    {
        ProfileSetupDefinition setup = new(
            source.Setup.MinimumPlayers,
            source.Setup.MaximumPlayers,
            source.Setup.DiceCount,
            source.Setup.DieSides,
            source.Setup.StartSpaceId,
            source.Setup.StartingResources,
            policy);
        return Validate(Copy(source, setup: setup));
    }

    private static ValidatedGameProfile ProfileWithStatus()
    {
        GameProfileDefinition source = ProfileTestFactory.Create();
        PresentationToken statusToken = new("status.synthetic");
        ProfilePresentation presentation = new(source.Presentation.Entries.Append(
            new PresentationMetadata(statusToken, "Synthetic status")));
        return Validate(Copy(
            source,
            presentation: presentation,
            statuses: [new StatusDefinition(new StatusId("status.synthetic"), statusToken, 3)]));
    }

    private static GameProfileDefinition Copy(
        GameProfileDefinition source,
        ProfileSetupDefinition? setup = null,
        ProfilePresentation? presentation = null,
        IEnumerable<StatusDefinition>? statuses = null) => new(
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
            statuses ?? source.Statuses,
            source.Policies);

    private static ProfileComponentRegistry Registry(
        IEnumerable<CapabilityId>? capabilities = null,
        IEnumerable<EffectKindId>? effects = null,
        IEnumerable<StatusId>? statuses = null,
        IEnumerable<StartingPlayerPolicyKind>? startingPolicies = null,
        bool supportsRoundLimitedScore = true) => new(
            capabilities ?? [CapabilityKinds.Move, CapabilityKinds.Ownable, CapabilityKinds.Purchasable, CapabilityKinds.UsageFee, CapabilityKinds.Draw],
            effects ?? [EffectKinds.Move, EffectKinds.ResourceChange],
            statuses ?? [],
            startingPolicies ?? [StartingPlayerPolicyKind.FixedOrder, StartingPlayerPolicyKind.Random, StartingPlayerPolicyKind.HighestRoll],
            [PurchaseDeclinePolicyKind.LeaveUnowned],
            [MatchTieBreakPolicy.LowestPlayerId],
            supportsRoundLimitedScore);

    private static string Snapshot(Game game) => JsonSerializer.Serialize(new
    {
        Profile = new { game.Profile!.Id, game.Profile.Revision, game.Profile.Fingerprint },
        Players = game.Players.Select(player => new
        {
            player.Id,
            player.Name,
            player.Position,
            player.CurrentSpaceId,
            Resources = player.Resources.OrderBy(entry => entry.Key).Select(entry => new { entry.Key, entry.Value })
        }),
        CurrentPlayer = game.CurrentPlayer.Id,
        Track = game.Board.Track.SpaceIds,
        Decks = game.Decks.Entries.Select(deck => new { deck.Id, Cards = deck.Cards.Select(card => card.Id) }),
        Ownership = game.Ownership.Entries,
        Statuses = game.Statuses.Entries,
        game.RoundNumber,
        game.CurrentTurn,
        game.Phase,
        game.PendingDecision,
        game.LastDiceRoll,
        Winner = game.Winner?.Id,
        Logs = game.Logs.LogList
    });

    private sealed class RecordingConstantRandomSource(int value) : IMatchRandomSource
    {
        private readonly List<RandomRequest> _requests = [];
        internal IReadOnlyList<RandomRequest> Requests => _requests;

        public int NextInt(RandomRequest request)
        {
            _requests.Add(request);
            return value;
        }
    }

    private sealed class ThrowingRandomSource : IMatchRandomSource
    {
        public int NextInt(RandomRequest request) => throw new InvalidOperationException("setup source failure");
    }
}
