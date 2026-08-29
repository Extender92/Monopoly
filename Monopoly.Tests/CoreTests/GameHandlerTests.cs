using Monopoly.Core.Logs;
using Monopoly.Core.Models.Board;

namespace Monopoly.Tests.CoreTests;

public class GameHandlerTests
{
    [Fact]
    public void RoleDiceAndMovePlayer_ShouldAdjustPlayerPosition()
    {
        Game game = new GameTestBuilder(new GameRules(2, 1, 6)).WithRandomValues(3).Build();

        game.Handler.RoleDiceAndMovePlayer(game.Players[0]);

        Assert.Equal(3, game.Players[0].Position);
    }

    [Fact]
    public void CheckIfPlayerGoPastGo_ShouldAdjustPlayerPositionAndGrantSalary()
    {
        Game game = new GameTestBuilder().WithPlayer(0, money: 3000).Build();

        game.Handler.MovePlayerAndInvokeEvent(game.Players[0], 41);

        Assert.Equal(1, game.Players[0].Position);
        Assert.Equal(3200, game.Players[0].Money);
    }

    [Fact]
    public void CheckIfPlayerGoPastGoAfterRollingDice_ShouldAdjustPlayerPositionAndGrantSalary()
    {
        Game game = new GameTestBuilder(new GameRules(2, 1, 6))
            .WithPlayer(0, money: 3000, position: 37)
            .WithRandomValues(4)
            .Build();

        game.Handler.RoleDiceAndMovePlayer(game.Players[0]);

        Assert.Equal(1, game.Players[0].Position);
        Assert.Equal(3200, game.Players[0].Money);
    }

    [Fact]
    public void RoleDiceAndMovePlayer_ShouldAdjustPlayerPositionWithTwoDice()
    {
        Game game = new GameTestBuilder()
            .WithPlayer(0, position: 10)
            .WithRandomValues(2, 4)
            .Build();

        game.Handler.RoleDiceAndMovePlayer(game.Players[0]);

        Assert.Equal(16, game.Players[0].Position);
        Assert.Equal(new[] { 2, 4 }, game.LastDiceRoll!.Results);
    }

    [Fact]
    public void RollDice_ShouldRollDiceAndLogRollAndTotal()
    {
        Game game = new GameTestBuilder(new GameRules(1, 2, 6))
            .WithPlayer(0, name: "TestPlayer")
            .WithRandomValues(3, 4)
            .Build();
        Player player = game.Players[0];

        DiceRoll roll = game.Handler.RollDice(player);

        Assert.Contains(game.Logs.LogList, log => log.Info == "TestPlayer rolled: 3 4 Total: 7");
        Assert.Equal(7, roll.Sum);
    }

    [Fact]
    public void DiceRollMarksAPairWithEqualValuesAsDouble()
    {
        DiceRoll roll = CreateGameWithRoll(2, 3, 3);
        Assert.True(roll.IsDouble);
    }

    [Fact]
    public void DiceRollMarksAllEqualValuesAsDouble()
    {
        GameRules rules = new(1, 4, 6);
        Game game = new GameTestBuilder(rules).WithRandomValues(3, 3, 3, 3).Build();

        Assert.True(game.Handler.RollDice(game.CurrentPlayer).IsDouble);
    }

    [Fact]
    public void DiceRollDoesNotMarkDifferentValuesAsDouble()
    {
        DiceRoll roll = CreateGameWithRoll(2, 3, 4);
        Assert.False(roll.IsDouble);
    }

    [Fact]
    public void DiceRollDoesNotMarkOneValueAsDouble()
    {
        GameRules rules = new(1, 1, 6);
        Game game = new GameTestBuilder(rules).WithRandomValues(3).Build();

        Assert.False(game.Handler.RollDice(game.CurrentPlayer).IsDouble);
    }

    [Fact]
    public void GetMoneyFromBankruptPlayerAndBankruptPlayer_ShouldReturnRemainingMoneyAndHandleBankruptcyOnOnlyOnePlayer()
    {
        Game game = new GameTestBuilder()
            .WithPlayer(0, money: 200)
            .WithSquare(1, ownerId: 0)
            .WithSquare(5, ownerId: 0)
            .WithSquare(3, ownerId: 1)
            .Build();
        Player player = game.Players[0];
        int expectedAssets = 200 + game.Board.GetSquareAtPosition(1).MortgageValue + game.Board.GetSquareAtPosition(5).MortgageValue;

        int remainingMoney = game.Handler.GetMoneyFromBankruptPlayerAndBankruptPlayer(player);

        Assert.Equal(expectedAssets, remainingMoney);
        Assert.Equal(0, player.Money);
        Assert.True(player.IsBankrupt);
        Assert.Same(game.Players[0], game.Board.GetSquareAtPosition(3).Owner);
    }

