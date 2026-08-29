using Monopoly.Core.Interface;
using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Notifications;
using Monopoly.Core.Presentation;
using Monopoly.Core.Randomness;
using System.Collections.ObjectModel;

namespace Monopoly.Core;

public sealed class Game : IGame
{
    private readonly List<Player> _players;
    private readonly ReadOnlyCollection<Player> _playersView;
    private readonly HashSet<Guid> _consumedDecisionIds = new();
    private readonly GameNotificationHub _notifications = new();
    private TurnContinuation? _turnContinuation;
    private Guid? _lastConsumedDecisionId;
    private int _notificationDispatchDepth;

    internal GameHandler Handler { get; }
    private readonly ILogHandler _logs;
    public IGameLog Logs => _logs;
    public IGameNotificationSource Notifications => _notifications;
    internal ILogHandler LogWriter => _logs;
    internal FortuneCardHandler CardHandler { get; }
    public GameBoard Board { get; }
    public DeckCollection Decks => CardHandler.CreateSnapshot();
    public IReadOnlyList<Player> Players => _playersView;
    public Player CurrentPlayer { get; private set; }
    public DiceRoll? LastDiceRoll { get; private set; }
    internal MatchRandomizer Randomizer { get; }
    public GameRules Rules { get; }
    public ProfilePresentation Presentation { get; }
    internal Transaction Transactions { get; }
    public Jail TheJail { get; }
    internal IPlayerDecisionProvider Decisions { get; private set; }
    public int Fines { get; private set; }
    public int CurrentTurn { get; private set; }
    public int ConsecutiveDoubles { get; private set; }
    public Player? Winner { get; private set; }
    public bool IsGameOver => Winner is not null || Players.Count(p => !p.IsBankrupt) <= 1;
    public GamePhase Phase { get; private set; }
    public PendingDecision? PendingDecision { get; private set; }
    internal TurnContinuation? TurnContinuationSnapshot => _turnContinuation;
    internal Guid? LastConsumedDecisionId => _lastConsumedDecisionId;
    internal IReadOnlyCollection<Guid> ConsumedDecisionIds => _consumedDecisionIds;
    internal int NotificationSubscriberCount => _notifications.SubscriberCount;

    public Game(
        IEnumerable<Player> players,
        Player currentPlayer,
        GameRules rules,
        IPlayerDecisionProvider? decisions = null,
        ProfilePresentation? presentation = null,
        IMatchRandomSource? randomSource = null)
        : this(players, currentPlayer, rules, new LogHandler(), decisions, presentation, randomSource, true)
    {
    }

    internal Game(
        IEnumerable<Player> players,
        Player currentPlayer,
        GameRules rules,
        IPlayerDecisionProvider? decisions,
        ProfilePresentation? presentation,
        IMatchRandomSource? randomSource,
        bool shuffleDecks)
        : this(players, currentPlayer, rules, new LogHandler(), decisions, presentation, randomSource, shuffleDecks)
    {
    }

    internal Game(
        IEnumerable<Player> players,
        Player currentPlayer,
        GameRules rules,
        ILogHandler logs,
        IPlayerDecisionProvider? decisions = null,
        ProfilePresentation? presentation = null,
        IMatchRandomSource? randomSource = null,
        bool shuffleDecks = true)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(currentPlayer);
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

        _playersView = _players.AsReadOnly();
        CurrentPlayer = currentPlayer;
        Rules = rules;
        _logs = logs;
        Decisions = decisions ?? new DefaultPlayerDecisionProvider();
        Randomizer = new MatchRandomizer(randomSource ?? new SystemMatchRandomSource());

