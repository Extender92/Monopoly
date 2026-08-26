using Monopoly.Core.Interface;
using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.SaveAndLoad;

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

    [Theory]
    [InlineData(GameRules.Language.UK, "£")]
    [InlineData(GameRules.Language.US, "$" )]
    public void SaveLoadRoundTripRestoresRulesStateOwnershipJailFinesAndDecks(GameRules.Language language, string currency)
    {
        GameRules rules = new(2, 2, 6);
        rules.SetLanguage(language);
        (Game game, Player first, Player second, _) = CreateGame(null, rules);
        game.Rules.Salary = 250;
        game.Rules.FreeParking = GameRules.Parking.Fines;
        game.CurrentPlayer = second;
        game.CurrentTurn = 7;
        game.RestoreConsecutiveDoubles(1);
        game.Fines = 35;

        PropertySquare property = game.Board.GetAllPropertySquares().First();
        property.Owner = first;
        property.Houses = 4;
        property.IsMortgage = true;
        game.TheJail.PlayerGoToJail(second);
        game.TheJail.IncrementTurnsInJail(second);
        string[] chanceOrder = game.FortuneCard.ChanceQueue.Select(card => card.Info).ToArray();
        string[] chestOrder = game.FortuneCard.CommunityChestQueue.Select(card => card.Info).ToArray();
        string filePath = Path.Combine(Path.GetTempPath(), $"monopoly-{Guid.NewGuid():N}.json");

        try
        {
            GameStateSerializer.Save(game, filePath);
            Game loaded = GameStateSerializer.Load(filePath, new TestDecisionProvider());

            Assert.Equal(language, loaded.Rules.GameLanguage);
            Assert.Equal(currency, loaded.Rules.CurrencySymbol);
            Assert.Equal(250, loaded.Rules.Salary);
            Assert.Equal(35, loaded.Fines);
            Assert.Equal(7, loaded.CurrentTurn);
            Assert.Equal(1, loaded.ConsecutiveDoubles);
            Assert.Equal(second.Id, loaded.CurrentPlayer.Id);

            Player loadedFirst = loaded.Players.Single(player => player.Id == first.Id);
            Player loadedSecond = loaded.Players.Single(player => player.Id == second.Id);
            PropertySquare loadedProperty = loaded.Board.GetAllPropertySquares().First(square => square.Position == property.Position);
            Assert.Same(loadedFirst, loadedProperty.Owner);
            Assert.Equal(4, loadedProperty.Houses);
            Assert.True(loadedProperty.IsMortgage);
            Assert.True(loaded.TheJail.IsPlayerInJail(loadedSecond));
            Assert.True(loaded.TheJail.TryGetJailInfo(loadedSecond, out Jail.JailStatus? jailStatus));
            Assert.NotNull(jailStatus);
            Assert.Equal(1, jailStatus.TurnsInJail);
            Assert.False(loaded.TheJail.TryGetJailInfo(second, out _));
            Assert.Equal(chanceOrder, loaded.FortuneCard.ChanceQueue.Select(card => card.Info));
            Assert.Equal(chestOrder, loaded.FortuneCard.CommunityChestQueue.Select(card => card.Info));
        }
        finally
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }

    [Fact]
    public void LoadRejectsMissingOrUnsupportedVersion()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"monopoly-invalid-{Guid.NewGuid():N}.json");

        try
        {
            File.WriteAllText(filePath, "{\"Version\":99}");

            InvalidDataException exception = Assert.Throws<InvalidDataException>(() => GameStateSerializer.Load(filePath));

            Assert.Contains("Expected version 1", exception.Message);
        }
        finally
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }

    [Fact]
    public void StartingAnotherConsoleGameDoesNotDuplicateUiEventSubscribers()
    {
        Game firstGame = CoreGameSetup.Setup(new GameRules(2, 2, 6));
        Game secondGame = CoreGameSetup.Setup(new GameRules(2, 2, 6));
        Monopoly.Console.ConsoleGame first = new(firstGame, null!, new(), null!, null!, null!);
        Monopoly.Console.ConsoleGame second = new(secondGame, null!, new(), null!, null!, null!);

        Monopoly.Console.Events.ConsoleEventHandler.SubscribeToEvents(first);
        try
        {
            Assert.Equal(1, GetSubscriberCount("UpdateGameBoard"));

            Monopoly.Console.Events.ConsoleEventHandler.SubscribeToEvents(second);
            Assert.Equal(1, GetSubscriberCount("UpdateGameBoard"));

            Monopoly.Core.Events.GameEvents.InvokeUpdateGameBoard(firstGame);
        }
        finally
        {
            Monopoly.Console.Events.ConsoleEventHandler.UnsubscribeFromEvents(second);
        }

        Assert.Equal(0, GetSubscriberCount("UpdateGameBoard"));
    }

    private static (Game Game, Player First, Player Second, TestDecisionProvider Decisions) CreateGame(
        TestDecisionProvider? decisions = null)
    {
        return CreateGame(decisions, new GameRules(2, 2, 6));
    }

    private static (Game Game, Player First, Player Second, TestDecisionProvider Decisions) CreateGame(
        TestDecisionProvider? decisions,
        GameRules rules)
    {
        decisions ??= new TestDecisionProvider();
        Game game = CoreGameSetup.Setup(rules, decisions);
        return (game, game.Players[0], game.Players[1], decisions);
    }

    private static int GetSubscriberCount(string eventName)
    {
        System.Reflection.FieldInfo? field = typeof(Monopoly.Core.Events.GameEvents)
            .GetField(eventName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        return ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0;
    }

    private sealed class FixedDie : IDie
    {
        private readonly Queue<int> values;
        private int result;

        public FixedDie(params int[] values)
        {
            this.values = new Queue<int>(values);
            result = values.Length > 0 ? values[0] : 1;
        }

        public int GetDieResult() => result;
        public int GetDieType() => 20;
        public void Roll() => result = values.Count > 0 ? values.Dequeue() : result;
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
