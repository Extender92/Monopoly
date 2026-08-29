using Monopoly.Core.Interface;
using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;

namespace Monopoly.Tests.CoreTests;

public sealed class GameFlowIntegrationTests
{
    [Fact]
    public void ExactMoneyPaysRentWithoutBankruptcy()
    {
        GameRules rules = new(2, 2, 6);
        int rent = ((PropertySquare)new GameBoard(rules).GetSquareAtPosition(1)).Rent;
        TestDecisionProvider decisions = new();
        Game game = new GameTestBuilder(rules)
            .WithSquare(1, ownerId: 0)
            .WithPlayer(1, money: rent)
            .WithDecisions(decisions)
            .Build();
        Player owner = game.Players[0];
        Player debtor = game.Players[1];
        PropertySquare property = (PropertySquare)game.Board.GetSquareAtPosition(1);

        property.LandOn(debtor, game);

        Assert.False(debtor.IsBankrupt);
        Assert.Equal(0, debtor.Money);
        Assert.Equal(3000 + property.Rent, owner.Money);
        Assert.Empty(decisions.PaymentRequests);
    }

    [Fact]
    public void RentResolutionReceivesActualRentInsteadOfPropertyPrice()
    {
        TestDecisionProvider decisions = new() { ResolveFunds = true };
        Game game = new GameTestBuilder()
            .WithSquare(1, ownerId: 0)
            .WithSquare(3, ownerId: 1)
            .WithPlayer(1, money: 0)
            .WithDecisions(decisions)
            .Build();
        Player owner = game.Players[0];
        Player debtor = game.Players[1];
        PropertySquare property = (PropertySquare)game.Board.GetSquareAtPosition(1);

        property.LandOn(debtor, game);

        Assert.Equal(new[] { property.Rent }, decisions.PaymentRequests);
        Assert.False(debtor.IsBankrupt);
        Assert.Equal(0, debtor.Money);
        Assert.Equal(3000 + property.Rent, owner.Money);
    }

    [Fact]
    public void ExactMoneyPaysTax()
    {
        GameRules rules = new(2, 2, 6);
        int taxAmount = ((TaxSquare)new GameBoard(rules).GetSquareAtPosition(4)).Price;
        Game game = new GameTestBuilder(rules).WithPlayer(1, money: taxAmount).Build();
        Player debtor = game.Players[1];
        TaxSquare tax = (TaxSquare)game.Board.GetSquareAtPosition(4);

        tax.LandOn(debtor, game);

        Assert.False(debtor.IsBankrupt);
        Assert.Equal(0, debtor.Money);
    }

    [Fact]
    public void InsufficientFundsWithoutProgressCausesBankruptcyAndWinner()
    {
        TestDecisionProvider decisions = new();
        Game game = new GameTestBuilder()
            .WithCurrentPlayer(1)
            .WithSquare(1, ownerId: 0)
            .WithPlayer(1, money: 0)
            .WithDecisions(decisions)
            .Build();
        Player owner = game.Players[0];
        Player debtor = game.Players[1];
        PropertySquare property = (PropertySquare)game.Board.GetSquareAtPosition(1);

        property.LandOn(debtor, game);

        Assert.False(decisions.ResolveFunds);
        Assert.True(debtor.IsBankrupt);
        Assert.DoesNotContain(debtor, game.Players);
        Assert.Same(owner, game.Winner);
        Assert.True(game.IsGameOver);
    }

    [Fact]
    public void PlayTurnWrapsAroundGoAndPaysSalary()
    {
        Game game = new GameTestBuilder()
            .WithPlayer(0, money: 1000, position: 39)
            .WithRandomValues(2, 2)
            .Build();
        Player player = game.Players[0];

        TurnResult result = game.PlayTurnToCompletion();

        Assert.Equal(3, player.Position);
        Assert.Equal(1200, player.Money);
        Assert.Same(game.Board.GetSquareAtPosition(3), result.LandedSquare);
    }

    [Fact]
    public void JailDoublesReleasePlayerMoveWithWrapAndLandWithoutExtraTurn()
    {
        Game game = new GameTestBuilder(new GameRules(2, 2, 30))
            .WithPlayerInJail(0)
            .WithRandomValues(21, 21)
            .Build();
        Player player = game.Players[0];
        Player next = game.Players[1];

        TurnResult result = game.PlayTurnToCompletion();

        Assert.False(game.TheJail.IsPlayerInJail(player));
        Assert.Equal(12, player.Position);
        Assert.Same(game.Board.GetSquareAtPosition(12), result.LandedSquare);
        Assert.True(result.WasReleasedFromJailByDouble);
        Assert.False(result.ExtraTurn);
        Assert.Same(next, game.CurrentPlayer);
    }

