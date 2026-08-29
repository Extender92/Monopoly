using System.Reflection;
using System.Text.Json;
using Monopoly.Core.Interface;
using Monopoly.Core.Logs;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models.FortuneCard;
using Monopoly.Core.Persistence;
using static Monopoly.Core.Jail;

namespace Monopoly.Tests.CoreTests;

public sealed class GameStateEncapsulationTests
{
    [Fact]
    public void AuthoritativeDomainPropertiesHaveNoPublicSetters()
    {
        AssertNoPublicSetter<Game>(
            nameof(Game.Players),
            nameof(Game.CurrentPlayer),
            nameof(Game.LastDiceRoll),
            nameof(Game.Rules),
            nameof(Game.Presentation),
            nameof(Game.Decks),
            nameof(Game.Fines),
            nameof(Game.CurrentTurn),
            nameof(Game.ConsecutiveDoubles),
            nameof(Game.Winner));
        AssertNoPublicSetter<Player>(
            nameof(Player.Id),
            nameof(Player.Name),
            nameof(Player.Money),
            nameof(Player.Position),
            nameof(Player.NumberOfGetOutOFJailCards),
            nameof(Player.IsBankrupt));
        AssertNoPublicSetter<Square>(
            nameof(Square.Position),
            nameof(Square.PresentationToken),
            nameof(Square.Owner),
            nameof(Square.Price),
            nameof(Square.MortgageValue),
            nameof(Square.IsMortgage));
        AssertNoPublicSetter<PropertySquare>(nameof(PropertySquare.Houses));
        AssertNoPublicSetter<JailStatus>(nameof(JailStatus.TurnsInJail));

        foreach (PropertyInfo property in typeof(GameRules).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            Assert.False(property.SetMethod?.IsPublic ?? false, $"{property.Name} has a public setter.");
    }

    [Fact]
    public void RulePrimitivesAreNotPublicMutationEntryPoints()
    {
        BindingFlags publicInstance = BindingFlags.Instance | BindingFlags.Public;

        Assert.Null(typeof(Game).GetMethod("NextPlayer", publicInstance));
        Assert.Null(typeof(Game).GetMethod("RemovePlayer", publicInstance));
        Assert.Null(typeof(Game).GetMethod("MovePlayerBySteps", publicInstance));
        Assert.Null(typeof(GameBoard).GetMethod("HandlePlayerLandingOnSquare", publicInstance));
        Assert.Null(typeof(Square).GetMethod("LandOn", publicInstance));
        Assert.Null(typeof(Jail).GetMethod("PlayerGoToJail", publicInstance));
        Assert.Null(typeof(Jail).GetMethod("ReleasePlayerFromJail", publicInstance));
        Assert.False(typeof(FortuneCardHandler).IsPublic);
        Assert.Null(typeof(Game).GetProperty("FortuneCard", publicInstance));
        Assert.Null(typeof(UKChanceCard).GetMethod("ExecuteEffect", publicInstance));
        Assert.Null(typeof(USChanceCard).GetMethod("ExecuteEffect", publicInstance));
        Assert.Null(typeof(UKCommunityChestCard).GetMethod("ExecuteEffect", publicInstance));
        Assert.Null(typeof(USCommunityChestCard).GetMethod("ExecuteEffect", publicInstance));
        Assert.False(typeof(GameHandler).IsPublic);
        Assert.False(typeof(Transaction).IsPublic);
    }

    [Fact]
    public void PublicCollectionsAndBoardQueriesAreActuallyReadOnly()
    {
        Game game = new GameTestBuilder()
            .WithSquare(1, ownerId: 0)
            .WithPlayerInJail(1)
            .Build();
        game.LogWriter.CreateLog("immutable log view");
        game.Handler.RollDice(game.CurrentPlayer);

        Assert.Throws<NotSupportedException>(() => ((IList<Player>)game.Players).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<int>)game.LastDiceRoll!.Results).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<Square>)game.Board.Squares).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<PropertySquare>)game.Board.GetAllPropertySquares()).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<Log>)game.Logs.LogList).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<Player, JailStatus>)game.TheJail.PlayersInJail).Clear());
        DeckCollection decks = game.Decks;
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<DeckId, DeckView>)decks.ById).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IList<ICardView>)decks.Entries[0].Cards).Clear());

        Assert.Equal(2, game.Players.Count);
        Assert.Equal(40, game.Board.Squares.Count);
        Assert.Single(game.TheJail.PlayersInJail);
        Assert.Equal(2, game.Logs.LogList.Count);
    }

    [Fact]
    public void GameConstructorCopiesSuppliedCollections()
    {
        Player first = new("First", 0);
        Player second = new("Second", 1);
        List<Player> players = [first, second];
        Game game = new(players, first, new GameRules(2, 2, 6), randomSource: new MinimumMatchRandomSource());

        players.Clear();

        Assert.Equal(new[] { first, second }, game.Players);
        Assert.Null(game.LastDiceRoll);
    }

    [Fact]
    public void PublicGameConstruction_DoesNotExposeMutableDiceInjection()
    {
        Assert.Null(typeof(Game).Assembly.GetType("Monopoly.Core.Models.IDie"));
        Assert.Null(typeof(Game).Assembly.GetType("Monopoly.Core.Models.Die"));
        Assert.Equal(typeof(DiceRoll), typeof(Game).GetProperty(nameof(Game.LastDiceRoll))!.PropertyType);
        Assert.DoesNotContain(
            typeof(Game).GetProperties(BindingFlags.Instance | BindingFlags.Public),
            property => typeof(IMatchRandomSource).IsAssignableFrom(property.PropertyType));
    }

    [Fact]
    public void GameRulesConstructorProvidesValidatedImmutableDefaultsAndOverrides()
    {
        GameRules defaults = new(2, 2, 6);

        Assert.Equal(GameRules.Language.UK, defaults.GameLanguage);
        Assert.Equal(Monopoly.Core.Presentation.PresentationTokens.PrimaryResource, defaults.PrimaryResourcePresentationToken);
        Assert.Equal(200, defaults.Salary);
        Assert.False(defaults.DoubleOnGo);
        Assert.Equal(GameRules.Parking.Classic, defaults.FreeParking);
        Assert.Equal(10, defaults.MortgageInterestRate);
        Assert.Equal(50, defaults.JailFine);
        Assert.Equal(3, defaults.MaxTurnsInJail);

        GameRules customized = new(
            3,
            1,
            8,
            GameRules.Language.US,
            salary: 250,
            doubleOnGo: true,
            freeParking: GameRules.Parking.Fines,
            mortgageInterestRate: 12,
            jailFine: 75,
            maxTurnsInJail: 4);
        Assert.Equal(Monopoly.Core.Presentation.PresentationTokens.PrimaryResource, customized.PrimaryResourcePresentationToken);
        Assert.Equal(250, customized.Salary);
        Assert.True(customized.DoubleOnGo);
        Assert.Equal(GameRules.Parking.Fines, customized.FreeParking);
        Assert.Equal(12, customized.MortgageInterestRate);
        Assert.Equal(75, customized.JailFine);
        Assert.Equal(4, customized.MaxTurnsInJail);

        Assert.Throws<ArgumentOutOfRangeException>(() => new GameRules(0, 2, 6));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameRules(2, 0, 6));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameRules(2, 2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameRules(2, 2, 6, salary: -1));
    }

    [Fact]
    public void GameConstructorRejectsInvalidAggregateReferencesBeforeExposure()
    {
        Player first = new("First", 0);
        Player duplicateId = new("Duplicate", 0);
        Player foreignCurrentPlayer = new("Foreign", 2);
        GameRules rules = new(2, 2, 6);

        Assert.Throws<ArgumentException>(() =>
            new Game([first, duplicateId], first, rules, randomSource: new MinimumMatchRandomSource()));
        Assert.Throws<ArgumentException>(() =>
            new Game([first, new Player("Second", 1)], foreignCurrentPlayer, rules, randomSource: new MinimumMatchRandomSource()));
    }

    [Fact]
    public void VersionOneDtosAreDetachedFromTheLiveGameInBothDirections()
    {
        Game source = new GameTestBuilder().WithSquare(1, ownerId: 0).Build();
        int sourceMoney = source.Players[0].Money;
        GameStateV1 state = GameStateV1Mapper.ToState(source);

        state.Players[0].Money = 1;
        state.Squares.Clear();
        state.ChanceDeck.Reverse();

        Assert.Equal(sourceMoney, source.Players[0].Money);
        Assert.Same(source.Players[0], source.Board.GetSquareAtPosition(1).Owner);

        GameStateV1 candidate = GameStateV1Mapper.ToState(source);
        Game restored = GameStateV1Mapper.FromState(candidate);
        int restoredMoney = restored.Players[0].Money;
        candidate.Players[0].Money = 2;
        candidate.Players.Clear();
        candidate.Squares.Clear();
        candidate.Jail.Clear();
        candidate.ChanceDeck.Clear();

        Assert.Equal(restoredMoney, restored.Players[0].Money);
        Assert.Equal(2, restored.Players.Count);
        Assert.Same(restored.Players[0], restored.Board.GetSquareAtPosition(1).Owner);
        Assert.NotEmpty(restored.Decks.Resolve(LegacyStructureIds.PrimaryDeck).Cards);
    }

    [Fact]
    public void ValidAssetCommandsMutateOnlyThroughGameBoundary()
    {
        Game buyGame = new GameTestBuilder()
            .WithPlayer(0, money: 100)
            .WithSquare(1, ownerId: 0)
            .WithSquare(3, ownerId: 0)
            .Build();
        Player buyer = buyGame.Players[0];
        PropertySquare buyProperty = (PropertySquare)buyGame.Board.GetSquareAtPosition(1);

        Assert.True(buyGame.TryBuyHouse(buyer, buyProperty));
        Assert.Equal(1, buyProperty.Houses);
        Assert.Equal(100 - buyProperty.BuildHouseCost, buyer.Money);

        Assert.True(buyGame.TrySellHouse(buyer, buyProperty));
        Assert.Equal(0, buyProperty.Houses);
        Assert.Equal(100 - buyProperty.BuildHouseCost + buyProperty.BuildHouseCost / 2, buyer.Money);

        Game mortgageGame = new GameTestBuilder()
            .WithPlayer(0, money: 200)
            .WithSquare(5, ownerId: 0)
            .Build();
        Player mortgagor = mortgageGame.Players[0];
        Square mortgageSquare = mortgageGame.Board.GetSquareAtPosition(5);

        Assert.True(mortgageGame.TryMortgageProperty(mortgagor, mortgageSquare));
        Assert.True(mortgageSquare.IsMortgage);
        Assert.Equal(200 + mortgageSquare.MortgageValue, mortgagor.Money);

        int repayment = (int)(mortgageSquare.MortgageValue * (1 + mortgageGame.Rules.MortgageInterestRate / 100.0));
        Assert.True(mortgageGame.TryRepayMortgage(mortgagor, mortgageSquare));
        Assert.False(mortgageSquare.IsMortgage);
        Assert.Equal(200 + mortgageSquare.MortgageValue - repayment, mortgagor.Money);
    }

    [Fact]
    public void RejectedAssetCommandsLeaveCompleteSnapshotUnchanged()
    {
        Game wrongOwner = new GameTestBuilder().WithSquare(1, ownerId: 1).Build();
        AssertRejected(wrongOwner, () =>
            wrongOwner.TryBuyHouse(wrongOwner.Players[0], (PropertySquare)wrongOwner.Board.GetSquareAtPosition(1)));

        Game insufficientHouseFunds = new GameTestBuilder()
            .WithPlayer(0, money: 0)
            .WithSquare(1, ownerId: 0)
            .WithSquare(3, ownerId: 0)
            .Build();
        AssertRejected(insufficientHouseFunds, () => insufficientHouseFunds.TryBuyHouse(
            insufficientHouseFunds.Players[0],
            (PropertySquare)insufficientHouseFunds.Board.GetSquareAtPosition(1)));

        Game mortgagedProperty = new GameTestBuilder()
            .WithSquare(1, ownerId: 0, isMortgage: true)
            .WithSquare(3, ownerId: 0)
            .Build();
        AssertRejected(mortgagedProperty, () => mortgagedProperty.TryBuyHouse(
            mortgagedProperty.Players[0],
            (PropertySquare)mortgagedProperty.Board.GetSquareAtPosition(1)));

        Game noHouse = new GameTestBuilder().WithSquare(1, ownerId: 0).Build();
        AssertRejected(noHouse, () => noHouse.TrySellHouse(
            noHouse.Players[0],
            (PropertySquare)noHouse.Board.GetSquareAtPosition(1)));

        Game wrongHouseOwner = new GameTestBuilder().WithSquare(1, ownerId: 1, houses: 1).Build();
        AssertRejected(wrongHouseOwner, () => wrongHouseOwner.TrySellHouse(
            wrongHouseOwner.Players[0],
            (PropertySquare)wrongHouseOwner.Board.GetSquareAtPosition(1)));

        Game builtProperty = new GameTestBuilder()
            .WithSquare(1, ownerId: 0, houses: 1)
            .Build();
        AssertRejected(builtProperty, () => builtProperty.TryMortgageProperty(
            builtProperty.Players[0],
            builtProperty.Board.GetSquareAtPosition(1)));

        Game alreadyMortgaged = new GameTestBuilder().WithSquare(5, ownerId: 0, isMortgage: true).Build();
        AssertRejected(alreadyMortgaged, () => alreadyMortgaged.TryMortgageProperty(
            alreadyMortgaged.Players[0],
            alreadyMortgaged.Board.GetSquareAtPosition(5)));

        Game wrongMortgageOwner = new GameTestBuilder().WithSquare(5, ownerId: 1).Build();
        AssertRejected(wrongMortgageOwner, () => wrongMortgageOwner.TryMortgageProperty(
            wrongMortgageOwner.Players[0],
            wrongMortgageOwner.Board.GetSquareAtPosition(5)));

        Game notMortgaged = new GameTestBuilder().WithSquare(5, ownerId: 0).Build();
        AssertRejected(notMortgaged, () => notMortgaged.TryRepayMortgage(
            notMortgaged.Players[0],
            notMortgaged.Board.GetSquareAtPosition(5)));

        Game insufficientRepaymentFunds = new GameTestBuilder()
            .WithPlayer(0, money: 0)
            .WithSquare(5, ownerId: 0, isMortgage: true)
            .Build();
        AssertRejected(insufficientRepaymentFunds, () => insufficientRepaymentFunds.TryRepayMortgage(
            insufficientRepaymentFunds.Players[0],
            insufficientRepaymentFunds.Board.GetSquareAtPosition(5)));

        Game wrongRepaymentOwner = new GameTestBuilder().WithSquare(5, ownerId: 1, isMortgage: true).Build();
        AssertRejected(wrongRepaymentOwner, () => wrongRepaymentOwner.TryRepayMortgage(
            wrongRepaymentOwner.Players[0],
            wrongRepaymentOwner.Board.GetSquareAtPosition(5)));

        Game mortgageOverflow = new GameTestBuilder()
            .WithPlayer(0, money: int.MaxValue)
            .WithSquare(5, ownerId: 0)
            .Build();
        AssertRejected(mortgageOverflow, () => mortgageOverflow.TryMortgageProperty(
            mortgageOverflow.Players[0],
            mortgageOverflow.Board.GetSquareAtPosition(5)));

        Game saleOverflow = new GameTestBuilder()
            .WithPlayer(0, money: int.MaxValue)
            .WithSquare(1, ownerId: 0, houses: 1)
            .Build();
        AssertRejected(saleOverflow, () => saleOverflow.TrySellHouse(
            saleOverflow.Players[0],
            (PropertySquare)saleOverflow.Board.GetSquareAtPosition(1)));
    }

    [Fact]
    public void AssetCommandsRejectNullAndForeignAggregateObjects()
    {
        Game game = new GameTestBuilder()
            .WithSquare(1, ownerId: 0)
            .WithSquare(3, ownerId: 0)
            .WithSquare(5, ownerId: 0)
            .Build();
        Game foreignGame = new GameTestBuilder()
            .WithSquare(1, ownerId: 0)
            .WithSquare(5, ownerId: 0)
            .Build();
        Player player = game.Players[0];
        PropertySquare property = (PropertySquare)game.Board.GetSquareAtPosition(1);
        Square square = game.Board.GetSquareAtPosition(5);
        string snapshot = CaptureSnapshot(game);

        Assert.Throws<ArgumentNullException>(() => game.TryBuyHouse(null!, property));
        Assert.Throws<ArgumentNullException>(() => game.TryBuyHouse(player, null!));
        Assert.Throws<ArgumentNullException>(() => game.TryMortgageProperty(null!, square));
        Assert.Throws<ArgumentNullException>(() => game.TryMortgageProperty(player, null!));
        Assert.Throws<ArgumentException>(() => game.TryBuyHouse(foreignGame.Players[0], property));
        Assert.Throws<ArgumentException>(() => game.TrySellHouse(player,
            (PropertySquare)foreignGame.Board.GetSquareAtPosition(1)));
        Assert.Throws<ArgumentException>(() => game.TryMortgageProperty(player,
            foreignGame.Board.GetSquareAtPosition(5)));
        Assert.Throws<ArgumentException>(() => game.TryRepayMortgage(foreignGame.Players[0], square));
        Assert.Equal(snapshot, CaptureSnapshot(game));
    }

    [Fact]
    public void DecisionProviderCanOnlyBeReattachedThroughValidatedMethod()
    {
        Game game = new GameTestBuilder().Build();
        TestDecisionProvider provider = new();

        game.SetDecisionProvider(provider);

        Assert.Same(provider, game.Decisions);
        Assert.Throws<ArgumentNullException>(() => game.SetDecisionProvider(null!));
        Assert.Same(provider, game.Decisions);
    }

    private static void AssertNoPublicSetter<T>(params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            PropertyInfo property = typeof(T).GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
            Assert.NotNull(property);
            Assert.False(property.SetMethod?.IsPublic ?? false, $"{typeof(T).Name}.{propertyName} has a public setter.");
        }
    }

    private static void AssertRejected(Game game, Func<bool> command)
    {
        string before = CaptureSnapshot(game);

        Assert.False(command());

        Assert.Equal(before, CaptureSnapshot(game));
    }

    private static string CaptureSnapshot(Game game)
    {
        string state = JsonSerializer.Serialize(GameStateV1Mapper.ToState(game));
        string logs = JsonSerializer.Serialize(game.Logs.LogList.Select(log => new { log.Id, log.Info }));
        return state + logs;
    }

    private sealed class TestDecisionProvider : IPlayerDecisionProvider
    {
        public bool ResolveInsufficientFunds(Game game, Player player, int amount) => false;
    }
}
