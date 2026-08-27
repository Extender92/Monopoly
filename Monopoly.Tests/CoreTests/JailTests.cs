using Monopoly.Core.Events;
using Monopoly.Core.Models;
using static Monopoly.Core.Events.PlayerEventArgs;
using static Monopoly.Core.Jail;

namespace Monopoly.Tests.CoreTests;

public class JailTests
{
    [Fact]
    public void TryGetJailInfo_PlayerInJail_ReturnsStoredStatus()
    {
        Game game = CreateGameInJail(turnsInJail: 2);
        Player player = PlayerZero(game);

        bool found = game.TheJail.TryGetJailInfo(player, out JailStatus? jailStatus);

        Assert.True(found);
        Assert.NotNull(jailStatus);
        Assert.Same(game.TheJail.PlayersInJail[player], jailStatus);
        Assert.Equal(2, jailStatus.TurnsInJail);
    }

    [Fact]
    public void TryGetJailInfo_PlayerNotInJail_ReturnsFalseAndNull()
    {
        Game game = new GameTestBuilder().Build();

        bool found = game.TheJail.TryGetJailInfo(PlayerZero(game), out JailStatus? jailStatus);

        Assert.False(found);
        Assert.Null(jailStatus);
    }

    [Fact]
    public void TryGetJailInfo_NullPlayer_ThrowsArgumentNullException()
    {
        Game game = new GameTestBuilder().Build();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => game.TheJail.TryGetJailInfo(null!, out _));