        Fines = 0;
        CurrentTurn = 1;
        Phase = GamePhase.ReadyForTurn;
        Board = new GameBoard(rules);
        CardHandler = new FortuneCardHandler(rules, Randomizer, shuffleDecks);
        CardHandler.EnsureReferences(Board.ReferencedDeckIds);
        ProfilePresentation defaultPresentation = LegacyPresentationFactory.Create(rules, Board, CardHandler);
        Presentation = presentation ?? defaultPresentation;
        Presentation.EnsureReferences(LegacyPresentationFactory.RequiredReferences(Board, CardHandler));
        TheJail = new Jail(this, Board.Squares.OfType<JailSquare>().Single().Position);
        Handler = new GameHandler(this);
        Transactions = new Transaction(this);

        if (_logs is LogHandler logHandler)
            logHandler.OwnerGame = this;
    }

    internal string ResolveDisplayText(PresentationToken token) => Presentation.ResolveDisplayText(token);

    internal string FormatAmount(int amount)
    {
        string? symbol = Presentation.Resolve(Rules.PrimaryResourcePresentationToken).Symbol;
        return symbol is null ? amount.ToString() : $"{amount}{symbol}";
    }

    public void SetDecisionProvider(IPlayerDecisionProvider decisions)
    {
        EnsureNotificationIsNotBeingPublished();
        Decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
    }

    public bool TryBuyHouse(Player player, PropertySquare property) =>
        _notificationDispatchDepth == 0 && Transactions.TryBuyPropertyHouse(player, property);

    public bool TrySellHouse(Player player, PropertySquare property) =>
        _notificationDispatchDepth == 0 && Transactions.TrySellPropertyHouse(player, property);

    public bool TryMortgageProperty(Player player, Square square) =>
        _notificationDispatchDepth == 0 && Transactions.TryMortgageProperty(player, square);

    public bool TryRepayMortgage(Player player, Square square) =>
        _notificationDispatchDepth == 0 && Transactions.TryRepayMortgageProperty(player, square);

    /// <summary>Starts a turn and runs until it completes or requires a frontend decision.</summary>
    public GameActionResult PlayTurn()
    {
        if (_notificationDispatchDepth > 0)
            return GameActionResult.Rejected(GameActionRejectionReason.OperationInProgress, PendingDecision);

        if (Phase == GamePhase.AwaitingDecision)
            return GameActionResult.Rejected(GameActionRejectionReason.PendingDecisionRequired, PendingDecision);

        if (IsGameOver)
        {
            Winner ??= Players.FirstOrDefault(p => !p.IsBankrupt);
            Phase = GamePhase.GameOver;
            _notifications.Complete();
            return GameActionResult.Over(new TurnResult { Player = CurrentPlayer, GameOver = true, Winner = Winner });
        }

        Player player = CurrentPlayer;
        if (player.IsBankrupt)
        {
            AdvanceToNextActivePlayer();
            return CompleteAction(new TurnResult { Player = player, PlayerBankrupt = true, GameOver = IsGameOver, Winner = Winner });
        }

        if (TheJail.IsPlayerInJail(player))
            return RequestJailDecision(player);

        DiceRoll roll = Handler.RollDice(player, RandomPurpose.TurnDice);

        if (roll.IsDouble && ConsecutiveDoubles == 2)
        {
            ConsecutiveDoubles = 0;
            TheJail.PlayerGoToJail(player, "Rolled doubles three times in a row");
            AdvanceToNextActivePlayer();
            return CompleteAction(BuildResult(player, roll, null, true, false, false));
        }

        Square landedSquare = MovePlayerBySteps(player, roll.Sum);
        _turnContinuation = new TurnContinuation(
            TurnContinuationKind.StandardLanding,
            player.Id,
            roll,
            landedSquare.Position,
            false);
        PublishNotification(new SpaceReachedNotification(landedSquare));
        landedSquare.LandOn(player, this);

        if (PendingDecision is not null)
            return GameActionResult.DecisionRequired(PendingDecision);

        return CompleteTurnContinuation(player);
    }

    public GameActionResult SubmitDecision(DecisionResponse? response)
    {
        if (_notificationDispatchDepth > 0)
            return GameActionResult.Rejected(GameActionRejectionReason.OperationInProgress, PendingDecision);

        if (response is null || response.DecisionId == Guid.Empty || !Enum.IsDefined(response.Response))
            return GameActionResult.Rejected(GameActionRejectionReason.MalformedResponse, PendingDecision);

        if (PendingDecision is null)
        {
            GameActionRejectionReason reason = response.DecisionId == _lastConsumedDecisionId
                ? GameActionRejectionReason.DuplicateDecision
                : _consumedDecisionIds.Contains(response.DecisionId)
                    ? GameActionRejectionReason.StaleDecision
                    : GameActionRejectionReason.NoPendingDecision;
            return GameActionResult.Rejected(reason);
        }

        if (response.DecisionId != PendingDecision.DecisionId)
        {
            GameActionRejectionReason reason = response.DecisionId == _lastConsumedDecisionId
                ? GameActionRejectionReason.DuplicateDecision
                : GameActionRejectionReason.StaleDecision;
            return GameActionResult.Rejected(reason, PendingDecision);
        }

        if (!PendingDecision.AllowedResponses.Contains(response.Response) || !CanApplyPendingDecision(PendingDecision, response.Response))
            return GameActionResult.Rejected(GameActionRejectionReason.ResponseNotAllowed, PendingDecision);

        PendingDecision acceptedDecision = PendingDecision;
        DiceRoll? preparedDetentionRoll = acceptedDecision is JailReleaseDecision
            ? Handler.PrepareDiceRoll(RandomPurpose.DetentionDice)
            : null;
        ConsumePendingDecision(acceptedDecision.DecisionId);

        return acceptedDecision switch
        {
            PropertyPurchaseDecision purchase => ResumePropertyPurchase(purchase, response.Response),
            JailReleaseDecision => ResumeJailTurn(response.Response, preparedDetentionRoll!),
            _ => throw new InvalidOperationException("The pending decision type is not supported.")
        };
    }

    internal void RequestPropertyPurchase(Player player, Square square)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(square);
        if (!ContainsPlayer(player) || !ReferenceEquals(player, CurrentPlayer))
            throw new ArgumentException("The purchasing player must be the current player in this game.", nameof(player));
        if (!ContainsSquare(square))
            throw new ArgumentException("The square does not belong to this game.", nameof(square));
        if (_turnContinuation is null)
            throw new InvalidOperationException("A purchase decision can only be requested while a turn is in progress.");
        if (PendingDecision is not null)
            throw new InvalidOperationException("Only one decision can be pending at a time.");
        if (player.IsBankrupt || square.Owner is not null || square.Price < 0 || !Handler.CanAffordWithAssets(player, square.Price))
            return;

        PendingDecision = new PropertyPurchaseDecision(Guid.NewGuid(), player.Id, square.Position, square.Price);
        Phase = GamePhase.AwaitingDecision;
    }

    private GameActionResult RequestJailDecision(Player player)
    {
        Jail.JailStatus jailStatus = TheJail.GetJailInfo(player);
        PendingDecision = new JailReleaseDecision(
            Guid.NewGuid(),
            player.Id,
            Rules.JailFine,
            player.NumberOfGetOutOFJailCards > 0,
            jailStatus.TurnsInJail,
            Rules.MaxTurnsInJail);
        Phase = GamePhase.AwaitingDecision;
        return GameActionResult.DecisionRequired(PendingDecision);
    }

    private bool CanApplyPendingDecision(PendingDecision decision, DecisionOption response)
    {
        Player? player = _players.SingleOrDefault(candidate => candidate.Id == decision.PlayerId);
        if (player is null || player.IsBankrupt || !ReferenceEquals(player, CurrentPlayer))
            return false;

        return decision switch
        {
            PropertyPurchaseDecision purchase =>
                _turnContinuation is not null &&
                Board.GetSquareAtPosition(purchase.SquarePosition) is Square square &&
                square.Owner is null &&
                square.Price == purchase.Price &&
                (response != DecisionOption.Purchase || Handler.CanAffordWithAssets(player, purchase.Price)),
            JailReleaseDecision jail =>
                _turnContinuation is null &&
                TheJail.TryGetJailInfo(player, out Jail.JailStatus? status) &&
                jail.Fine == Rules.JailFine &&
                jail.HasGetOutOfJailCard == (player.NumberOfGetOutOFJailCards > 0) &&
                jail.TurnsInJail == status.TurnsInJail &&
                jail.MaximumTurnsInJail == Rules.MaxTurnsInJail,
            _ => false
        };
    }

    private void ConsumePendingDecision(Guid decisionId)
    {
        _consumedDecisionIds.Add(decisionId);
        _lastConsumedDecisionId = decisionId;
        PendingDecision = null;
        Phase = GamePhase.ReadyForTurn;
    }

    private GameActionResult ResumePropertyPurchase(PropertyPurchaseDecision decision, DecisionOption response)
    {
        Player player = _players.Single(candidate => candidate.Id == decision.PlayerId);
        Square square = Board.GetSquareAtPosition(decision.SquarePosition);
        if (response == DecisionOption.Purchase)
            Transactions.TryBuyPurchasableSquareAfterDecision(player, square);

        return CompleteTurnContinuation(player);
    }

    private GameActionResult ResumeJailTurn(DecisionOption response, DiceRoll roll)
    {
        Player player = CurrentPlayer;
        if (response == DecisionOption.LeaveJail)
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
                return CompleteAction(BuildResult(player, null, null, false, false, false, true));
            }
        }

        Handler.CommitDiceRoll(player, roll);

        if (!TheJail.TryGetJailInfo(player, out _))
        {
            AdvanceToNextActivePlayer();
            return CompleteAction(BuildResult(player, roll, null, false, false, false));
        }

        if (roll.IsDouble)
        {
            TheJail.ReleasePlayerFromJail(player, ", rolled doubles");
            Square landedSquare = MovePlayerBySteps(player, roll.Sum);
            _turnContinuation = new TurnContinuation(
                TurnContinuationKind.JailDoubleLanding,
                player.Id,
                roll,
                landedSquare.Position,
                true);
            PublishNotification(new SpaceReachedNotification(landedSquare));
            landedSquare.LandOn(player, this);
            if (PendingDecision is not null)
                return GameActionResult.DecisionRequired(PendingDecision);

            return CompleteTurnContinuation(player);
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
                return CompleteAction(BuildResult(player, roll, null, false, false, false, true));
            }
        }

        AdvanceToNextActivePlayer();
        return CompleteAction(BuildResult(player, roll, null, false, false, false));
    }

    private GameActionResult CompleteTurnContinuation(Player? knownPlayer = null)
    {
        TurnContinuation continuation = _turnContinuation
            ?? throw new InvalidOperationException("There is no turn continuation to complete.");
        _turnContinuation = null;

        Player player = knownPlayer ?? _players.Single(candidate => candidate.Id == continuation.PlayerId);
        Square landedSquare = Board.GetSquareAtPosition(continuation.LandedSquarePosition);

        if (continuation.Kind == TurnContinuationKind.JailDoubleLanding)
        {
            ConsecutiveDoubles = 0;
            AdvanceToNextActivePlayer();
            return CompleteAction(BuildResult(
                player,
                continuation.Roll,
                landedSquare,
                false,
                continuation.WasReleasedFromJailByDouble,
                false));
        }

        bool bankrupt = player.IsBankrupt;
        bool sentToJail = !bankrupt && TheJail.IsPlayerInJail(player);
        if (!bankrupt && (sentToJail || !continuation.Roll.IsDouble))
        {
            ConsecutiveDoubles = 0;
            AdvanceToNextActivePlayer();
        }
        else if (!bankrupt)
        {
            ConsecutiveDoubles++;
            CurrentTurn++;
        }

        return CompleteAction(BuildResult(
            player,
            continuation.Roll,
            landedSquare,
            sentToJail,
            false,
            continuation.Roll.IsDouble && !bankrupt && !sentToJail));
    }

    private GameActionResult CompleteAction(TurnResult result)
    {
        if (result.GameOver)
        {
            Phase = GamePhase.GameOver;
            _notifications.Complete();
            return GameActionResult.Over(result);
        }

        Phase = GamePhase.ReadyForTurn;
        return GameActionResult.Completed(result);
    }

    private TurnResult BuildResult(
        Player player,
        DiceRoll? roll,
        Square? landedSquare,
        bool wasSentToJail,
        bool wasReleasedFromJailByDouble,
        bool extraTurn,
        bool playerBankrupt = false)
    {
        return new TurnResult
        {
            Player = player,
            Roll = roll,
            LandedSquare = landedSquare,
            WasSentToJail = wasSentToJail,
            WasReleasedFromJailByDouble = wasReleasedFromJailByDouble,
            ExtraTurn = extraTurn,
            PlayerBankrupt = playerBankrupt || player.IsBankrupt,
            GameOver = IsGameOver,
            Winner = Winner
        };
    }

    internal void CommitDiceRoll(DiceRoll roll)
    {
        LastDiceRoll = roll ?? throw new ArgumentNullException(nameof(roll));
    }

    internal Square MovePlayerBySteps(Player player, int steps)
    {
        Handler.MovePlayerAndInvokeEvent(player, player.Position + steps);
        return Board.GetSquareAtPosition(player.Position);
    }

    internal void PublishNotification(GameNotification notification)
    {
        _notificationDispatchDepth++;
        try
        {
            _notifications.Publish(notification);
        }
        finally
        {
            _notificationDispatchDepth--;
        }
    }

    internal void NextPlayer() => AdvanceToNextActivePlayer();

    private void EnsureNotificationIsNotBeingPublished()
    {
        if (_notificationDispatchDepth > 0)
            throw new InvalidOperationException("Authoritative operations cannot start while a presentation notification is being delivered.");
    }

    private void AdvanceToNextActivePlayer()
    {
        if (Players.Count == 0)
        {
            Winner = null;
            Phase = GamePhase.GameOver;
            return;
        }

        List<Player> activePlayers = Players.Where(p => !p.IsBankrupt).ToList();
        if (activePlayers.Count <= 1)
        {
            Winner = activePlayers.SingleOrDefault();
            if (Winner is not null && !ReferenceEquals(CurrentPlayer, Winner))
                TransitionToPlayer(Winner);
            Phase = GamePhase.GameOver;
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
            Phase = GamePhase.GameOver;
            return;
        }

        List<Player> activePlayers = Players.Where(candidate => !candidate.IsBankrupt).ToList();
        Winner = activePlayers.Count == 1 ? activePlayers[0] : null;

        if (Winner is not null)
        {
            if (!ReferenceEquals(CurrentPlayer, Winner))
                TransitionToPlayer(Winner);
            Phase = GamePhase.GameOver;
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
        Phase = winner is null ? GamePhase.ReadyForTurn : GamePhase.GameOver;
    }

    internal void ResetProgressForVersionOne()
    {
        PendingDecision = null;
        _turnContinuation = null;
        _consumedDecisionIds.Clear();
        _lastConsumedDecisionId = null;
        Phase = GamePhase.ReadyForTurn;
    }

    internal void ValidateAuthoritativeState()
    {
        if (_players.Count == 0 || _players.Count > Rules.NumberOfPlayers)
            throw new InvalidOperationException("The active match roster is inconsistent with the configured player count.");
        if (!ContainsPlayer(CurrentPlayer) || CurrentPlayer.IsBankrupt)
            throw new InvalidOperationException("The current player must be active and belong to the game.");
        if (Board.Squares.Select(square => square.Position).Distinct().Count() != Board.Squares.Count)
            throw new InvalidOperationException("Board positions must be unique.");
        if (Board.Track.Count != Board.Squares.Count ||
            Board.Squares.Where((square, index) => square.Id != Board.Track.GetSpaceIdAt(index)).Any())
        {
            throw new InvalidOperationException("The board spaces must match the authoritative track order.");
        }
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

}
