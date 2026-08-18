using System.Text.Json;
using Monopoly.Core.Data;
using Monopoly.Core.Interface;
using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;

namespace Monopoly.Core.SaveAndLoad;

public static class GameStateSerializer
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static void Save(Game game, string filePath)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        GameStateV1 state = new()
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
            Jail = game.TheJail.playersInJail
                .Select(pair => new JailState { PlayerId = pair.Key.Id, TurnsInJail = pair.Value.TurnsInJail })
                .ToList(),
            ChanceDeck = game.FortuneCard.GetChanceDeckOrder().ToList(),
            CommunityChestDeck = game.FortuneCard.GetCommunityChestDeckOrder().ToList()
        };

        File.WriteAllText(filePath, JsonSerializer.Serialize(state, JsonOptions));
    }

    public static Game Load(string filePath, IPlayerDecisionProvider? decisions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Save file '{filePath}' was not found.", filePath);

        GameStateV1? state = JsonSerializer.Deserialize<GameStateV1>(File.ReadAllText(filePath), JsonOptions);
        if (state is null || state.Version != CurrentVersion)
            throw new InvalidDataException($"Unsupported or missing save version. Expected version {CurrentVersion}.");

        Validate(state);

        GameRules rules = state.Rules.ToGameRules();
        List<Player> players = state.Players.Select(player => player.ToPlayer()).ToList();
        Dictionary<int, Player> playersById = players.ToDictionary(player => player.Id);
        Player currentPlayer = playersById[state.CurrentPlayerId];
        List<IDie> dice = Enumerable.Range(0, rules.NumberOfDice)
            .Select(_ => (IDie)new Die(rules.DieSides))
            .ToList();

        Game game = new(players, currentPlayer, dice, rules, new LogHandler(), decisions);
        game.Fines = state.Fines;
        game.CurrentTurn = state.CurrentTurn;
        game.RestoreConsecutiveDoubles(state.ConsecutiveDoubles);

        foreach (SquareState squareState in state.Squares)
        {
            Square square = game.Board.GetSquareAtPosition(squareState.Position);
            square.Owner = squareState.OwnerId is int ownerId ? playersById[ownerId] : null;
            square.IsMortgage = squareState.IsMortgage;
            if (square is PropertySquare property)
                property.Houses = squareState.Houses;
        }

        foreach (JailState jailState in state.Jail)
            game.TheJail.RestorePlayerInJail(playersById[jailState.PlayerId], jailState.TurnsInJail);

        game.FortuneCard.RestoreDeckOrder(state.ChanceDeck, state.CommunityChestDeck);

        List<Player> activePlayers = players.Where(player => !player.IsBankrupt).ToList();
        Player? winner = activePlayers.Count == 1 ? activePlayers[0] : null;
        if (activePlayers.Count <= 1)
            game.RestoreWinner(winner);

        return game;
    }

    private static void Validate(GameStateV1 state)
    {
        if (state.Rules is null || state.Players is null || state.Squares is null || state.Jail is null)
            throw new InvalidDataException("Save data is missing required sections.");
        if (state.Players.Count == 0)
            throw new InvalidDataException("A save must contain at least one player.");
        if (state.Players.Select(player => player.Id).Distinct().Count() != state.Players.Count)
            throw new InvalidDataException("Player IDs must be unique.");
        if (!state.Players.Any(player => player.Id == state.CurrentPlayerId))
            throw new InvalidDataException("CurrentPlayerId does not refer to a saved player.");
        if (state.Rules.NumberOfPlayers != state.Players.Count || state.Rules.NumberOfDice <= 0 || state.Rules.DieSides <= 0)
            throw new InvalidDataException("Saved game rules are invalid.");
        if (state.CurrentTurn < 1 || state.ConsecutiveDoubles is < 0 or > 2 || state.Fines < 0)
            throw new InvalidDataException("Saved turn state is invalid.");
        if (state.Players.Any(player => player.Position < 0 || player.Position >= 40 || player.Money < 0 || player.NumberOfGetOutOfJailCards < 0))
            throw new InvalidDataException("A saved player state is invalid.");
        if (state.Squares.Any(square => square.Position < 0 || square.Position >= 40))
            throw new InvalidDataException("A saved square position is outside the board.");
        if (state.Squares.Select(square => square.Position).Distinct().Count() != state.Squares.Count)
            throw new InvalidDataException("Saved square states must contain unique positions.");
        HashSet<int> playerIds = state.Players.Select(player => player.Id).ToHashSet();
        if (state.Squares.Any(square => square.OwnerId is int ownerId && !playerIds.Contains(ownerId)))
            throw new InvalidDataException("A saved square refers to an unknown owner.");
        if (state.Jail.Any(jail => !playerIds.Contains(jail.PlayerId)))
            throw new InvalidDataException("A saved jail entry refers to an unknown player.");
        if (state.Jail.Any(jail => jail.TurnsInJail < 0) ||
            state.Jail.Select(jail => jail.PlayerId).Distinct().Count() != state.Jail.Count)
            throw new InvalidDataException("Saved jail states are invalid.");

        GameRules rules = state.Rules.ToGameRules();
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
            throw new InvalidDataException("Saved card deck state has an invalid length or order.");
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

    public GameRules ToGameRules()
    {
        GameRules rules = new(NumberOfPlayers, NumberOfDice, DieSides)
        {
            Salary = Salary,
            DoubleOnGo = DoubleOnGo,
            FreeParking = FreeParking,
            MortgageInterestRate = MortgageInterestRate,
            JailFine = JailFine,
            MaxTurnsInJail = MaxTurnsInJail
        };
        rules.SetLanguage(GameLanguage);
        return rules;
    }
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

    public Player ToPlayer() => new(Name, Id)
    {
        Money = Money,
        Position = Position,
        NumberOfGetOutOFJailCards = NumberOfGetOutOfJailCards,
        IsBankrupt = IsBankrupt
    };
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
