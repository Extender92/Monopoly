using Monopoly.Core.Persistence;
using Monopoly.Tests.TestDoubles;

namespace Monopoly.Tests.CoreTests;

public sealed class GameStateV2Tests
{
    [Fact]
    public void CapturedStateOwnsImmutableDetachedCollections()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create(spaceCount: 3);
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(8, "Aster")],
            new MinimumMatchRandomSource());

        GameStateV2 state = GameStateV2Mapper.Capture(game);

        Assert.Throws<NotSupportedException>(() => ((IList<PlayerStateV2>)state.Players).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<ResourceBalanceStateV2>)state.Players[0].Resources).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<DeckStateV2>)state.Decks).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<OwnershipStateV2>)state.ModuleState.Ownership).Clear());
        Assert.Single(state.Players);
        Assert.Equal(2, state.Players[0].Resources.Count);
        Assert.Equal(GamePhase.ReadyForTurn, game.Phase);
    }

    [Fact]
    public void ProfileRegistryRequiresOneUnambiguousIdentityAndExactFingerprint()
    {
        ValidatedGameProfile first = ExecutionProfileFactory.Create(spaceCount: 3);
        ValidatedGameProfile changed = ExecutionProfileFactory.Create(spaceCount: 4);
        GameProfileRegistry registry = new([first]);

        Assert.Same(first, registry.ResolveExact(first.Id, first.Revision, first.Fingerprint));
        GameProfileResolutionException mismatch = Assert.Throws<GameProfileResolutionException>(() =>
            registry.ResolveExact(changed.Id, changed.Revision, changed.Fingerprint));
        Assert.Equal(GameProfileResolutionErrorKind.FingerprintMismatch, mismatch.Kind);
        Assert.Throws<ArgumentException>(() => new GameProfileRegistry([first, changed]));
        Assert.Throws<ArgumentException>(() => new GameProfileRegistry([]));
        Assert.Throws<NotSupportedException>(() =>
            ((IList<ValidatedGameProfile>)registry.Profiles).Clear());
    }
}
