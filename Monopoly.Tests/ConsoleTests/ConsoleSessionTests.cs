using Moq;
using Monopoly.Console;
using Monopoly.Core;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Persistence;
using Monopoly.Tests.TestDoubles;

namespace Monopoly.Tests.ConsoleTests;

public sealed class ConsoleSessionTests
{
    [Fact]
    public void PurchaseDecisionRendersAndResumesWithTheAuthoritativeResponse()
    {
        ValidatedGameProfile profile = PurchasableProfile();
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(0, "Aster")],
            ScriptedMatchRandomSource.ForDice(1));
        TestConsole console = new("1", "1", "5");
        Mock<IGameSaveStore> store = new(MockBehavior.Strict);

        new ConsoleGameSession(console, store.Object).Run(game, "Ready.");

        Assert.Equal(0, game.Ownership.BySpaceId[new SpaceId("space.execution-1")].OwnerPlayerId);
        Assert.Contains("may acquire Execution Space 1 for 5", console.Output, StringComparison.Ordinal);
        Assert.Contains("Aster chose accept", console.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Aster acquired Execution Space 1", console.Output, StringComparison.Ordinal);
        Assert.Equal(0, game.NotificationSubscriberCount);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public void ReadyPendingAndTerminalMatchesCanBeSavedExplicitly()
    {
        ValidatedGameProfile readyProfile = ExecutionProfileFactory.Create();
        Game ready = GameSetup.Create(
            readyProfile,
            [new PlayerSetup(0, "Ready")],
            new MinimumMatchRandomSource());
        AssertSavedInSession(ready, new TestConsole("4", "5"));

        ValidatedGameProfile pendingProfile = PurchasableProfile();
        Game pending = GameSetup.Create(
            pendingProfile,
            [new PlayerSetup(0, "Pending")],
            ScriptedMatchRandomSource.ForDice(1));
        Assert.Equal(GameActionStatus.DecisionRequired, pending.PlayTurn().Status);
        AssertSavedInSession(pending, new TestConsole("5", "6"));

        ValidatedGameProfile terminalProfile = ExecutionProfileFactory.Create(roundLimit: 1);
        Game terminal = GameSetup.Create(
            terminalProfile,
            [new PlayerSetup(0, "Winner")],
            ScriptedMatchRandomSource.ForDice(1));
        Assert.Equal(GameActionStatus.GameOver, terminal.PlayTurn().Status);
        TestConsole terminalConsole = new("3", "4");
        AssertSavedInSession(terminal, terminalConsole);
        Assert.Contains("Winner: Winner", terminalConsole.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void RestoredPendingDecisionCanBeResolvedWithoutReplayingTheTurn()
    {
        ValidatedGameProfile profile = PurchasableProfile();
        Game pending = GameSetup.Create(
            profile,
            [new PlayerSetup(0, "Aster")],
            ScriptedMatchRandomSource.ForDice(1));
        Assert.Equal(GameActionStatus.DecisionRequired, pending.PlayTurn().Status);
        int positionBeforeResume = pending.CurrentPlayer.Position;
        TestConsole console = new("2", "5");
        Mock<IGameSaveStore> store = new(MockBehavior.Strict);

        new ConsoleGameSession(console, store.Object).Run(pending, "Saved match loaded.");

        Assert.Equal(positionBeforeResume, pending.Players[0].Position);
        Assert.Null(pending.Ownership.BySpaceId[new SpaceId("space.execution-1")].OwnerPlayerId);
        Assert.Contains("Aster chose decline", console.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, pending.NotificationSubscriberCount);
    }

    [Fact]
    public void ReturningFromSessionDoesNotAutoSave()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create();
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(0, "Aster")],
            new MinimumMatchRandomSource());
        Mock<IGameSaveStore> store = new(MockBehavior.Strict);

        new ConsoleGameSession(new TestConsole("5"), store.Object).Run(game, "Ready.");

        store.VerifyNoOtherCalls();
        Assert.Equal(0, game.NotificationSubscriberCount);
    }

    [Fact]
    public void RandomFailureIsReportedWithoutEndingTheSession()
    {
        ValidatedGameProfile profile = ExecutionProfileFactory.Create();
        Game game = GameSetup.Create(
            profile,
            [new PlayerSetup(0, "Aster")],
            ScriptedMatchRandomSource.ForDice());
        TestConsole console = new("1", "5");
        Mock<IGameSaveStore> store = new(MockBehavior.Strict);

        new ConsoleGameSession(console, store.Object).Run(game, "Ready.");

        Assert.Contains("random source failed", console.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(GamePhase.ReadyForTurn, game.Phase);
        Assert.Equal(0, game.NotificationSubscriberCount);
    }

    private static ValidatedGameProfile PurchasableProfile() => ExecutionProfileFactory.Create(
        spaceCapabilities: new Dictionary<int, IReadOnlyList<CapabilityDefinition>>
        {
            [1] =
            [
                new OwnableCapabilityDefinition(new GroupId("group.sample")),
                new PurchasableCapabilityDefinition(new ResourceAmount(ExecutionProfileFactory.Credits, 5)),
                new UsageFeeCapabilityDefinition(new ResourceAmount(ExecutionProfileFactory.Credits, 2))
            ]
        });

    private static void AssertSavedInSession(Game game, TestConsole console)
    {
        Mock<IGameSaveStore> store = new();
        store.Setup(candidate => candidate.Save(It.Is<Game>(saved => ReferenceEquals(saved, game))));

        new ConsoleGameSession(console, store.Object).Run(game, "State.");

        Assert.Contains("Match saved.", console.Output, StringComparison.Ordinal);
        store.Verify(candidate => candidate.Save(It.Is<Game>(saved => ReferenceEquals(saved, game))), Times.Once);
        Assert.Equal(0, game.NotificationSubscriberCount);
    }
}
