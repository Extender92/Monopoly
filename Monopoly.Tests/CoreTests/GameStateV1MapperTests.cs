using Monopoly.Core;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Persistence;
using System.Text.Json;

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
        GameRules rules = new(
            2,
            2,
            6,
            language,
            salary: 250,
            freeParking: GameRules.Parking.Fines);
        TestDecisionProvider decisions = new();
        Game game = new GameTestBuilder(rules)
            .WithCurrentPlayer(1)
            .WithTurn(7, consecutiveDoubles: 1, fines: 35)
            .WithSquare(1, ownerId: 0, houses: 4)
            .WithSquare(5, ownerId: 0, isMortgage: true)
            .WithPlayerInJail(1, turnsInJail: 1)
            .WithDecisions(decisions)
            .Build();
        Player first = game.Players[0];
        Player second = game.Players[1];
        PropertySquare property = (PropertySquare)game.Board.GetSquareAtPosition(1);
        Square mortgagedSquare = game.Board.GetSquareAtPosition(5);
        Monopoly.Core.Presentation.PresentationToken[] chanceOrder = game.Decks.Resolve(LegacyStructureIds.PrimaryDeck).Cards.Select(card => card.PresentationToken).ToArray();
        Monopoly.Core.Presentation.PresentationToken[] chestOrder = game.Decks.Resolve(LegacyStructureIds.SecondaryDeck).Cards.Select(card => card.PresentationToken).ToArray();

        GameStateV1 state = GameStateV1Mapper.ToState(game);
        Game loaded = GameStateV1Mapper.FromState(state, decisions);

        Assert.Equal(GameStateV1Mapper.CurrentVersion, state.Version);
        Assert.Equal(language, loaded.Rules.GameLanguage);
        Assert.Equal(currency, loaded.Presentation.Resolve(loaded.Rules.PrimaryResourcePresentationToken).Symbol);
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
        Assert.Same(loadedFirst, loaded.Board.GetSquareAtPosition(mortgagedSquare.Position).Owner);
        Assert.True(loaded.Board.GetSquareAtPosition(mortgagedSquare.Position).IsMortgage);
        Assert.True(loaded.TheJail.IsPlayerInJail(loadedSecond));
        Assert.True(loaded.TheJail.TryGetJailInfo(loadedSecond, out Jail.JailStatus? jailStatus));
        Assert.NotNull(jailStatus);
        Assert.Equal(1, jailStatus.TurnsInJail);
        Assert.Throws<ArgumentException>(() => loaded.TheJail.TryGetJailInfo(second, out _));
        Assert.Equal(chanceOrder, loaded.Decks.Resolve(LegacyStructureIds.PrimaryDeck).Cards.Select(card => card.PresentationToken));
        Assert.Equal(chestOrder, loaded.Decks.Resolve(LegacyStructureIds.SecondaryDeck).Cards.Select(card => card.PresentationToken));
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
        Game loaded = new GameTestBuilder()
            .WithPlayer(1, money: 0, isBankrupt: true)
            .Build();

        Assert.Same(loaded.Players[0], loaded.Winner);
        Assert.True(loaded.IsGameOver);
    }

    [Fact]
    public void RoundTripRestoresGameAfterEliminatedPlayerWasRemoved()
    {
        Game game = new GameTestBuilder().WithPlayer(0, money: 0).Build();
        Player eliminated = game.Players[0];
        game.Handler.HandlePlayerBankruptcy(eliminated);

        GameStateV1 state = GameStateV1Mapper.ToState(game);
        Game loaded = GameStateV1Mapper.FromState(state);

        Assert.Single(loaded.Players);
        Assert.Equal(2, loaded.Rules.NumberOfPlayers);
        Assert.Equal(game.Winner!.Id, loaded.Winner!.Id);
        Assert.Same(loaded.Winner, loaded.CurrentPlayer);
        Assert.True(loaded.IsGameOver);
    }

    [Theory]
    [InlineData(InvalidCandidate.UnsupportedVersion)]
    [InlineData(InvalidCandidate.DuplicatePlayer)]
    [InlineData(InvalidCandidate.DuplicateSquare)]
    [InlineData(InvalidCandidate.UnknownOwner)]
    [InlineData(InvalidCandidate.MortgageWithoutOwner)]
    [InlineData(InvalidCandidate.MortgageWithBuildings)]
    [InlineData(InvalidCandidate.NonPurchasableOwner)]
    [InlineData(InvalidCandidate.BankruptOwner)]
    [InlineData(InvalidCandidate.BankruptAssets)]
    [InlineData(InvalidCandidate.InvalidJailPosition)]
    [InlineData(InvalidCandidate.DuplicateJailEntry)]
    [InlineData(InvalidCandidate.InvalidTurnState)]
    [InlineData(InvalidCandidate.InvalidDeckOrder)]
    [InlineData(InvalidCandidate.BankruptCurrentPlayer)]
    public void FromStateRejectsCompleteInvalidCandidateWithoutMutatingExistingGame(InvalidCandidate invalidCandidate)
    {
        Game existing = new GameTestBuilder()
            .WithCurrentPlayer(1)
            .WithTurn(4, consecutiveDoubles: 1, fines: 25)
            .WithSquare(5, ownerId: 0)
            .Build();
        string existingSnapshot = JsonSerializer.Serialize(GameStateV1Mapper.ToState(existing));
        GameStateV1 candidate = GameStateV1Mapper.ToState(existing);
        MakeInvalid(candidate, invalidCandidate);

        Assert.Throws<GameStateValidationException>(() => GameStateV1Mapper.FromState(candidate));

        Assert.Equal(existingSnapshot, JsonSerializer.Serialize(GameStateV1Mapper.ToState(existing)));
    }

    private static void MakeInvalid(GameStateV1 state, InvalidCandidate invalidCandidate)
    {
        switch (invalidCandidate)
        {
            case InvalidCandidate.UnsupportedVersion:
                state.Version = 2;
                break;
            case InvalidCandidate.DuplicatePlayer:
                state.Players[1].Id = state.Players[0].Id;
                break;
            case InvalidCandidate.DuplicateSquare:
                state.Squares.Add(new SquareState { Position = 5, OwnerId = 0 });
                break;
            case InvalidCandidate.UnknownOwner:
                state.Squares.Add(new SquareState { Position = 1, OwnerId = 999 });
                break;
            case InvalidCandidate.MortgageWithoutOwner:
                state.Squares.Add(new SquareState { Position = 1, IsMortgage = true });
                break;
            case InvalidCandidate.MortgageWithBuildings:
                state.Squares.Add(new SquareState { Position = 1, OwnerId = 0, IsMortgage = true, Houses = 1 });
                break;
            case InvalidCandidate.NonPurchasableOwner:
                state.Squares.Add(new SquareState { Position = 0, OwnerId = 0 });
                break;
            case InvalidCandidate.BankruptOwner:
                state.Players[0].IsBankrupt = true;
                state.Players[0].Money = 0;
                state.Players[0].NumberOfGetOutOfJailCards = 0;
                break;
            case InvalidCandidate.BankruptAssets:
                state.Players[0].IsBankrupt = true;
                break;
            case InvalidCandidate.InvalidJailPosition:
                state.Jail.Add(new JailState { PlayerId = 0, TurnsInJail = 1 });
                break;
            case InvalidCandidate.DuplicateJailEntry:
                state.Players[0].Position = 10;
                state.Jail.Add(new JailState { PlayerId = 0, TurnsInJail = 1 });
                state.Jail.Add(new JailState { PlayerId = 0, TurnsInJail = 2 });
                break;
            case InvalidCandidate.InvalidTurnState:
                state.ConsecutiveDoubles = 3;
                break;
            case InvalidCandidate.InvalidDeckOrder:
                state.ChanceDeck[0] = state.ChanceDeck[1];
                break;
            case InvalidCandidate.BankruptCurrentPlayer:
                state.Players.Single(player => player.Id == state.CurrentPlayerId).IsBankrupt = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(invalidCandidate));
        }
    }

    public enum InvalidCandidate
    {
        UnsupportedVersion,
        DuplicatePlayer,
        DuplicateSquare,
        UnknownOwner,
        MortgageWithoutOwner,
        MortgageWithBuildings,
        NonPurchasableOwner,
        BankruptOwner,
        BankruptAssets,
        InvalidJailPosition,
        DuplicateJailEntry,
        InvalidTurnState,
        InvalidDeckOrder,
        BankruptCurrentPlayer
    }

    private sealed class TestDecisionProvider : IPlayerDecisionProvider
    {
        public bool ResolveInsufficientFunds(Game game, Player player, int amount) => false;
    }
}