    [Fact]
    public void PaidJailReleaseFollowedByNonDoubleCompletesWithoutStaleStateLookup()
    {
        TestDecisionProvider decisions = new();
        Game game = new GameTestBuilder()
            .WithPlayer(0, money: 100)
            .WithPlayerInJail(0)
            .WithRandomValues(1, 4)
            .WithDecisions(decisions)
            .Build();
        Player player = game.Players[0];
        Player next = game.Players[1];

        TurnResult? result = null;
        Exception? exception = Record.Exception(() => result = game.PlayTurnToCompletion(
            decision => decision is JailReleaseDecision ? DecisionOption.LeaveJail : DecisionOption.Decline));

        Assert.Null(exception);
        Assert.NotNull(result);
        Assert.Equal(new[] { 1, 4 }, result.DiceResults);
        Assert.False(result.WasDouble);
        Assert.Equal(50, player.Money);
        Assert.False(game.TheJail.TryGetJailInfo(player, out _));
        Assert.Same(next, game.CurrentPlayer);
    }

    [Fact]
    public void PaidJailReleaseFollowedByDoubleDoesNotReleasePlayerTwice()
    {
        TestDecisionProvider decisions = new();
        Game game = new GameTestBuilder()
            .WithPlayer(0, money: 100)
            .WithPlayerInJail(0)
            .WithRandomValues(2, 2)
            .WithDecisions(decisions)
            .Build();
        Player player = game.Players[0];
        Player next = game.Players[1];

        TurnResult? result = null;
        Exception? exception = Record.Exception(() => result = game.PlayTurnToCompletion(
            decision => decision is JailReleaseDecision ? DecisionOption.LeaveJail : DecisionOption.Decline));

        Assert.Null(exception);
        Assert.NotNull(result);
        Assert.Equal(new[] { 2, 2 }, result.DiceResults);
        Assert.True(result.WasDouble);
        Assert.False(result.WasReleasedFromJailByDouble);
        Assert.Equal(50, player.Money);
        Assert.False(game.TheJail.TryGetJailInfo(player, out _));
        Assert.Same(next, game.CurrentPlayer);
    }

    [Fact]
    public void ThirdConsecutiveDoublesSendPlayerDirectlyToJail()
    {
        Game game = new GameTestBuilder()
            .WithPlayer(0, position: 1)
            .WithRandomValues(1, 1, 1, 1, 1, 1)
            .Build();
        Player player = game.Players[0];
        Player next = game.Players[1];

        game.PlayTurnToCompletion();
        game.PlayTurnToCompletion();
        TurnResult result = game.PlayTurnToCompletion();

        Assert.True(game.TheJail.IsPlayerInJail(player));
        Assert.True(result.WasSentToJail);
        Assert.Null(result.LandedSquare);
        Assert.Equal(0, game.ConsecutiveDoubles);
        Assert.Same(next, game.CurrentPlayer);
    }

    [Fact]
    public void BankruptcyToPlayerTransfersAssetsMortgageAndJailCards()
    {
        Game game = new GameTestBuilder()
            .WithCurrentPlayer(1)
            .WithPlayer(1, money: 125, jailCards: 1)
            .WithSquare(1, ownerId: 1, houses: 3)
            .WithSquare(5, ownerId: 1, isMortgage: true)
            .Build();
        Player creditor = game.Players[0];
        Player debtor = game.Players[1];
        PropertySquare property = (PropertySquare)game.Board.GetSquareAtPosition(1);
        Square mortgagedSquare = game.Board.GetSquareAtPosition(5);
        int houseValue = game.Handler.CalculateHouseAndHotelValue(property);

        game.Handler.DeclareBankruptcy(debtor, creditor, "Could not pay rent");

        Assert.Same(creditor, property.Owner);
        Assert.Same(creditor, mortgagedSquare.Owner);
        Assert.True(mortgagedSquare.IsMortgage);
        Assert.Equal(0, property.Houses);
        Assert.Equal(3000 + 125 + houseValue, creditor.Money);
        Assert.Equal(1, creditor.NumberOfGetOutOFJailCards);
        Assert.True(debtor.IsBankrupt);
        Assert.DoesNotContain(debtor, game.Players);
        Assert.Same(creditor, game.Winner);
    }

    [Fact]
    public void BankruptcyToBankClearsAssetsAndMortgageState()
    {
        Game game = new GameTestBuilder()
            .WithSquare(1, ownerId: 1, houses: 2)
            .WithSquare(5, ownerId: 1, isMortgage: true)
            .Build();
        Player debtor = game.Players[1];
        PropertySquare property = (PropertySquare)game.Board.GetSquareAtPosition(1);
        Square mortgagedSquare = game.Board.GetSquareAtPosition(5);

        game.Handler.DeclareBankruptcy(debtor, null, "Could not pay tax");

        Assert.Null(property.Owner);
        Assert.Null(mortgagedSquare.Owner);
        Assert.False(mortgagedSquare.IsMortgage);
        Assert.Equal(0, property.Houses);
        Assert.True(debtor.IsBankrupt);
    }