        Assert.Equal("player", exception.ParamName);
    }

    [Fact]
    public void GetJailInfo_PlayerNotInJail_ThrowsControlledException()
    {
        Game game = new GameTestBuilder().Build();
        Player player = PlayerZero(game);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => game.TheJail.GetJailInfo(player));

        Assert.Equal($"Player '{player.Name}' is not in jail.", exception.Message);
    }

    [Fact]
    public void CheckIfPlayerGoToJail_ShouldSetPlayerInJail()
    {
        Game game = new GameTestBuilder().WithPlayer(0, position: 0).Build();
        Player player = PlayerZero(game);

        game.TheJail.PlayerGoToJail(player);

        Assert.True(game.TheJail.IsPlayerInJail(player));
        Assert.Equal(game.TheJail.JailPosition, player.Position);
    }

    [Fact]
    public void CheckIfPlayerGoToJail_ShouldThrowArgumentNullException()
    {
        Game game = new GameTestBuilder().Build();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => game.TheJail.PlayerGoToJail(null!));

        Assert.Equal("player", exception.ParamName);
        Assert.StartsWith("Player cannot be null.", exception.Message);
    }

    [Fact]
    public void CheckIfPlayerGoToJail_ShouldSetCorrectLog()
    {
        Game game = new GameTestBuilder().Build();
        Player player = PlayerZero(game);

        game.TheJail.PlayerGoToJail(player, "testReason");

        Assert.Contains(game.Logs.LogList, log => log.Info == $"{player.Name} has been sent to jail testReason.");
    }

    [Fact]
    public void IsPlayerInJail_ShouldThrowArgumentNullException()
    {
        Game game = new GameTestBuilder().Build();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => game.TheJail.IsPlayerInJail(null!));

        Assert.Equal("player", exception.ParamName);
    }

    [Fact]
    public void IsPlayerInJail_ShouldReturnFalse()
    {
        Game game = new GameTestBuilder().Build();

        Assert.False(game.TheJail.IsPlayerInJail(PlayerZero(game)));
    }

    [Fact]
    public void IsPlayerInJail_ShouldReturnTrue()
    {
        Game game = CreateGameInJail();

        Assert.True(game.TheJail.IsPlayerInJail(PlayerZero(game)));
    }

    [Fact]
    public void TryPlayerBuyOut_ShouldReturnTrueWithGetOutOfJailCard()
    {
        Game game = CreateGameInJail(jailCards: 1);

        Assert.True(InvokeBuyOutDecision(game));
    }

    [Fact]
    public void TryPlayerBuyOut_PlayerNotInJail_ReturnsFalse()
    {
        Game game = new GameTestBuilder().Build();

        Assert.False(game.TheJail.TryPlayerBuyOut(PlayerZero(game)));
    }

    [Fact]
    public void TryPlayerBuyOut_ShouldReturnTrueWithMoney()
    {
        Game game = CreateGameInJail(money: 50);

        Assert.True(InvokeBuyOutDecision(game));
    }

    [Fact]
    public void TryPlayerBuyOut_ShouldReturnTrueWithAssets()
    {
        Game game = CreateGameInJail(money: 0, ownsProperty: true);

        Assert.True(InvokeBuyOutDecision(game));
    }

    [Fact]
    public void TryPlayerBuyOut_ShouldReturnFalse()
    {
        Game game = CreateGameInJail(money: 0);

        Assert.False(InvokeBuyOutDecision(game));
    }

    [Fact]
    public void TryIncrementTurnsInJail_ShouldIncrementTurnsInJailForPlayer()
    {
        Game game = CreateGameInJail(turnsInJail: 1);
        Player player = PlayerZero(game);

        game.TheJail.IncrementTurnsInJail(player);

        Assert.Equal(2, game.TheJail.GetJailInfo(player).TurnsInJail);
    }

    [Fact]
    public void PlayerReachedMaxTurnsInJail_WhenTurnsEqualMax_ReturnsTrue()
    {
        Game game = CreateGameInJail(turnsInJail: 3);

        Assert.True(game.TheJail.PlayerReachedMaxTurnsInJail(PlayerZero(game)));
    }

    [Fact]
    public void PlayerReachedMaxTurnsInJail_PlayerNotInJail_ReturnsFalse()
    {
        Game game = new GameTestBuilder().Build();

        Assert.False(game.TheJail.PlayerReachedMaxTurnsInJail(PlayerZero(game)));
    }

    [Fact]
    public void PlayerReachedMaxTurnsInJail_WhenTurnsLessThanMax_ReturnsFalse()
    {
        Game game = CreateGameInJail(turnsInJail: 2);

        Assert.False(game.TheJail.PlayerReachedMaxTurnsInJail(PlayerZero(game)));
    }

    [Fact]
    public void PlayerReachedMaxTurnsInJail_WhenTurnsGreaterThanMax_ReturnsTrue()
    {
        Game game = CreateGameInJail(turnsInJail: 3);
        Player player = PlayerZero(game);
        game.TheJail.IncrementTurnsInJail(player);

        Assert.True(game.TheJail.PlayerReachedMaxTurnsInJail(player));
    }

    [Fact]
    public void HandleMaxTurnsInJail_PlayerHasNoMoney_PlayerShouldBecomeBankrupt()
    {
        Game game = CreateGameInJail(money: 0, turnsInJail: 3);
        Player player = PlayerZero(game);

        game.TheJail.HandleMaxTurnsInJail(player);

        Assert.True(player.IsBankrupt);
        Assert.DoesNotContain(player, game.Players);
        Assert.Contains(game.Logs.LogList, log =>
            log.Info == $"{player.Name} has been bankrupt, {player.Name} Could not afford to pay Jail Fine of 50£.");
    }

    [Fact]
    public void HandleMaxTurnsInJail_PlayerHasGetOutOfJailCard_PlayerShouldUseGetOutOfJailCard()
    {
        Game game = CreateGameInJail(money: 200, jailCards: 2, turnsInJail: 3);
        Player player = PlayerZero(game);

        game.TheJail.HandleMaxTurnsInJail(player);

        Assert.Equal(200, player.Money);
        Assert.Equal(1, player.NumberOfGetOutOFJailCards);
        Assert.Contains(game.Logs.LogList, log =>
            log.Info == $"JailTurn 3: {player.Name} has been released from jail, {player.Name} used a Get Out of Jail For Free card and have 1 left.");
    }

    [Fact]
    public void HandleMaxTurnsInJail_PlayerHasMoney_PlayerMoneyShouldBeDeducted()
    {
        Game game = CreateGameInJail(money: 200, turnsInJail: 3);
        Player player = PlayerZero(game);

        game.TheJail.HandleMaxTurnsInJail(player);

        Assert.Equal(150, player.Money);
        Assert.Contains(game.Logs.LogList, log =>
            log.Info == $"JailTurn 3: {player.Name} has been released from jail, {player.Name} paid the fine to get out of jail.");
        Assert.Contains(game.Logs.LogList, log => log.Info == $"{player.Name} payed fines of 50£.");
    }

    [Fact]
    public void BuyOutPlayerFromJail_UseGetOutOfJailFreeCard()
    {
        Game game = CreateGameInJail(money: 200, jailCards: 1);
        Player player = PlayerZero(game);

        string reason = game.TheJail.BuyOutPlayerFromJail(player);

        Assert.Equal($", {player.Name} used a Get Out of Jail For Free card and have 0 left", reason);
        Assert.Equal(0, player.NumberOfGetOutOFJailCards);
    }

    [Fact]
    public void BuyOutPlayerFromJail_PayFine()
    {
        Game game = CreateGameInJail(money: 200);
        Player player = PlayerZero(game);

        string reason = game.TheJail.BuyOutPlayerFromJail(player);

        Assert.Equal($", {player.Name} paid the fine to get out of jail", reason);
        Assert.Equal(150, player.Money);
        Assert.Contains(game.Logs.LogList, log => log.Info == $"{player.Name} payed fines of 50£.");
    }

    [Fact]
    public void BuyOutPlayerFromJail_InsufficientFunds_ShouldRaiseEventAndPayFine()
    {
        Game game = CreateGameInJail(money: 0);
        Player player = PlayerZero(game);
        bool eventRaised = false;
        EventHandler<PlayerEventArgs> handler = (_, args) =>
        {
            if (!ReferenceEquals(args.Player, player)) return;
            eventRaised = true;
            game.Transactions.GetMoneyFromBank(player, game.Rules.JailFine);
        };
        GameEvents.PlayerInsufficientFundsEvent += handler;

        try
        {
            string reason = game.TheJail.BuyOutPlayerFromJail(player);

            Assert.Equal($", {player.Name} paid the fine to get out of jail", reason);
        }
        finally
        {
            GameEvents.PlayerInsufficientFundsEvent -= handler;
        }

        Assert.True(eventRaised);
        Assert.Equal(0, player.Money);
        Assert.Contains(game.Logs.LogList, log => log.Info == $"{player.Name} payed fines of 50£.");
    }

    [Fact]
    public void ReleasePlayerFromJail_PlayerInJail_NoReason()
    {
        Game game = CreateGameInJail();
        Player player = PlayerZero(game);

        game.TheJail.ReleasePlayerFromJail(player);

        Assert.Contains(game.Logs.LogList, log => log.Info == $"JailTurn 0: {player.Name} has been released from jail.");
        Assert.DoesNotContain(player, game.TheJail.PlayersInJail.Keys);
        Assert.False(game.TheJail.TryGetJailInfo(player, out JailStatus? jailStatus));
        Assert.Null(jailStatus);
    }

    [Fact]
    public void ReleasePlayerFromJail_PlayerInJail_WithReason()
    {
        Game game = CreateGameInJail();
        Player player = PlayerZero(game);

        game.TheJail.ReleasePlayerFromJail(player, ". Some reason for release");

        Assert.Contains(game.Logs.LogList, log =>
            log.Info == $"JailTurn 0: {player.Name} has been released from jail. Some reason for release.");
        Assert.DoesNotContain(player, game.TheJail.PlayersInJail.Keys);
    }

    private static Game CreateGameInJail(
        int money = 3_000,
        int jailCards = 0,
        int turnsInJail = 0,
        bool ownsProperty = false)
    {
        GameTestBuilder builder = new GameTestBuilder()
            .WithPlayer(0, money: money, jailCards: jailCards)
            .WithPlayerInJail(0, turnsInJail);

        if (ownsProperty)
            builder.WithSquare(5, ownerId: 0);

        return builder.Build();
    }

    private static Player PlayerZero(Game game) => game.Players.Single(player => player.Id == 0);

    private static bool InvokeBuyOutDecision(Game game)
    {
        PlayerEventHandler handler = (_, args) => ReferenceEquals(args.Player, PlayerZero(game));
        GameEvents.AskPlayerToBuyOutOfJailEvent += handler;
        try
        {
            return game.TheJail.TryPlayerBuyOut(PlayerZero(game));
        }
        finally
        {
            GameEvents.AskPlayerToBuyOutOfJailEvent -= handler;
        }
    }
}
