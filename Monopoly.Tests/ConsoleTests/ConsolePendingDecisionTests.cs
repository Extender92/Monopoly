using Monopoly.Console;
using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core;
using Monopoly.Core.Models;
using Monopoly.Core.Persistence;
using Monopoly.Tests.CoreTests;
using Moq;

namespace Monopoly.Tests.ConsoleTests;

public sealed class ConsolePendingDecisionTests
{
    [Theory]
    [InlineData(0, DecisionOption.Purchase)]
    [InlineData(1, DecisionOption.Decline)]
    public void PurchasePromptMapsUserChoiceToCoreOption(int selectedIndex, DecisionOption expected)
    {
        Game game = new GameTestBuilder()
            .WithRandomValues(1, 2)
            .Build();
        PropertyPurchaseDecision decision = Assert.IsType<PropertyPurchaseDecision>(game.PlayTurn().PendingDecision);
        DecisionFixture fixture = new(game, selectedIndex);

        DecisionResponse response = fixture.Provider.GetResponse(decision);

        Assert.Equal(decision.DecisionId, response.DecisionId);
        Assert.Equal(expected, response.Response);
        fixture.Console.Verify(wrapper => wrapper.Write(
            It.Is<string>(message => message.Contains(game.Presentation.ResolveDisplayText(game.Board.GetSquareAtPosition(3).PresentationToken), StringComparison.Ordinal) &&
                                     message.Contains(decision.Price.ToString(), StringComparison.Ordinal))), Times.Once);
    }

    [Theory]
    [InlineData(0, DecisionOption.LeaveJail)]
    [InlineData(1, DecisionOption.RollForDoubles)]
    public void JailPromptUsesConfiguredCoreFineAndMapsChoice(int selectedIndex, DecisionOption expected)
    {
        Game game = new GameTestBuilder(new GameRules(2, 2, 6, jailFine: 73))
            .WithPlayerInJail(0)
            .Build();
        JailReleaseDecision decision = Assert.IsType<JailReleaseDecision>(game.PlayTurn().PendingDecision);
        DecisionFixture fixture = new(game, selectedIndex);

        DecisionResponse response = fixture.Provider.GetResponse(decision);

        Assert.Equal(expected, response.Response);
        fixture.Console.Verify(wrapper => wrapper.Write(
            It.Is<string>(message => message.Contains("73", StringComparison.Ordinal))), Times.Once);
        fixture.Console.Verify(wrapper => wrapper.Write(
            It.Is<string>(message => message.Contains("50", StringComparison.Ordinal))), Times.Never);
    }

    [Fact]
    public void ConsoleSynchronouslyDrivesJailThenPurchaseUntilTurnCompletes()
    {
        ScriptedMatchRandomSource randomSource = new(1, 1);
        Game game = new GameTestBuilder()
            .WithPlayerInJail(0)
            .WithRandomSource(randomSource)
            .Build();
        DecisionFixture fixture = new(game, 1, 1);
        ConsoleLogPrinter logPrinter = new(fixture.Console.Object);
        ConsoleCardPrinter cardPrinter = new(fixture.Console.Object, game.Board.Squares, game.Rules);
        ConsoleGame consoleGame = new(
            game,
            fixture.Printer,
            new List<TablePiece>(),
            fixture.DecisionInput,
            logPrinter,
            cardPrinter,
            fixture.SaveStore.Object,
            fixture.Provider);
        GameActionResult jailRequired = game.PlayTurn();

        GameActionResult completed = consoleGame.ResolvePendingDecisions(jailRequired);

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        Assert.Equal(GamePhase.ReadyForTurn, game.Phase);
        Assert.Null(game.PendingDecision);
        Assert.Equal(2, fixture.Menu.CallCount);
        Assert.Equal(2, randomSource.Requests.Count(request => request.Purpose == RandomPurpose.DetentionDice));
        Assert.Null(game.Board.GetSquareAtPosition(12).Owner);
    }

    [Fact]
    public void ConsoleDisplaysTypedRejectionBeforeResolvingCurrentDecision()
    {
        Game game = new GameTestBuilder()
            .WithRandomValues(1, 2)
            .Build();
        _ = game.PlayTurn();
        GameActionResult rejected = game.PlayTurn();
        DecisionFixture fixture = new(game, 1);
        ConsoleGame consoleGame = new(
            game,
            fixture.Printer,
            new List<TablePiece>(),
            fixture.DecisionInput,
            new ConsoleLogPrinter(fixture.Console.Object),
            new ConsoleCardPrinter(fixture.Console.Object, game.Board.Squares, game.Rules),
            fixture.SaveStore.Object,
            fixture.Provider);

        GameActionResult completed = consoleGame.ResolvePendingDecisions(rejected);

        Assert.Equal(GameActionStatus.TurnCompleted, completed.Status);
        fixture.Console.Verify(wrapper => wrapper.Write(
            It.Is<string>(message => message.Contains(
                nameof(GameActionRejectionReason.PendingDecisionRequired),
                StringComparison.Ordinal))), Times.Once);
    }

    private sealed class DecisionFixture
    {
        internal DecisionFixture(Game game, params int[] selections)
        {
            Console = new Mock<IConsoleWrapper>();
            Menu = new QueueMenuSelector(selections);
            DecisionInput = new Input(Console.Object, Menu);
            Printer = new ConsolePrinter(Console.Object, game.Board.Squares, game.Rules);
            SaveStore = new Mock<IGameSaveStore>();
            Provider = new ConsolePlayerDecisionProvider(Printer, DecisionInput, game, SaveStore.Object);
        }

        internal Mock<IConsoleWrapper> Console { get; }
        internal QueueMenuSelector Menu { get; }
        internal Input DecisionInput { get; }
        internal ConsolePrinter Printer { get; }
        internal Mock<IGameSaveStore> SaveStore { get; }
        internal ConsolePlayerDecisionProvider Provider { get; }
    }

    private sealed class QueueMenuSelector(IEnumerable<int> selections) : IMenuOptionSelector
    {
        private readonly Queue<int> _selections = new(selections);

        internal int CallCount { get; private set; }

        public int GetSelectedOption(
            List<string> options,
            int spacingPerLine = 18,
            int index = 0,
            int optionsPerLine = 1,
            bool canCancel = false,
            ConsoleColor selectColor = ConsoleColor.Red)
        {
            CallCount++;
            return _selections.Dequeue();
        }

        public void SetPositions()
        {
        }
    }

}
