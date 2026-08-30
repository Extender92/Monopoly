using System.Collections.ObjectModel;
using Monopoly.Core.Interface;
using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Notifications;
using Monopoly.Core.Presentation;
using Monopoly.Core.Randomness;

namespace Monopoly.Core;

public sealed class Game : IGame
{
    private readonly List<Player> _players;
    private readonly ReadOnlyCollection<Player> _playersView;
    private readonly Dictionary<SpaceId, int?> _ownership;
    private readonly HashSet<Guid> _consumedDecisionIds = [];
    private readonly GameNotificationHub _notifications = new();
    private readonly ILogHandler _logs;
    private readonly ProfileComponentRegistry _registry;
    private readonly int _roundAnchorPlayerId;
    private TurnContinuation? _turnContinuation;
    private Guid? _lastConsumedDecisionId;
    private int _notificationDispatchDepth;

    internal Game(
        ValidatedGameProfile profile,
        IEnumerable<Player> players,
        Player currentPlayer,
        GameBoard board,
        DeckRuntime decks,
        MatchRandomizer randomizer,
        ProfileComponentRegistry registry,
        IEnumerable<SpaceId> ownableSpaceIds,
        ILogHandler logs)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        ArgumentNullException.ThrowIfNull(players);
        CurrentPlayer = currentPlayer ?? throw new ArgumentNullException(nameof(currentPlayer));
        Board = board ?? throw new ArgumentNullException(nameof(board));
        DeckRuntime = decks ?? throw new ArgumentNullException(nameof(decks));
        Randomizer = randomizer ?? throw new ArgumentNullException(nameof(randomizer));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logs = logs ?? throw new ArgumentNullException(nameof(logs));
        ArgumentNullException.ThrowIfNull(ownableSpaceIds);

        _players = players.ToList();
        if (_players.Count < profile.Setup.MinimumPlayers || _players.Count > profile.Setup.MaximumPlayers ||
            _players.Any(player => player is null))
        {
            throw new ArgumentException("The profile match roster is invalid.", nameof(players));
        }
        if (_players.Select(player => player.Id).Distinct().Count() != _players.Count)
            throw new ArgumentException("Player IDs must be unique.", nameof(players));
        if (!_players.Any(player => ReferenceEquals(player, currentPlayer)))
            throw new ArgumentException("The current player must belong to the game.", nameof(currentPlayer));

        _playersView = _players.AsReadOnly();
        _roundAnchorPlayerId = currentPlayer.Id;
        _ownership = ownableSpaceIds
            .Distinct()
            .OrderBy(id => id)
            .ToDictionary(id => id, _ => (int?)null);
        Presentation = profile.Presentation;
        Statuses = new StatusCollection([]);
        RoundNumber = 1;
        Phase = GamePhase.ReadyForTurn;

        if (_logs is LogHandler logHandler)
            logHandler.OwnerGame = this;

