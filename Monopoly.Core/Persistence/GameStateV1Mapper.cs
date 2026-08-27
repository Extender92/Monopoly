using Monopoly.Core.Data;
using Monopoly.Core.Interface;
using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;

namespace Monopoly.Core.Persistence;

public static class GameStateV1Mapper
{
    public const int CurrentVersion = 1;

    public static GameStateV1 ToState(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return new GameStateV1
        {
            Version = CurrentVersion,
            Rules = GameRulesState.From(game.Rules),
            Players = game.Players.Select(PlayerState.From).ToList(),
            CurrentPlayerId = game.CurrentPlayer.Id,
            CurrentTurn = game.CurrentTurn,
            ConsecutiveDoubles = game.ConsecutiveDoubles,
            Fines = game.Fines,
            Squares = game.Board.Squares
                .Where(square => square.Owner is not null || square.IsMortgage || square is PropertySquare { Houses: > 0 })
                .Select(SquareState.From)
                .ToList(),
            Jail = game.TheJail.PlayersInJail
                .Select(pair => new JailState { PlayerId = pair.Key.Id, TurnsInJail = pair.Value.TurnsInJail })
                .ToList(),
            ChanceDeck = game.FortuneCard.GetChanceDeckOrder().ToList(),
            CommunityChestDeck = game.FortuneCard.GetCommunityChestDeckOrder().ToList()
        };
    }

    public static Game FromState(GameStateV1 state, IPlayerDecisionProvider? decisions = null)
        => FromState(state, decisions, null);

    internal static Game FromState(
        GameStateV1 state,
        IPlayerDecisionProvider? decisions,
        IReadOnlyList<IDie>? dice)
    {
        ArgumentNullException.ThrowIfNull(state);
        Validate(state);

        try
        {
            GameRules rules = state.Rules.ToGameRules();
            List<Player> players = state.Players.Select(player => player.ToPlayer()).ToList();
            Dictionary<int, Player> playersById = players.ToDictionary(player => player.Id);
            Player currentPlayer = playersById[state.CurrentPlayerId];
            IReadOnlyList<IDie> runtimeDice = dice ?? Enumerable.Range(0, rules.NumberOfDice)
                .Select(_ => (IDie)new Die(rules.DieSides))
                .ToList();

            Game game = new(players, currentPlayer, runtimeDice, rules, decisions);
            game.RestoreTurnState(state.Fines, state.CurrentTurn, state.ConsecutiveDoubles);

            foreach (SquareState squareState in state.Squares)
            {
                Square square = game.Board.GetSquareAtPosition(squareState.Position);
                Player? owner = squareState.OwnerId is int ownerId ? playersById[ownerId] : null;
                square.RestoreState(owner, squareState.IsMortgage, squareState.Houses);
            }

            foreach (JailState jailState in state.Jail)
                game.TheJail.RestorePlayerInJail(playersById[jailState.PlayerId], jailState.TurnsInJail);

            game.FortuneCard.RestoreDeckOrder(state.ChanceDeck, state.CommunityChestDeck);

            List<Player> activePlayers = players.Where(player => !player.IsBankrupt).ToList();
            if (activePlayers.Count <= 1)
                game.RestoreWinner(activePlayers.SingleOrDefault());

            game.ValidateAuthoritativeState();
            return game;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new GameStateValidationException("Saved game state could not be reconstructed.", exception);
        }
    }

