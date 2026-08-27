using Monopoly.Core;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Persistence;

namespace Monopoly.Tests.CoreTests;

public sealed class GameStateV1MapperTests
{
    [Theory]
    [InlineData(GameRules.Language.UK, "£")]
    [InlineData(GameRules.Language.US, "$")]
    public void RoundTripRestoresRulesStateOwnershipJailFinesAndDecks(
        GameRules.Language language,
        string currency)
    {
        GameRules rules = new(2, 2, 6);
        rules.SetLanguage(language);
        TestDecisionProvider decisions = new();
        Game game = CoreGameSetup.Setup(rules, decisions);
        Player first = game.Players[0];
        Player second = game.Players[1];
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

        GameStateV1 state = GameStateV1Mapper.ToState(game);
        Game loaded = GameStateV1Mapper.FromState(state, decisions);

        Assert.Equal(GameStateV1Mapper.CurrentVersion, state.Version);
        Assert.Equal(language, loaded.Rules.GameLanguage);
        Assert.Equal(currency, loaded.Rules.CurrencySymbol);
        Assert.Equal(250, loaded.Rules.Salary);
        Assert.Equal(GameRules.Parking.Fines, loaded.Rules.FreeParking);
        Assert.Equal(35, loaded.Fines);
        Assert.Equal(7, loaded.CurrentTurn);
        Assert.Equal(1, loaded.ConsecutiveDoubles);
        Assert.Equal(second.Id, loaded.CurrentPlayer.Id);
        Assert.Same(decisions, loaded.Decisions);

        Player loadedFirst = loaded.Players.Single(player => player.Id == first.Id);
        Player loadedSecond = loaded.Players.Single(player => player.Id == second.Id);
        PropertySquare loadedProperty = loaded.Board.GetAllPropertySquares()
            .First(square => square.Position == property.Position);
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

    [Fact]
    public void FromStateRejectsInvalidDomainStateWithoutPhysicalStorage()
    {
        GameStateV1 state = GameStateV1Mapper.ToState(CoreGameSetup.Setup(new GameRules(2, 2, 6)));
        state.Players.Clear();

        GameStateValidationException exception = Assert.Throws<GameStateValidationException>(
            () => GameStateV1Mapper.FromState(state));

        Assert.Contains("at least one player", exception.Message);
    }

    [Fact]
    public void FromStateDerivesWinnerWhenOnlyOneSavedPlayerIsActive()
    {
        Game game = CoreGameSetup.Setup(new GameRules(2, 2, 6));
        game.Players[1].IsBankrupt = true;

        Game loaded = GameStateV1Mapper.FromState(GameStateV1Mapper.ToState(game));

        Assert.Same(loaded.Players[0], loaded.Winner);
        Assert.True(loaded.IsGameOver);
    }

    private sealed class TestDecisionProvider : IPlayerDecisionProvider
    {
        public bool ConfirmPurchase(Player player, Square square) => false;

        public bool ConfirmJailBuyout(Player player) => false;

        public bool ResolveInsufficientFunds(Game game, Player player, int amount) => false;
    }
}