        ValidateAuthoritativeState();
    }

    public IGameLog Logs => _logs;
    public IGameNotificationSource Notifications => _notifications;
    public GameBoard Board { get; }
    internal DeckRuntime DeckRuntime { get; }
    public DeckCollection Decks => DeckRuntime.CreateSnapshot();
    public IReadOnlyList<Player> Players => _playersView;
    public Player CurrentPlayer { get; private set; }
    public DiceRoll? LastDiceRoll { get; private set; }
    internal MatchRandomizer Randomizer { get; }
    public ProfilePresentation Presentation { get; }
    public ValidatedGameProfile Profile { get; }
    public StatusCollection Statuses { get; }
    public OwnershipCollection Ownership => new(_ownership.Select(entry => new SpaceOwnershipView(entry.Key, entry.Value)));
    public ProfileModuleState ModuleState => new(Ownership, Statuses);
    public int RoundNumber { get; private set; }
    public Player? Winner { get; private set; }
    public bool IsGameOver => Winner is not null;
    public GamePhase Phase { get; private set; }
    public PendingDecision? PendingDecision { get; private set; }
    internal TurnContinuation? TurnContinuationSnapshot => _turnContinuation;
    internal Guid? LastConsumedDecisionId => _lastConsumedDecisionId;
    internal IReadOnlyCollection<Guid> ConsumedDecisionIds => _consumedDecisionIds;
    internal int RoundAnchorPlayerId => _roundAnchorPlayerId;
    internal ProfileComponentRegistry Registry => _registry;
    internal int NotificationSubscriberCount => _notifications.SubscriberCount;

    public GameActionResult PlayTurn()
    {
        if (_notificationDispatchDepth > 0)
            return GameActionResult.Rejected(GameActionRejectionReason.OperationInProgress, PendingDecision);
        ValidateAuthoritativeState();
        if (Phase == GamePhase.AwaitingDecision)
            return GameActionResult.Rejected(GameActionRejectionReason.PendingDecisionRequired, PendingDecision);
        if (Phase == GamePhase.GameOver)
        {
            DiceRoll roll = LastDiceRoll ?? throw new InvalidOperationException("A completed match must retain its final dice outcome.");
            return GameActionResult.Over(BuildResult(CurrentPlayer, roll, Winner));
        }

        DiceRoll preparedRoll = PrepareTurnRoll();
        ExecutionTransition transition = new(this);
        transition.LastDiceRoll = preparedRoll;
        ProfileExecutionContext context = new(this, transition, CurrentPlayer.Id, preparedRoll);
        context.MoveByOffset(preparedRoll.Sum, applyOriginReward: true, "turn.roll");
        context.ResolveLanding(context.CurrentSpaceId, 0);

        if (transition.PendingDecision is not null)
        {
            transition.Phase = GamePhase.AwaitingDecision;
            Commit(transition);
            return GameActionResult.DecisionRequired(PendingDecision!);
        }

        context.CompleteTurn();
        Commit(transition);
        TurnResult result = BuildResult(CurrentPlayerById(context.ActorPlayerId), preparedRoll, Winner);
        return IsGameOver ? GameActionResult.Over(result) : GameActionResult.Completed(result);
    }

    public GameActionResult SubmitDecision(DecisionResponse? response)
    {
        if (_notificationDispatchDepth > 0)
            return GameActionResult.Rejected(GameActionRejectionReason.OperationInProgress, PendingDecision);
        ValidateAuthoritativeState();
        if (response is null || response.DecisionId == Guid.Empty || response.PlayerId < 0 || !response.Response.IsValid)
            return GameActionResult.Rejected(GameActionRejectionReason.MalformedResponse, PendingDecision);

        if (PendingDecision is null)
        {
            GameActionRejectionReason missingReason = response.DecisionId == _lastConsumedDecisionId
                ? GameActionRejectionReason.DuplicateDecision
                : _consumedDecisionIds.Contains(response.DecisionId)
                    ? GameActionRejectionReason.StaleDecision
                    : GameActionRejectionReason.NoPendingDecision;
            return GameActionResult.Rejected(missingReason);
        }

        if (response.DecisionId != PendingDecision.DecisionId)
            return GameActionResult.Rejected(GameActionRejectionReason.StaleDecision, PendingDecision);
        if (response.PlayerId != PendingDecision.PlayerId)
            return GameActionResult.Rejected(GameActionRejectionReason.WrongPlayer, PendingDecision);
        if (!PendingDecision.AllowedResponses.Contains(response.Response))
            return GameActionResult.Rejected(GameActionRejectionReason.ResponseNotAllowed, PendingDecision);
        if (PendingDecision is not PurchaseDecision purchase || _turnContinuation is null)
            return GameActionResult.Rejected(GameActionRejectionReason.ResponseNotAllowed, PendingDecision);

        SpaceDefinition space = Board.GetDefinition(purchase.SpaceId);
        PurchasableCapabilityDefinition? currentPurchase = space.Capabilities.Find<PurchasableCapabilityDefinition>();
        if (currentPurchase is null || currentPurchase.Price != purchase.Price ||
            !_ownership.TryGetValue(purchase.SpaceId, out int? owner) || owner is not null)
        {
            return GameActionResult.Rejected(GameActionRejectionReason.ResponseNotAllowed, PendingDecision);
        }
        if (response.Response == DecisionOptions.Accept &&
            CurrentPlayer.Resources[purchase.Price.ResourceId] < purchase.Price.Value)
        {
            return GameActionResult.Rejected(GameActionRejectionReason.ResponseNotAllowed, PendingDecision);
        }

        TurnContinuation continuation = _turnContinuation;
        ExecutionTransition transition = new(this);
        transition.PendingDecision = null;
        transition.Continuation = null;
        transition.Phase = GamePhase.ReadyForTurn;
        transition.ConsumedDecisionIds.Add(purchase.DecisionId);
        transition.LastConsumedDecisionId = purchase.DecisionId;

        ProfileExecutionContext context = new(this, transition, purchase.PlayerId, continuation.Roll);
        if (response.Response == DecisionOptions.Accept)
            context.ApplyPurchase(purchase);
        else
            _registry.ExecutePurchaseDecline(context, Profile.Policies.PurchaseDecline);
        context.ResolveLanding(continuation.SpaceId, continuation.NextCapabilityIndex);

        if (transition.PendingDecision is not null)
            throw new ProfileExecutionException(ProfileExecutionErrorKind.InvalidRuntimeState, "decision.continuation", "The baseline cannot request a second purchase while resuming one landing.");

        context.CompleteTurn();
        Commit(transition);
        Player actor = CurrentPlayerById(context.ActorPlayerId);
        TurnResult result = BuildResult(actor, continuation.Roll, Winner);
        return IsGameOver ? GameActionResult.Over(result) : GameActionResult.Completed(result);
    }

    private DiceRoll PrepareTurnRoll()
    {
        int[] values = new int[Profile.Setup.DiceCount];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = Randomizer.NextInt(new RandomRequest(
                RandomPurpose.TurnDice,
                1,
                checked(Profile.Setup.DieSides + 1),
                index));
        }
        return new DiceRoll(RandomPurpose.TurnDice, values, Profile.Setup.DieSides);
    }

    private TurnResult BuildResult(Player actor, DiceRoll roll, Player? winner) => new()
    {
        Player = actor,
        Roll = roll,
        LandedSpace = Board.GetSpace(actor.CurrentSpaceId),
        GameOver = winner is not null,
        Winner = winner
    };

    private void Commit(ExecutionTransition transition)
    {
        ValidatePreparedTransition(transition);

        foreach (PreparedPlayerState state in transition.Players.Values)
            CurrentPlayerById(state.PlayerId).ApplyState(state.Resources, state.SpaceId, state.Position);

        _ownership.Clear();
        foreach ((SpaceId id, int? ownerId) in transition.Ownership.OrderBy(entry => entry.Key))
            _ownership.Add(id, ownerId);
        DeckRuntime.ApplyOrders(transition.DeckOrders);

        CurrentPlayer = CurrentPlayerById(transition.CurrentPlayerId);
        RoundNumber = transition.RoundNumber;
        Winner = transition.WinnerPlayerId is int winnerId ? CurrentPlayerById(winnerId) : null;
        LastDiceRoll = transition.LastDiceRoll;
        PendingDecision = transition.PendingDecision;
        _turnContinuation = transition.Continuation;
        Phase = transition.Phase;
        _consumedDecisionIds.Clear();
        _consumedDecisionIds.UnionWith(transition.ConsumedDecisionIds);
        _lastConsumedDecisionId = transition.LastConsumedDecisionId;

        ValidateAuthoritativeState();
        foreach (GameNotification notification in transition.Notifications)
            PublishNotification(notification);
        if (Phase == GamePhase.GameOver)
            _notifications.Complete();
    }

    private void ValidatePreparedTransition(ExecutionTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        HashSet<int> playerIds = _players.Select(player => player.Id).ToHashSet();
        if (!playerIds.SetEquals(transition.Players.Keys))
            throw PreparedStateError("transition.players", "The prepared player set does not match the active match.");

        HashSet<ResourceId> resourceIds = Profile.RuleGraph.Resources.ToHashSet();
        foreach ((int playerId, PreparedPlayerState player) in transition.Players)
        {
            if (player.PlayerId != playerId ||
                !resourceIds.SetEquals(player.Resources.Keys) ||
                player.Resources.Values.Any(value => value < 0) ||
                player.Position < 0 ||
                player.Position >= Board.Track.Count ||
                player.SpaceId != Board.Track.GetSpaceIdAt(player.Position))
            {
                throw PreparedStateError($"transition.players[{playerId}]", "The prepared player state is inconsistent with the validated profile.");
            }
        }

        HashSet<SpaceId> ownableSpaceIds = Profile.RuleGraph.Spaces
            .Where(space => space.Capabilities.Contains(CapabilityKinds.Ownable))
            .Select(space => space.Id)
            .ToHashSet();
        if (!ownableSpaceIds.SetEquals(transition.Ownership.Keys) ||
            transition.Ownership.Values.Any(ownerId => ownerId is int value && !playerIds.Contains(value)))
        {
            throw PreparedStateError("transition.ownership", "The prepared ownership state is inconsistent with the validated profile.");
        }

        try
        {
            DeckRuntime.ValidateOrders(transition.DeckOrders);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new ProfileExecutionException(
                ProfileExecutionErrorKind.InvalidRuntimeState,
                "transition.decks",
                "The prepared deck state is inconsistent with the validated profile.",
                exception);
        }

        if (!playerIds.Contains(transition.CurrentPlayerId))
            throw PreparedStateError("transition.currentPlayerId", "The prepared current player does not belong to the match.");
        if (transition.RoundNumber < 1 || transition.RoundNumber > Profile.Policies.MatchEnd.RoundLimit)
            throw PreparedStateError("transition.roundNumber", "The prepared round is outside the profile match limit.");
        if (transition.ConsumedDecisionIds.Contains(Guid.Empty) ||
            transition.LastConsumedDecisionId is Guid lastConsumed && !transition.ConsumedDecisionIds.Contains(lastConsumed))
        {
            throw PreparedStateError("transition.consumedDecisions", "The prepared consumed-decision state is inconsistent.");
        }

        bool awaitingDecision = transition.PendingDecision is not null && transition.Continuation is not null;
        if ((transition.Phase == GamePhase.AwaitingDecision) != awaitingDecision)
            throw PreparedStateError("transition.phase", "The prepared phase, decision and continuation do not agree.");
        if ((transition.Phase == GamePhase.GameOver) != (transition.WinnerPlayerId is not null))
            throw PreparedStateError("transition.winner", "The prepared terminal phase and winner do not agree.");
        if (transition.WinnerPlayerId is int winnerId && !playerIds.Contains(winnerId))
            throw PreparedStateError("transition.winnerPlayerId", "The prepared winner does not belong to the match.");

        if (transition.PendingDecision is PurchaseDecision decision && transition.Continuation is TurnContinuation continuation)
        {
            if (decision.PlayerId != continuation.PlayerId ||
                transition.CurrentPlayerId != decision.PlayerId ||
                !transition.Players.TryGetValue(decision.PlayerId, out PreparedPlayerState? actor) ||
                actor.SpaceId != decision.SpaceId ||
                continuation.SpaceId != decision.SpaceId ||
                !ReferenceEquals(continuation.Roll, transition.LastDiceRoll))
            {
                throw PreparedStateError("transition.pendingDecision", "The prepared decision cannot resume the current turn.");
            }
        }
        else if (awaitingDecision)
        {
            throw PreparedStateError("transition.pendingDecision", "The prepared decision type is not supported by the execution baseline.");
        }
    }

    private static ProfileExecutionException PreparedStateError(string path, string message) =>
        new(ProfileExecutionErrorKind.InvalidRuntimeState, path, message);

    internal Player CurrentPlayerById(int playerId) =>
        _players.Single(player => player.Id == playerId);

    internal PresentationToken ResourcePresentationToken(ResourceId resourceId) =>
        Profile.Resources.Single(resource => resource.Id == resourceId).PresentationToken;

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

    internal void ValidateAuthoritativeState()
    {
        if (_players.Count < Profile.Setup.MinimumPlayers || _players.Count > Profile.Setup.MaximumPlayers)
            throw new InvalidOperationException("The active match roster is inconsistent with the profile player range.");
        if (!_players.Contains(CurrentPlayer))
            throw new InvalidOperationException("The current player must belong to the profile match.");
        if (Board.Track.Count != Profile.RuleGraph.Track.Count ||
            Board.Spaces.Where((space, index) => space.Id != Profile.RuleGraph.Track.GetSpaceIdAt(index)).Any())
        {
            throw new InvalidOperationException("The runtime track does not match the validated profile.");
        }

        HashSet<ResourceId> resourceIds = Profile.RuleGraph.Resources.ToHashSet();
        foreach (Player player in _players)
        {
            if (!resourceIds.SetEquals(player.Resources.Keys) || player.Resources.Values.Any(value => value < 0))
                throw new InvalidOperationException("A player's resources do not match the validated profile.");
            if (player.Position < 0 || player.Position >= Board.Track.Count ||
                player.CurrentSpaceId != Board.Track.GetSpaceIdAt(player.Position))
            {
                throw new InvalidOperationException("A player's position and space ID are inconsistent.");
            }
        }

        HashSet<SpaceId> expectedOwnable = Profile.RuleGraph.Spaces
            .Where(space => space.Capabilities.Contains(CapabilityKinds.Ownable))
            .Select(space => space.Id)
            .ToHashSet();
        if (!expectedOwnable.SetEquals(_ownership.Keys))
            throw new InvalidOperationException("The ownership module does not match the validated profile.");
        if (_ownership.Values.Any(ownerId => ownerId is int value && _players.All(player => player.Id != value)))
            throw new InvalidOperationException("Every owner must belong to the profile match.");
        if (Statuses.Count != 0)
            throw new InvalidOperationException("The public baseline does not support runtime statuses.");
        if (RoundNumber < 1 || RoundNumber > Profile.Policies.MatchEnd.RoundLimit)
            throw new InvalidOperationException("The profile match round number is inconsistent.");
        if ((Phase == GamePhase.AwaitingDecision) != (PendingDecision is not null && _turnContinuation is not null))
            throw new InvalidOperationException("The pending decision and continuation state is inconsistent.");
        if ((Phase == GamePhase.GameOver) != (Winner is not null))
            throw new InvalidOperationException("The terminal phase and winner state is inconsistent.");
    }
}

