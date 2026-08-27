using Monopoly.Core.Events;
using Monopoly.Core.Interface;
using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System.Collections.ObjectModel;

namespace Monopoly.Core;

public sealed class Game : IGame
{
    private readonly List<Player> _players;
    private readonly ReadOnlyCollection<Player> _playersView;
    private readonly List<IDie> _dice;
    private readonly ReadOnlyCollection<IDieView> _diceView;

    internal GameHandler Handler { get; }
    private readonly ILogHandler _logs;
    public IGameLog Logs => _logs;
    internal ILogHandler LogWriter => _logs;
    public GameBoard Board { get; }
    public IReadOnlyList<Player> Players => _playersView;
    public Player CurrentPlayer { get; private set; }
    public IReadOnlyList<IDieView> Dice => _diceView;
    internal IReadOnlyList<IDie> DiceControllers => _dice;
    public GameRules Rules { get; }
    internal Transaction Transactions { get; }
    public Jail TheJail { get; }
    public FortuneCardHandler FortuneCard { get; }
    internal IPlayerDecisionProvider Decisions { get; private set; }
    public int Fines { get; private set; }
    public int CurrentTurn { get; private set; }
    public int ConsecutiveDoubles { get; private set; }
    public Player? Winner { get; private set; }
    public bool IsGameOver => Winner is not null || Players.Count(p => !p.IsBankrupt) <= 1;

    public Game(
        IEnumerable<Player> players,
        Player currentPlayer,
        GameRules rules,
        IPlayerDecisionProvider? decisions = null)
        : this(players, currentPlayer, CreateDice(rules), rules, new LogHandler(), decisions)
    {
    }

    internal Game(
        IEnumerable<Player> players,
        Player currentPlayer,
        IEnumerable<IDie> dice,
        GameRules rules,
        IPlayerDecisionProvider? decisions = null)
        : this(players, currentPlayer, dice, rules, new LogHandler(), decisions)
    {
    }

    internal Game(
        IEnumerable<Player> players,
        Player currentPlayer,
        IEnumerable<IDie> dice,
        GameRules rules,
        ILogHandler logs,
        IPlayerDecisionProvider? decisions = null)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(currentPlayer);
        ArgumentNullException.ThrowIfNull(dice);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(logs);

        _players = players.ToList();
        if (_players.Count == 0 || _players.Any(player => player is null))
            throw new ArgumentException("A game requires at least one non-null player.", nameof(players));
        if (_players.Select(player => player.Id).Distinct().Count() != _players.Count)
            throw new ArgumentException("Player IDs must be unique.", nameof(players));
        if (!_players.Any(player => ReferenceEquals(player, currentPlayer)))
            throw new ArgumentException("The current player must belong to the game.", nameof(currentPlayer));
        if (currentPlayer.IsBankrupt)
            throw new ArgumentException("The current player cannot be bankrupt.", nameof(currentPlayer));
        if (_players.Count > rules.NumberOfPlayers)
            throw new ArgumentException("The supplied players cannot exceed the configured player count.", nameof(players));

        _dice = dice.ToList();
        if (_dice.Any(die => die is null) || _dice.Count != rules.NumberOfDice)
            throw new ArgumentException("The supplied dice must match the game rules.", nameof(dice));
        if (_dice.Any(die => die.GetDieType() != rules.DieSides))
            throw new ArgumentException("Every die must match the configured number of sides.", nameof(dice));

        _playersView = _players.AsReadOnly();
        _diceView = _dice
            .Select(die => (IDieView)new ReadOnlyDieView(die))
            .ToList()
            .AsReadOnly();
        CurrentPlayer = currentPlayer;
        Rules = rules;
        _logs = logs;
        Decisions = decisions ?? new DefaultPlayerDecisionProvider();

        Fines = 0;
        CurrentTurn = 1;
        Board = new GameBoard(rules);
        FortuneCard = new FortuneCardHandler(rules);
        TheJail = new Jail(this, Board.Squares.First(s => s.Name == "Jail").Position);
        Handler = new GameHandler(this);
        Transactions = new Transaction(this);