    [Fact]
    public void HandlePlayerBankruptcy_ShouldClearOwnershipAndSetPlayerBankruptOnOnlyOnePlayer()
    {
        Game game = new GameTestBuilder()
            .WithSquare(1, ownerId: 0)
            .WithSquare(5, ownerId: 0)
            .WithSquare(3, ownerId: 1)
            .WithSquare(12, ownerId: 1)
            .Build();
        Player debtor = game.Players[0];
        Player other = game.Players[1];

        game.Handler.HandlePlayerBankruptcy(debtor);

        Assert.Null(game.Board.GetSquareAtPosition(1).Owner);
        Assert.Null(game.Board.GetSquareAtPosition(5).Owner);
        Assert.Same(other, game.Board.GetSquareAtPosition(3).Owner);
        Assert.Same(other, game.Board.GetSquareAtPosition(12).Owner);
        Assert.True(debtor.IsBankrupt);
    }

    [Fact]
    public void ClearOwnershipForPlayer_ShouldClearOwnershipAndHousesOnOnlyOnePlayer()
    {
        Game game = new GameTestBuilder()
            .WithSquare(1, ownerId: 0, houses: 3)
            .WithSquare(5, ownerId: 0)
            .WithSquare(3, ownerId: 1, houses: 2)
            .Build();
        Player first = game.Players[0];
        Player second = game.Players[1];

        game.Handler.ClearOwnershipForPlayer(first);

        Assert.Null(game.Board.GetSquareAtPosition(1).Owner);
        Assert.Equal(0, ((PropertySquare)game.Board.GetSquareAtPosition(1)).Houses);
        Assert.Null(game.Board.GetSquareAtPosition(5).Owner);
        Assert.Same(second, game.Board.GetSquareAtPosition(3).Owner);
        Assert.Equal(2, ((PropertySquare)game.Board.GetSquareAtPosition(3)).Houses);
    }

    [Fact]
    public void IsPlayerBankrupt_ShouldReturnTrueWhenPlayerCannotAfford()
    {
        Game game = new GameTestBuilder().WithPlayer(0, money: 100).Build();
        Assert.True(game.Handler.IsPlayerBankrupt(game.Players[0], 150));
    }

    [Fact]
    public void IsPlayerBankrupt_ShouldReturnFalseWhenPlayerCanAfford()
    {
        Game game = new GameTestBuilder().WithPlayer(0, money: 200).Build();
        Assert.False(game.Handler.IsPlayerBankrupt(game.Players[0], 150));
    }

    [Fact]
    public void CanAffordWithAssets_ShouldReturnTrueWhenPlayerCanAfford()
    {
        Game game = new GameTestBuilder().WithPlayer(0, money: 200).Build();
        Assert.True(game.Handler.CanAffordWithAssets(game.Players[0], 150));
    }

    [Fact]
    public void CanAffordWithAssets_ShouldReturnFalseWhenPlayerCannotAfford()
    {
        Game game = new GameTestBuilder().WithPlayer(0, money: 100).Build();
        Assert.False(game.Handler.CanAffordWithAssets(game.Players[0], 150));
    }

    [Fact]
    public void CalculatePlayerAssets_ShouldCalculateCorrectTotalAssets()
    {
        Game game = new GameTestBuilder().WithPlayer(0, money: 100).WithSquare(1, ownerId: 0).Build();
        int expected = 100 + game.Board.GetSquareAtPosition(1).MortgageValue;

        Assert.Equal(expected, game.Handler.CalculatePlayerAssets(game.Players[0]));
    }

    [Fact]
    public void CalculateMortgageValue_ShouldReturnCorrectMortgageValue()
    {
        Game game = new GameTestBuilder().Build();
        Square property = game.Board.GetSquareAtPosition(1);

        Assert.Equal(property.MortgageValue, game.Handler.CalculateMortgageValue(property));
    }

    [Fact]
    public void CalculateHouseAndHotelValue_ShouldReturnCorrectValueForHousesAndHotels()
    {
        Game game = new GameTestBuilder().WithSquare(1, ownerId: 0, houses: 3).Build();
        PropertySquare property = (PropertySquare)game.Board.GetSquareAtPosition(1);

        Assert.Equal(3 * (property.BuildHouseCost / 2), game.Handler.CalculateHouseAndHotelValue(property));
    }

    private static DiceRoll CreateGameWithRoll(int diceCount, params int[] results)
    {
        Game game = new GameTestBuilder(new GameRules(1, diceCount, 6))
            .WithRandomValues(results)
            .Build();
        return game.Handler.RollDice(game.CurrentPlayer);
    }
}