    private static void Validate(GameStateV1 state)
    {
        if (state.Version != CurrentVersion)
            throw new GameStateValidationException($"Unsupported save version '{state.Version}'.");
        if (state.Rules is null || state.Players is null || state.Squares is null || state.Jail is null ||
            state.ChanceDeck is null || state.CommunityChestDeck is null)
            throw new GameStateValidationException("Save data is missing required sections.");
        if (state.Players.Any(player => player is null) ||
            state.Squares.Any(square => square is null) ||
            state.Jail.Any(jail => jail is null) ||
            state.ChanceDeck.Any(card => card is null) ||
            state.CommunityChestDeck.Any(card => card is null))
            throw new GameStateValidationException("Save data contains null collection items.");
        if (state.Players.Count == 0)
            throw new GameStateValidationException("A save must contain at least one player.");
        if (state.Players.Select(player => player.Id).Distinct().Count() != state.Players.Count)
            throw new GameStateValidationException("Player IDs must be unique.");
        if (!state.Players.Any(player => player.Id == state.CurrentPlayerId))
            throw new GameStateValidationException("CurrentPlayerId does not refer to a saved player.");
        if (state.Rules.NumberOfPlayers < state.Players.Count || state.Rules.NumberOfPlayers <= 0 ||
            state.Rules.NumberOfDice <= 0 || state.Rules.DieSides <= 0 || state.Rules.Salary < 0 ||
            state.Rules.MortgageInterestRate < 0 || state.Rules.JailFine < 0 || state.Rules.MaxTurnsInJail <= 0)
            throw new GameStateValidationException("Saved game rules are invalid.");
        if (!Enum.IsDefined(state.Rules.GameLanguage) || !Enum.IsDefined(state.Rules.FreeParking))
            throw new GameStateValidationException("Saved game-rule enum values are invalid.");

        GameRules rules = state.Rules.ToGameRules();
        GameBoard board = new(rules);
        if (state.CurrentTurn < 1 || state.ConsecutiveDoubles is < 0 or > 2 || state.Fines < 0)
            throw new GameStateValidationException("Saved turn state is invalid.");
        if (state.Players.Any(player => player.Id < 0 || string.IsNullOrWhiteSpace(player.Name) ||
            player.Position < 0 || player.Position >= board.Squares.Count || player.Money < 0 || player.NumberOfGetOutOfJailCards < 0))
            throw new GameStateValidationException("A saved player state is invalid.");
        if (state.Players.Any(player => player.IsBankrupt &&
            (player.Money != 0 || player.NumberOfGetOutOfJailCards != 0)))
            throw new GameStateValidationException("A bankrupt saved player cannot retain money or jail cards.");
        PlayerState savedCurrentPlayer = state.Players.Single(player => player.Id == state.CurrentPlayerId);
        if (savedCurrentPlayer.IsBankrupt)
            throw new GameStateValidationException("The current player cannot be bankrupt.");
        if (state.Squares.Any(square => square.Position < 0 || square.Position >= board.Squares.Count))
            throw new GameStateValidationException("A saved square position is outside the board.");
        if (state.Squares.Select(square => square.Position).Distinct().Count() != state.Squares.Count)
            throw new GameStateValidationException("Saved square states must contain unique positions.");
        if (state.Squares.Any(square => square.Houses is < 0 or > 5 ||
            (square.OwnerId is null && (square.IsMortgage || square.Houses > 0)) ||
            (square.IsMortgage && square.Houses > 0)))
            throw new GameStateValidationException("Saved ownership, mortgage, or building state is invalid.");

        HashSet<int> playerIds = state.Players.Select(player => player.Id).ToHashSet();
        if (state.Squares.Any(square => square.OwnerId is int ownerId && !playerIds.Contains(ownerId)))
            throw new GameStateValidationException("A saved square refers to an unknown owner.");
        HashSet<int> bankruptPlayerIds = state.Players
            .Where(player => player.IsBankrupt)
            .Select(player => player.Id)
            .ToHashSet();
        if (state.Squares.Any(square => square.OwnerId is int ownerId && bankruptPlayerIds.Contains(ownerId)))
            throw new GameStateValidationException("A bankrupt player cannot own a saved square.");
        if (state.Jail.Any(jail => !playerIds.Contains(jail.PlayerId)))
            throw new GameStateValidationException("A saved jail entry refers to an unknown player.");
        if (state.Jail.Any(jail => jail.TurnsInJail < 0 || jail.TurnsInJail > state.Rules.MaxTurnsInJail) ||
            state.Jail.Select(jail => jail.PlayerId).Distinct().Count() != state.Jail.Count)
            throw new GameStateValidationException("Saved jail states are invalid.");

        Dictionary<int, Square> squaresByPosition = board.Squares.ToDictionary(square => square.Position);
        if (state.Squares.Any(square => !squaresByPosition.TryGetValue(square.Position, out Square? boardSquare) ||
            (square.Houses > 0 && boardSquare is not PropertySquare) ||
            (square.OwnerId is not null && boardSquare is not PropertySquare and not RailroadSquare and not UtilitySquare)))
            throw new GameStateValidationException("Saved square state is incompatible with the board.");

        int jailPosition = board.Squares.Single(square => square.Name == "Jail").Position;
        Dictionary<int, PlayerState> playersById = state.Players.ToDictionary(player => player.Id);
        if (state.Jail.Any(jail => playersById[jail.PlayerId].IsBankrupt || playersById[jail.PlayerId].Position != jailPosition))
            throw new GameStateValidationException("Saved jail players must be active and positioned in jail.");

        int expectedChanceCards = FortuneCardBuilder.GetChanceCards(rules).Count;
        int expectedCommunityChestCards = FortuneCardBuilder.GetCommunityChestCards(rules).Count;
        HashSet<string> validChanceCards = Enumerable.Range(0, expectedChanceCards).Select(index => index.ToString()).ToHashSet();
        HashSet<string> validCommunityChestCards = Enumerable.Range(0, expectedCommunityChestCards).Select(index => index.ToString()).ToHashSet();
        if (state.ChanceDeck.Count != expectedChanceCards ||
            state.CommunityChestDeck.Count != expectedCommunityChestCards ||
            state.ChanceDeck.Distinct().Count() != state.ChanceDeck.Count ||
            state.CommunityChestDeck.Distinct().Count() != state.CommunityChestDeck.Count ||
            state.ChanceDeck.Any(card => !validChanceCards.Contains(card)) ||
            state.CommunityChestDeck.Any(card => !validCommunityChestCards.Contains(card)))
            throw new GameStateValidationException("Saved card deck state has an invalid length or order.");
    }
}