internal sealed class PreparedPlayerState
{
    internal PreparedPlayerState(Player player)
    {
        PlayerId = player.Id;
        Resources = player.Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
        Position = player.Position;
        SpaceId = player.CurrentSpaceId;
    }

    internal int PlayerId { get; }
    internal Dictionary<ResourceId, int> Resources { get; }
    internal int Position { get; set; }
    internal SpaceId SpaceId { get; set; }
}

internal sealed class ExecutionTransition
{
    internal ExecutionTransition(Game game)
    {
        Players = game.Players.ToDictionary(player => player.Id, player => new PreparedPlayerState(player));
        Ownership = game.Ownership.Entries.ToDictionary(entry => entry.SpaceId, entry => entry.OwnerPlayerId);
        DeckOrders = game.DeckRuntime.CaptureOrders();
        CurrentPlayerId = game.CurrentPlayer.Id;
        RoundNumber = game.RoundNumber;
        WinnerPlayerId = game.Winner?.Id;
        LastDiceRoll = game.LastDiceRoll;
        PendingDecision = game.PendingDecision;
        Continuation = game.TurnContinuationSnapshot;
        Phase = game.Phase;
        ConsumedDecisionIds = game.ConsumedDecisionIds.ToHashSet();
        LastConsumedDecisionId = game.LastConsumedDecisionId;
    }

    internal Dictionary<int, PreparedPlayerState> Players { get; }
    internal Dictionary<SpaceId, int?> Ownership { get; }
    internal Dictionary<DeckId, List<CardDefinition>> DeckOrders { get; }
    internal List<GameNotification> Notifications { get; } = [];
    internal int CurrentPlayerId { get; set; }
    internal int RoundNumber { get; set; }
    internal int? WinnerPlayerId { get; set; }
    internal DiceRoll? LastDiceRoll { get; set; }
    internal PendingDecision? PendingDecision { get; set; }
    internal TurnContinuation? Continuation { get; set; }
    internal GamePhase Phase { get; set; }
    internal HashSet<Guid> ConsumedDecisionIds { get; }
    internal Guid? LastConsumedDecisionId { get; set; }
}