        if (_logs is LogHandler logHandler)
            logHandler.OwnerGame = this;
    }

    public void SetDecisionProvider(IPlayerDecisionProvider decisions)
    {
        Decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
    }

    public bool TryBuyHouse(Player player, PropertySquare property) =>
        Transactions.TryBuyPropertyHouse(player, property);

    public bool TrySellHouse(Player player, PropertySquare property) =>
        Transactions.TrySellPropertyHouse(player, property);

    public bool TryMortgageProperty(Player player, Square square) =>
        Transactions.TryMortgageProperty(player, square);

    public bool TryRepayMortgage(Player player, Square square) =>
        Transactions.TryRepayMortgageProperty(player, square);

    /// <summary>Executes one complete dice roll.</summary>
    public TurnResult PlayTurn()
    {
        if (IsGameOver)
        {
            Winner ??= Players.FirstOrDefault(p => !p.IsBankrupt);
            return new TurnResult { Player = CurrentPlayer, GameOver = true, Winner = Winner };
        }

        Player player = CurrentPlayer;
        if (player.IsBankrupt)
        {
            AdvanceToNextActivePlayer();
            return new TurnResult { Player = player, PlayerBankrupt = true, GameOver = IsGameOver, Winner = Winner };
        }

        if (TheJail.IsPlayerInJail(player))
            return PlayJailTurn(player);

        Handler.RollDice(player);
        IReadOnlyList<int> results = Dice.Select(die => die.GetDieResult()).ToList().AsReadOnly();
        int diceSum = Handler.CalculateDiceSum();
        bool isDouble = Handler.IsDiceDouble();

        if (isDouble && ConsecutiveDoubles == 2)
        {
            ConsecutiveDoubles = 0;
            TheJail.PlayerGoToJail(player, "Rolled doubles three times in a row");
            AdvanceToNextActivePlayer();
            return BuildResult(player, results, diceSum, null, true, true, false, false);
        }

        Square landedSquare = MovePlayerBySteps(player, diceSum);
        GameEvents.InvokeLandOnSquare(this, landedSquare);
        landedSquare.LandOn(player, this);

        bool bankrupt = player.IsBankrupt;
        bool sentToJail = !bankrupt && TheJail.IsPlayerInJail(player);
        if (!bankrupt && (sentToJail || !isDouble))
        {
            ConsecutiveDoubles = 0;
            AdvanceToNextActivePlayer();
        }
        else if (!bankrupt)
        {
            ConsecutiveDoubles++;
            CurrentTurn++;
        }

        return BuildResult(player, results, diceSum, landedSquare, isDouble, sentToJail, false, isDouble && !bankrupt && !sentToJail);
    }

    private TurnResult PlayJailTurn(Player player)
    {
        if (Decisions.ConfirmJailBuyout(player))
        {
            if (player.NumberOfGetOutOFJailCards > 0)
            {
                TheJail.BuyOutPlayerFromJail(player);
                TheJail.ReleasePlayerFromJail(player, ", used a Get Out of Jail For Free card");
            }
            else if (Handler.TryResolvePayment(player, Rules.JailFine, null, "Could not afford to pay Jail Fine"))
            {
                TheJail.ReleasePlayerFromJail(player, ", paid the fine to get out of jail");
            }

            if (player.IsBankrupt)
            {
                AdvanceToNextActivePlayer();
                return BuildResult(player, Array.Empty<int>(), 0, null, false, false, false, false, true);
            }
        }

        Handler.RollDice(player);
        IReadOnlyList<int> results = Dice.Select(die => die.GetDieResult()).ToList().AsReadOnly();
        int diceSum = Handler.CalculateDiceSum();
        bool isDouble = Handler.IsDiceDouble();

        if (!TheJail.TryGetJailInfo(player, out _))
        {
            AdvanceToNextActivePlayer();
            return BuildResult(player, results, diceSum, null, isDouble, false, false, false);
        }

        if (isDouble)
        {
            TheJail.ReleasePlayerFromJail(player, ", rolled doubles");
            Square landedSquare = MovePlayerBySteps(player, diceSum);
            GameEvents.InvokeLandOnSquare(this, landedSquare);
            landedSquare.LandOn(player, this);
            ConsecutiveDoubles = 0;
            AdvanceToNextActivePlayer();
            return BuildResult(player, results, diceSum, landedSquare, true, false, true, false);
        }

        TheJail.IncrementTurnsInJail(player);
        if (TheJail.PlayerReachedMaxTurnsInJail(player))
        {
            if (player.NumberOfGetOutOFJailCards > 0)
            {
                TheJail.BuyOutPlayerFromJail(player);
                TheJail.ReleasePlayerFromJail(player, ", used a Get Out of Jail For Free card");
            }
            else if (Handler.TryResolvePayment(player, Rules.JailFine, null, "Could not afford to pay Jail Fine"))
            {
                TheJail.ReleasePlayerFromJail(player, ", paid the fine to get out of jail");
            }
            else
            {
                AdvanceToNextActivePlayer();
                return BuildResult(player, results, diceSum, null, false, false, false, false, true);
            }
        }

        AdvanceToNextActivePlayer();
        return BuildResult(player, results, diceSum, null, false, false, false, false);
    }

    private TurnResult BuildResult(
        Player player,
        IReadOnlyList<int> results,
        int diceSum,
        Square? landedSquare,
        bool wasDouble,
        bool wasSentToJail,
        bool wasReleasedFromJailByDouble,
        bool extraTurn,
        bool playerBankrupt = false)
    {
        return new TurnResult
        {
            Player = player,
            DiceResults = results,
            DiceSum = diceSum,
            LandedSquare = landedSquare,
            WasDouble = wasDouble,
            WasSentToJail = wasSentToJail,
            WasReleasedFromJailByDouble = wasReleasedFromJailByDouble,
            ExtraTurn = extraTurn,
            PlayerBankrupt = playerBankrupt || player.IsBankrupt,
            GameOver = IsGameOver,
            Winner = Winner
        };
    }

    internal Square MovePlayerBySteps(Player player, int steps)
    {
        Handler.MovePlayerAndInvokeEvent(player, player.Position + steps);
        return Board.GetSquareAtPosition(player.Position);
    }

    internal void NextPlayer() => AdvanceToNextActivePlayer();

    private void AdvanceToNextActivePlayer()
    {
        if (Players.Count == 0)
        {
            Winner = null;
            return;
        }

        List<Player> activePlayers = Players.Where(p => !p.IsBankrupt).ToList();
        if (activePlayers.Count <= 1)
        {
            Winner = activePlayers.SingleOrDefault();
            if (Winner is not null && !ReferenceEquals(CurrentPlayer, Winner))
                TransitionToPlayer(Winner);
            return;
        }

        int currentIndex = _players.IndexOf(CurrentPlayer);
        if (currentIndex < 0) currentIndex = -1;

        for (int offset = 1; offset <= Players.Count; offset++)
        {
            Player candidate = Players[(currentIndex + offset + Players.Count) % Players.Count];
            if (!candidate.IsBankrupt)
            {
                TransitionToPlayer(candidate);
                return;
            }
        }
    }

    internal void RemovePlayer(Player player)
    {
        int removedIndex = _players.IndexOf(player);
        if (removedIndex < 0) return;

        bool removedCurrentPlayer = ReferenceEquals(CurrentPlayer, player);
        _players.RemoveAt(removedIndex);

        if (Players.Count == 0)
        {
            Winner = null;
            return;
        }

        List<Player> activePlayers = Players.Where(candidate => !candidate.IsBankrupt).ToList();
        Winner = activePlayers.Count == 1 ? activePlayers[0] : null;

        if (Winner is not null)
        {
            if (!ReferenceEquals(CurrentPlayer, Winner))
                TransitionToPlayer(Winner);
            return;
        }

        if (!removedCurrentPlayer) return;

        for (int offset = 0; offset < Players.Count; offset++)
        {
            Player candidate = Players[(removedIndex + offset) % Players.Count];
            if (!candidate.IsBankrupt)
            {
                TransitionToPlayer(candidate);
                return;
            }
        }
    }

    private void TransitionToPlayer(Player player)
    {
        CurrentPlayer = player;
        CurrentTurn = 1;
        ConsecutiveDoubles = 0;
    }

    internal bool ContainsPlayer(Player player) =>
        _players.Any(candidate => ReferenceEquals(candidate, player));

    internal bool ContainsSquare(Square square) =>
        Board.Squares.Any(candidate => ReferenceEquals(candidate, square));

    internal void RestoreTurnState(int fines, int currentTurn, int consecutiveDoubles)
    {
        if (fines < 0) throw new ArgumentOutOfRangeException(nameof(fines));
        if (currentTurn < 1) throw new ArgumentOutOfRangeException(nameof(currentTurn));
        if (consecutiveDoubles is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(consecutiveDoubles));

        Fines = fines;
        CurrentTurn = currentTurn;
        ConsecutiveDoubles = consecutiveDoubles;
    }

    internal int TakeFines()
    {
        int fines = Fines;
        Fines = 0;
        return fines;
    }

    internal void AddFines(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Fines = checked(Fines + amount);
    }

    internal void RestoreWinner(Player? winner)
    {
        if (winner is not null && (!ContainsPlayer(winner) || winner.IsBankrupt))
            throw new ArgumentException("The winner must be an active player in the game.", nameof(winner));
        Winner = winner;
    }

    internal void ValidateAuthoritativeState()
    {
        if (_players.Count == 0 || _players.Count > Rules.NumberOfPlayers)
            throw new InvalidOperationException("The active match roster is inconsistent with the configured player count.");
        if (!ContainsPlayer(CurrentPlayer) || CurrentPlayer.IsBankrupt)
            throw new InvalidOperationException("The current player must be active and belong to the game.");
        if (Board.Squares.Select(square => square.Position).Distinct().Count() != Board.Squares.Count)
            throw new InvalidOperationException("Board positions must be unique.");
        if (Board.Squares.Any(square => square.Owner is not null && !ContainsPlayer(square.Owner)))
            throw new InvalidOperationException("Every square owner must belong to the game.");
        if (Board.Squares.Any(square => square.IsMortgage && square.Owner is null) ||
            Board.Squares.OfType<PropertySquare>().Any(property =>
                property.Houses is < 0 or > 5 ||
                (property.Houses > 0 && property.Owner is null) ||
                (property.IsMortgage && property.Houses > 0)))
            throw new InvalidOperationException("Square ownership, mortgage, and building state is inconsistent.");
        if (_players.Any(player => player.IsBankrupt &&
            (player.Money != 0 || player.NumberOfGetOutOFJailCards != 0)))
            throw new InvalidOperationException("Bankrupt players cannot retain money or jail cards.");
        if (Board.Squares.Any(square => square.Owner?.IsBankrupt == true))
            throw new InvalidOperationException("Bankrupt players cannot own squares.");
        if (TheJail.PlayersInJail.Any(entry =>
                !ContainsPlayer(entry.Key) || entry.Key.IsBankrupt || entry.Key.Position != TheJail.JailPosition))
            throw new InvalidOperationException("Jail entries must refer to active players at the jail position.");

        List<Player> activePlayers = _players.Where(player => !player.IsBankrupt).ToList();
        Player? expectedWinner = activePlayers.Count == 1 ? activePlayers[0] : null;
        if (!ReferenceEquals(Winner, expectedWinner) && (Winner is not null || activePlayers.Count <= 1))
            throw new InvalidOperationException("Winner state is inconsistent with the active players.");
    }

    private static IReadOnlyList<IDie> CreateDice(GameRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return Enumerable.Range(0, rules.NumberOfDice)
            .Select(_ => (IDie)new Die(rules.DieSides))
            .ToList()
            .AsReadOnly();
    }

    private sealed class ReadOnlyDieView(IDie die) : IDieView
    {
        public int GetDieResult() => die.GetDieResult();

        public int GetDieType() => die.GetDieType();
    }
}