public sealed class GameStateV1
{
    public int Version { get; set; }
    public GameRulesState Rules { get; set; } = new();
    public List<PlayerState> Players { get; set; } = new();
    public int CurrentPlayerId { get; set; }
    public int CurrentTurn { get; set; }
    public int ConsecutiveDoubles { get; set; }
    public int Fines { get; set; }
    public List<SquareState> Squares { get; set; } = new();
    public List<JailState> Jail { get; set; } = new();
    public List<string> ChanceDeck { get; set; } = new();
    public List<string> CommunityChestDeck { get; set; } = new();
}

public sealed class GameRulesState
{
    public int NumberOfPlayers { get; set; }
    public int NumberOfDice { get; set; }
    public int DieSides { get; set; }
    public GameRules.Language GameLanguage { get; set; }
    public int Salary { get; set; }
    public bool DoubleOnGo { get; set; }
    public GameRules.Parking FreeParking { get; set; }
    public int MortgageInterestRate { get; set; }
    public int JailFine { get; set; }
    public int MaxTurnsInJail { get; set; }

    public static GameRulesState From(GameRules rules) => new()
    {
        NumberOfPlayers = rules.NumberOfPlayers,
        NumberOfDice = rules.NumberOfDice,
        DieSides = rules.DieSides,
        GameLanguage = rules.GameLanguage,
        Salary = rules.Salary,
        DoubleOnGo = rules.DoubleOnGo,
        FreeParking = rules.FreeParking,
        MortgageInterestRate = rules.MortgageInterestRate,
        JailFine = rules.JailFine,
        MaxTurnsInJail = rules.MaxTurnsInJail
    };

    public GameRules ToGameRules() => new(
        NumberOfPlayers,
        NumberOfDice,
        DieSides,
        GameLanguage,
        Salary,
        DoubleOnGo,
        FreeParking,
        MortgageInterestRate,
        JailFine,
        MaxTurnsInJail);
}

public sealed class PlayerState
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Money { get; set; }
    public int Position { get; set; }
    public int NumberOfGetOutOfJailCards { get; set; }
    public bool IsBankrupt { get; set; }

    public static PlayerState From(Player player) => new()
    {
        Id = player.Id,
        Name = player.Name,
        Money = player.Money,
        Position = player.Position,
        NumberOfGetOutOfJailCards = player.NumberOfGetOutOFJailCards,
        IsBankrupt = player.IsBankrupt
    };

    public Player ToPlayer()
    {
        Player player = new(Name, Id);
        player.RestoreState(Money, Position, NumberOfGetOutOfJailCards, IsBankrupt);
        return player;
    }
}

public sealed class SquareState
{
    public int Position { get; set; }
    public int? OwnerId { get; set; }
    public int Houses { get; set; }
    public bool IsMortgage { get; set; }

    public static SquareState From(Square square) => new()
    {
        Position = square.Position,
        OwnerId = square.Owner?.Id,
        Houses = square is PropertySquare property ? property.Houses : 0,
        IsMortgage = square.IsMortgage
    };
}

public sealed class JailState
{
    public int PlayerId { get; set; }
    public int TurnsInJail { get; set; }
}
