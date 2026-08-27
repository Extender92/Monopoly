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
        (Game game, Player owner, Player debtor, TestDecisionProvider decisions) = CreateGame();
        PropertySquare property = game.Board.GetAllPropertySquares().First();
        property.Owner = owner;
        debtor.Money = property.Rent;

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
        (Game game, Player owner, Player debtor, _) = CreateGame(decisions);
        PropertySquare property = game.Board.GetAllPropertySquares().First();
        PropertySquare asset = game.Board.GetAllPropertySquares().Skip(1).First();
        property.Owner = owner;
        asset.Owner = debtor;
        debtor.Money = 0;

        property.LandOn(debtor, game);

        Assert.Equal(new[] { property.Rent }, decisions.PaymentRequests);
        Assert.False(debtor.IsBankrupt);
        Assert.Equal(0, debtor.Money);
        Assert.Equal(3000 + property.Rent, owner.Money);
    }

    [Fact]
    public void ExactMoneyPaysTax()
    {
        (Game game, _, Player debtor, _) = CreateGame();
        TaxSquare tax = game.Board.GetAllSquaresOfType<TaxSquare>().First();
        debtor.Money = tax.Price;

        tax.LandOn(debtor, game);

        Assert.False(debtor.IsBankrupt);
        Assert.Equal(0, debtor.Money);
    }

    [Fact]
    public void InsufficientFundsWithoutProgressCausesBankruptcyAndWinner()
    {
        (Game game, Player owner, Player debtor, TestDecisionProvider decisions) = CreateGame();
        game.CurrentPlayer = debtor;
        PropertySquare property = game.Board.GetAllPropertySquares().First();
        property.Owner = owner;
        debtor.Money = 0;

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
        (Game game, Player player, _, _) = CreateGame();
        game.Dice = new List<IDie> { new FixedDie(2), new FixedDie(2) };
        player.Position = 39;
        player.Money = 1000;

        TurnResult result = game.PlayTurn();

        Assert.Equal(3, player.Position);
        Assert.Equal(1200, player.Money);
        Assert.Same(game.Board.GetSquareAtPosition(3), result.LandedSquare);
    }

    [Fact]
    public void JailDoublesReleasePlayerMoveWithWrapAndLandWithoutExtraTurn()
    {
        (Game game, Player player, Player next, _) = CreateGame(null, new GameRules(2, 2, 30));
        game.CurrentPlayer = player;
        game.TheJail.PlayerGoToJail(player);
        game.Dice = new List<IDie> { new FixedDie(21), new FixedDie(21) };

        TurnResult result = game.PlayTurn();

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
        TestDecisionProvider decisions = new() { ConfirmJailBuyoutResult = true };
        (Game game, Player player, Player next, _) = CreateGame(decisions);
        game.CurrentPlayer = player;
        player.Money = 100;
        game.TheJail.PlayerGoToJail(player);
        game.Dice = new List<IDie> { new FixedDie(1), new FixedDie(4) };

        TurnResult? result = null;
        Exception? exception = Record.Exception(() => result = game.PlayTurn());

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
        TestDecisionProvider decisions = new() { ConfirmJailBuyoutResult = true };
        (Game game, Player player, Player next, _) = CreateGame(decisions);
        game.CurrentPlayer = player;
        player.Money = 100;
        game.TheJail.PlayerGoToJail(player);
        game.Dice = new List<IDie> { new FixedDie(2), new FixedDie(2) };

        TurnResult? result = null;
        Exception? exception = Record.Exception(() => result = game.PlayTurn());

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
        (Game game, Player player, Player next, _) = CreateGame();
        player.Position = 1;
        game.Dice = new List<IDie> { new FixedDie(1, 1, 1), new FixedDie(1, 1, 1) };

        game.PlayTurn();
        game.PlayTurn();
        TurnResult result = game.PlayTurn();

        Assert.True(game.TheJail.IsPlayerInJail(player));
        Assert.True(result.WasSentToJail);
        Assert.Null(result.LandedSquare);
        Assert.Equal(0, game.ConsecutiveDoubles);
        Assert.Same(next, game.CurrentPlayer);
    }

    [Fact]
    public void BankruptcyToPlayerTransfersAssetsMortgageAndJailCards()
    {
        (Game game, Player creditor, Player debtor, _) = CreateGame();
        game.CurrentPlayer = debtor;
        PropertySquare property = game.Board.GetAllPropertySquares().First();
        property.Owner = debtor;
        property.Houses = 3;
        property.IsMortgage = true;
        debtor.Money = 125;
        debtor.NumberOfGetOutOFJailCards = 1;
        int houseValue = game.Handler.CalculateHouseAndHotelValue(property);

        game.Handler.DeclareBankruptcy(debtor, creditor, "Could not pay rent");

        Assert.Same(creditor, property.Owner);
        Assert.True(property.IsMortgage);
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
        (Game game, _, Player debtor, _) = CreateGame();
        PropertySquare property = game.Board.GetAllPropertySquares().First();
        property.Owner = debtor;
        property.Houses = 2;
        property.IsMortgage = true;

        game.Handler.DeclareBankruptcy(debtor, null, "Could not pay tax");

        Assert.Null(property.Owner);
        Assert.False(property.IsMortgage);
        Assert.Equal(0, property.Houses);
        Assert.True(debtor.IsBankrupt);
    }

    [Fact]
    public void PlayerCreditorBankruptcyDuringPlayTurnAdvancesToImmediateNextPlayer()
    {
        Game game = CreateGame(3);
        Player debtor = game.Players[0];
        Player expectedNext = game.Players[1];
        Player creditor = game.Players[2];
        PropertySquare property = game.Board.GetAllPropertySquares().Single(square => square.Position == 3);
        property.Owner = creditor;
        debtor.Money = 0;
        game.CurrentTurn = 4;
        game.RestoreConsecutiveDoubles(1);
        game.Dice = new List<IDie> { new FixedDie(1), new FixedDie(2) };

        TurnResult result = game.PlayTurn();

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
        Game game = CreateGame(3);
        Player debtor = game.Players[0];
        Player expectedNext = game.Players[1];
        debtor.Money = 0;
        game.CurrentTurn = 3;
        game.RestoreConsecutiveDoubles(2);
        game.Dice = new List<IDie> { new FixedDie(1), new FixedDie(3) };

        TurnResult result = game.PlayTurn();

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
        Game game = CreateGame(4);
        Player debtor = game.Players[currentIndex];
        Player expectedNext = game.Players[expectedNextIndex];
        game.CurrentPlayer = debtor;
        game.CurrentTurn = 5;
        game.RestoreConsecutiveDoubles(2);

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
        Game game = CreateGame(4);
        Player current = game.Players[0];
        Player creditor = game.Players[1];
        Player debtor = game.Players[2];
        game.CurrentPlayer = current;
        game.CurrentTurn = 6;
        game.RestoreConsecutiveDoubles(2);

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
        Game game = CreateGame(4);
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
        (Game game, Player debtor, Player survivor, _) = CreateGame();
        PropertySquare property = game.Board.GetAllPropertySquares().Single(square => square.Position == 3);
        property.Owner = survivor;
        debtor.Money = 0;
        FixedDie firstDie = new(1);
        FixedDie secondDie = new(2);
        game.Dice = new List<IDie> { firstDie, secondDie };

        TurnResult bankruptcyResult = game.PlayTurn();
        TurnResult gameOverResult = game.PlayTurn();

        Assert.True(bankruptcyResult.PlayerBankrupt);
        Assert.True(bankruptcyResult.GameOver);
        Assert.Same(survivor, bankruptcyResult.Winner);
        Assert.Same(survivor, game.CurrentPlayer);
        Assert.Same(survivor, game.Winner);
        Assert.True(gameOverResult.GameOver);
        Assert.Same(survivor, gameOverResult.Winner);
        Assert.Equal(1, firstDie.RollCount);
        Assert.Equal(1, secondDie.RollCount);
    }

    private static (Game Game, Player First, Player Second, TestDecisionProvider Decisions) CreateGame(
        TestDecisionProvider? decisions = null)
    {
        return CreateGame(decisions, new GameRules(2, 2, 6));
    }

    private static Game CreateGame(int playerCount)
    {
        return CoreGameSetup.Setup(new GameRules(playerCount, 2, 6), new TestDecisionProvider());
    }

    private static (Game Game, Player First, Player Second, TestDecisionProvider Decisions) CreateGame(
        TestDecisionProvider? decisions,
        GameRules rules)
    {
        decisions ??= new TestDecisionProvider();
        Game game = CoreGameSetup.Setup(rules, decisions);
        return (game, game.Players[0], game.Players[1], decisions);
    }

    private sealed class FixedDie : IDie
    {
        private readonly Queue<int> values;
        private int result;

        public int RollCount { get; private set; }

        public FixedDie(params int[] values)
        {
            this.values = new Queue<int>(values);
            result = values.Length > 0 ? values[0] : 1;
        }

        public int GetDieResult() => result;
        public int GetDieType() => 20;
        public void Roll()
        {
            RollCount++;
            result = values.Count > 0 ? values.Dequeue() : result;
        }
        public void ScrambleDie() => result = -1;
    }

    private sealed class TestDecisionProvider : IPlayerDecisionProvider
    {
        public bool ResolveFunds { get; init; }
        public bool ConfirmJailBuyoutResult { get; init; }
        public List<int> PaymentRequests { get; } = new();

        public bool ConfirmPurchase(Player player, Square square) => false;
        public bool ConfirmJailBuyout(Player player) => ConfirmJailBuyoutResult;

        public bool ResolveInsufficientFunds(Game game, Player player, int amount)
        {
            PaymentRequests.Add(amount);
            if (!ResolveFunds) return false;
            player.Money += amount;
            return true;
        }
    }
}