    [Fact]
    public void PlayerCreditorBankruptcyDuringPlayTurnAdvancesToImmediateNextPlayer()
    {
        Game game = new GameTestBuilder(3)
            .WithSquare(3, ownerId: 2)
            .WithPlayer(0, money: 0)
            .WithTurn(4, consecutiveDoubles: 1)
            .WithRandomValues(1, 2)
            .Build();
        Player debtor = game.Players[0];
        Player expectedNext = game.Players[1];
        Player creditor = game.Players[2];

        TurnResult result = game.PlayTurnToCompletion();

        Assert.True(result.PlayerBankrupt);
        Assert.False(result.ExtraTurn);
        Assert.False(result.GameOver);
        Assert.Null(result.Winner);
        Assert.DoesNotContain(debtor, game.Players);
        Assert.Same(expectedNext, game.CurrentPlayer);
        Assert.Equal(1, game.CurrentTurn);
        Assert.Equal(0, game.ConsecutiveDoubles);
    }

    [Fact]
    public void BankCreditorBankruptcyDuringPlayTurnAdvancesToImmediateNextPlayer()
    {
        Game game = new GameTestBuilder(3)
            .WithPlayer(0, money: 0)
            .WithTurn(3, consecutiveDoubles: 2)
            .WithRandomValues(1, 3)
            .Build();
        Player debtor = game.Players[0];
        Player expectedNext = game.Players[1];

        TurnResult result = game.PlayTurnToCompletion();

        Assert.IsType<TaxSquare>(result.LandedSquare);
        Assert.True(result.PlayerBankrupt);
        Assert.False(result.ExtraTurn);
        Assert.False(result.GameOver);
        Assert.Null(result.Winner);
        Assert.DoesNotContain(debtor, game.Players);
        Assert.Same(expectedNext, game.CurrentPlayer);
        Assert.Equal(1, game.CurrentTurn);
        Assert.Equal(0, game.ConsecutiveDoubles);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(3, 0)]
    public void BankruptcyOutsidePlayTurnAdvancesFromOriginalPosition(int currentIndex, int expectedNextIndex)
    {
        Game game = new GameTestBuilder(4)
            .WithCurrentPlayer(currentIndex)
            .WithTurn(5, consecutiveDoubles: 2)
            .Build();
        Player debtor = game.Players[currentIndex];
        Player expectedNext = game.Players[expectedNextIndex];

        game.Handler.DeclareBankruptcy(debtor, null, "Could not pay bank debt");

        Assert.DoesNotContain(debtor, game.Players);
        Assert.Same(expectedNext, game.CurrentPlayer);
        Assert.Equal(1, game.CurrentTurn);
        Assert.Equal(0, game.ConsecutiveDoubles);
        Assert.Null(game.Winner);
    }

    [Fact]
    public void NonCurrentPlayerBankruptcyDoesNotRotateOrResetTurnState()
    {
        Game game = new GameTestBuilder(4)
            .WithTurn(6, consecutiveDoubles: 2)
            .Build();
        Player current = game.Players[0];
        Player creditor = game.Players[1];
        Player debtor = game.Players[2];

        game.Handler.DeclareBankruptcy(debtor, creditor, "Could not pay player debt");

        Assert.DoesNotContain(debtor, game.Players);
        Assert.Same(current, game.CurrentPlayer);
        Assert.Equal(6, game.CurrentTurn);
        Assert.Equal(2, game.ConsecutiveDoubles);
        Assert.Null(game.Winner);
    }

    [Fact]
    public void RemovingAlreadyRemovedPlayerDoesNotAdvanceAgain()
    {
        Game game = new GameTestBuilder(4).Build();
        Player removed = game.Players[0];
        Player expectedCurrent = game.Players[1];

        game.RemovePlayer(removed);
        game.RemovePlayer(removed);

        Assert.Same(expectedCurrent, game.CurrentPlayer);
        Assert.Equal(3, game.Players.Count);
    }

    [Fact]
    public void FinalBankruptcySetsSurvivorAsCurrentWinnerAndStopsFurtherTurns()
    {
        ScriptedMatchRandomSource randomSource = new(1, 2);
        Game game = new GameTestBuilder()
            .WithSquare(3, ownerId: 1)
            .WithPlayer(0, money: 0)
            .WithRandomSource(randomSource)
            .Build();
        Player debtor = game.Players[0];
        Player survivor = game.Players[1];

        TurnResult bankruptcyResult = game.PlayTurnToCompletion();
        TurnResult gameOverResult = game.PlayTurnToCompletion();

        Assert.True(bankruptcyResult.PlayerBankrupt);
        Assert.True(bankruptcyResult.GameOver);
        Assert.Same(survivor, bankruptcyResult.Winner);
        Assert.Same(survivor, game.CurrentPlayer);
        Assert.Same(survivor, game.Winner);
        Assert.True(gameOverResult.GameOver);
        Assert.Same(survivor, gameOverResult.Winner);
        Assert.Equal(2, randomSource.Requests.Count(request => request.Purpose == RandomPurpose.TurnDice));
    }

    private sealed class TestDecisionProvider : IPlayerDecisionProvider
    {
        public bool ResolveFunds { get; init; }
        public List<int> PaymentRequests { get; } = new();

        public bool ResolveInsufficientFunds(Game game, Player player, int amount)
        {
            PaymentRequests.Add(amount);
            if (!ResolveFunds) return false;
            game.Transactions.GetMoneyFromBank(player, amount);
            return true;
        }
    }
}
