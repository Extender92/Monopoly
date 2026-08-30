using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Randomness;

namespace Monopoly.Core.Persistence;

/// <summary>Captures and reconstructs the complete supported authoritative match state.</summary>
public static class GameStateV2Mapper
{
    public static GameStateV2 Capture(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        game.ValidateAuthoritativeState();

        PendingDecisionStateV2? decision = game.PendingDecision switch
        {
            null => null,
            PurchaseDecision purchase => new PendingDecisionStateV2(
                purchase.DecisionId,
                purchase.Kind,
                purchase.PlayerId,
                purchase.AllowedResponses,
                purchase.SpaceId,
                purchase.Price.ResourceId,
                purchase.Price.Value),
            _ => throw Error(
                GameStateValidationErrorKind.InvalidValue,
                "pendingDecision",
                "The active pending decision is not supported by Save Format Version 2.")
        };

        TurnContinuationStateV2? continuation = game.TurnContinuationSnapshot is TurnContinuation state
            ? new TurnContinuationStateV2(state.PlayerId, state.SpaceId, state.NextCapabilityIndex)
            : null;

        return new GameStateV2(
            GameSaveFormat.Version2,
            game.Profile.Id,
            game.Profile.Revision,
            game.Profile.Fingerprint,
            game.Players.Select(player => new PlayerStateV2(
                player.Id,
                player.Name,
                player.CurrentSpaceId,
                player.Resources
                    .OrderBy(entry => entry.Key)
                    .Select(entry => new ResourceBalanceStateV2(entry.Key, entry.Value)))),
            game.CurrentPlayer.Id,
            game.RoundAnchorPlayerId,
            game.RoundNumber,
            game.Phase,
            game.LastDiceRoll is DiceRoll roll ? new DiceRollStateV2(roll.Purpose, roll.Results) : null,
            game.Winner?.Id,
            game.Decks.Entries
                .OrderBy(deck => deck.Id)
                .Select(deck => new DeckStateV2(deck.Id, deck.Cards.Select(card => card.Id))),
            new ModuleStateV2(
                GameSaveFormat.OwnershipModuleVersion,
                game.Ownership.Entries.Select(entry => new OwnershipStateV2(entry.SpaceId, entry.OwnerPlayerId)),
                GameSaveFormat.StatusModuleVersion,
                game.Statuses.Entries.Select(entry => new PlayerStatusStateV2(
                    entry.PlayerId,
                    entry.Status.Id,
                    entry.Status.Value))),
            decision,
            continuation,
            game.ConsumedDecisionIds.OrderBy(id => id),
            game.LastConsumedDecisionId);
    }

    public static Game Restore(
        GameStateV2 state,
        ValidatedGameProfile profile,
        IMatchRandomSource? randomSource = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(profile);

        ValidateFormatAndProfile(state, profile);
        ProfileComponentRegistry registry = ProfileComponentRegistry.CreateExecutionBaseline();
        try
        {
            GameSetup.ValidateCompatibility(profile, registry);
        }
        catch (GameSetupException exception)
        {
            throw Error(
                GameStateValidationErrorKind.InvalidValue,
                "profile",
                "The registered profile is not compatible with the supported execution baseline.",
                exception);
        }

        Player[] players = ValidateAndCreatePlayers(state, profile);
        HashSet<int> playerIds = players.Select(player => player.Id).ToHashSet();
        EnsurePlayerReference(state.CurrentPlayerId, playerIds, "currentPlayerId");
        EnsurePlayerReference(state.RoundAnchorPlayerId, playerIds, "roundAnchorPlayerId");
        if (state.WinnerPlayerId is int winnerId)
            EnsurePlayerReference(winnerId, playerIds, "winnerPlayerId");

        Ensure(
            state.RoundNumber >= 1 && state.RoundNumber <= profile.Policies.MatchEnd.RoundLimit,
            GameStateValidationErrorKind.InvalidValue,
            "roundNumber",
            "The saved round number is outside the profile match limit.");
        Ensure(
            Enum.IsDefined(state.Phase),
            GameStateValidationErrorKind.InvalidValue,
            "phase",
            "The saved phase is invalid.");

        DiceRoll? roll = ValidateDiceRoll(state.LastDiceRoll, profile);
        Dictionary<DeckId, List<CardDefinition>> deckOrders = ValidateDecks(state.Decks, profile);
        Dictionary<SpaceId, int?> ownership = ValidateModules(state.ModuleState, profile, playerIds);
        HashSet<Guid> consumed = ValidateConsumedDecisions(state);

        PendingDecision? pendingDecision = null;
        TurnContinuation? continuation = null;
        ValidatePhaseAndTerminalState(state, profile, players, roll);
        if (state.Phase == GamePhase.AwaitingDecision)
        {
            (pendingDecision, continuation) = ValidateDecisionState(
                state,
                profile,
                players,
                ownership,
                consumed,
                roll!);
        }
        else
        {
            Ensure(
                state.PendingDecision is null && state.Continuation is null,
                GameStateValidationErrorKind.InconsistentState,
                "phase",
                "Only an awaiting-decision phase can contain a pending decision and continuation.");
        }

        MatchRandomizer randomizer = new(randomSource ?? new SystemMatchRandomSource());
        DeckRuntime decks = DeckRuntime.CreateForProfile(profile.RuleGraph.Decks, randomizer, shuffleDecks: false);
        try
        {
            decks.ApplyOrders(deckOrders);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw Error(
                GameStateValidationErrorKind.InconsistentState,
                "decks",
                "The saved deck state is inconsistent with the registered profile.",
                exception);
        }

        return Game.RestoreValidatedState(
            profile,
            players,
            state.CurrentPlayerId,
            state.RoundAnchorPlayerId,
            new GameBoard(profile.RuleGraph.Spaces),
            decks,
            randomizer,
            registry,
            ownership,
            state.RoundNumber,
            roll,
            state.WinnerPlayerId,
            state.Phase,
            pendingDecision,
            continuation,
            consumed,
            state.LastConsumedDecisionId);
    }

    private static void ValidateFormatAndProfile(GameStateV2 state, ValidatedGameProfile profile)
    {
        Ensure(
            state.FormatVersion == GameSaveFormat.Version2,
            GameStateValidationErrorKind.InvalidValue,
            "formatVersion",
            "The game state is not Save Format Version 2.");
        Ensure(
            state.ProfileId.IsValid && state.ProfileRevision.IsValid && state.ProfileFingerprint.IsValid,
            GameStateValidationErrorKind.InvalidValue,
            "profile",
            "The saved profile identity is invalid.");
        Ensure(
            state.ProfileId == profile.Id && state.ProfileRevision == profile.Revision && state.ProfileFingerprint == profile.Fingerprint,
            GameStateValidationErrorKind.InconsistentState,
            "profile",
            "The supplied profile does not exactly match the saved profile identity.");
    }

    private static Player[] ValidateAndCreatePlayers(GameStateV2 state, ValidatedGameProfile profile)
    {
        Ensure(
            state.Players.Count >= profile.Setup.MinimumPlayers && state.Players.Count <= profile.Setup.MaximumPlayers,
            GameStateValidationErrorKind.InvalidValue,
            "players",
            "The saved player count is outside the profile range.");

        HashSet<int> playerIds = [];
        HashSet<ResourceId> expectedResources = profile.RuleGraph.Resources.ToHashSet();
        Player[] players = new Player[state.Players.Count];
        for (int index = 0; index < state.Players.Count; index++)
        {
            PlayerStateV2 saved = state.Players[index];
            string path = $"players[{index}]";
            Ensure(saved.PlayerId >= 0, GameStateValidationErrorKind.InvalidValue, $"{path}.playerId", "Player IDs must be non-negative.");
            Ensure(playerIds.Add(saved.PlayerId), GameStateValidationErrorKind.DuplicateEntry, $"{path}.playerId", "Player IDs must be unique.");
            Ensure(!string.IsNullOrWhiteSpace(saved.Name), GameStateValidationErrorKind.InvalidValue, $"{path}.name", "Player names cannot be blank.");

            int position;
            try
            {
                position = profile.RuleGraph.Track.GetIndex(saved.SpaceId);
            }
            catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
            {
                throw Error(GameStateValidationErrorKind.BrokenReference, $"{path}.spaceId", "The player's space does not belong to the profile track.", exception);
            }

            Dictionary<ResourceId, int> resources = [];
            for (int resourceIndex = 0; resourceIndex < saved.Resources.Count; resourceIndex++)
            {
                ResourceBalanceStateV2 resource = saved.Resources[resourceIndex];
                string resourcePath = $"{path}.resources[{resourceIndex}]";
                Ensure(resource.ResourceId.IsValid, GameStateValidationErrorKind.InvalidValue, $"{resourcePath}.resourceId", "The resource ID is invalid.");
                Ensure(expectedResources.Contains(resource.ResourceId), GameStateValidationErrorKind.BrokenReference, $"{resourcePath}.resourceId", "The resource is not declared by the profile.");
                Ensure(resources.TryAdd(resource.ResourceId, resource.Value), GameStateValidationErrorKind.DuplicateEntry, $"{resourcePath}.resourceId", "The player resource is duplicated.");
                Ensure(resource.Value >= 0, GameStateValidationErrorKind.InvalidValue, $"{resourcePath}.value", "Resource balances cannot be negative.");
            }
            Ensure(expectedResources.SetEquals(resources.Keys), GameStateValidationErrorKind.InconsistentState, $"{path}.resources", "The player must contain every profile resource exactly once.");

            Player player = new(saved.Name, saved.PlayerId);
            player.ApplyState(resources, saved.SpaceId, position);
            players[index] = player;
        }

        return players;
    }

    private static Dictionary<DeckId, List<CardDefinition>> ValidateDecks(
        IReadOnlyList<DeckStateV2> savedDecks,
        ValidatedGameProfile profile)
    {
        Dictionary<DeckId, DeckDefinition> definitions = profile.RuleGraph.Decks.ToDictionary(deck => deck.Id);
        Dictionary<DeckId, List<CardDefinition>> orders = [];
        for (int index = 0; index < savedDecks.Count; index++)
        {
            DeckStateV2 saved = savedDecks[index];
            string path = $"decks[{index}]";
            Ensure(saved.DeckId.IsValid, GameStateValidationErrorKind.InvalidValue, $"{path}.deckId", "The deck ID is invalid.");
            Ensure(definitions.TryGetValue(saved.DeckId, out DeckDefinition? definition), GameStateValidationErrorKind.BrokenReference, $"{path}.deckId", "The deck is not declared by the profile.");
            Ensure(!orders.ContainsKey(saved.DeckId), GameStateValidationErrorKind.DuplicateEntry, $"{path}.deckId", "The deck is duplicated.");

            Dictionary<CardId, CardDefinition> cards = definition!.Cards.ToDictionary(card => card.Id);
            List<CardDefinition> order = [];
            HashSet<CardId> seen = [];
            for (int cardIndex = 0; cardIndex < saved.CardIds.Count; cardIndex++)
            {
                CardId cardId = saved.CardIds[cardIndex];
                Ensure(cardId.IsValid, GameStateValidationErrorKind.InvalidValue, $"{path}.cardIds[{cardIndex}]", "The card ID is invalid.");
                Ensure(seen.Add(cardId), GameStateValidationErrorKind.DuplicateEntry, $"{path}.cardIds[{cardIndex}]", "A card is duplicated in the saved deck.");
                Ensure(cards.TryGetValue(cardId, out CardDefinition? card), GameStateValidationErrorKind.BrokenReference, $"{path}.cardIds[{cardIndex}]", "The card does not belong to the saved deck.");
                order.Add(card!);
            }
            Ensure(cards.Keys.ToHashSet().SetEquals(seen), GameStateValidationErrorKind.InconsistentState, $"{path}.cardIds", "The saved deck must contain every declared card exactly once.");
            orders.Add(saved.DeckId, order);
        }
        Ensure(definitions.Keys.ToHashSet().SetEquals(orders.Keys), GameStateValidationErrorKind.InconsistentState, "decks", "The save must contain every profile deck exactly once.");
        return orders;
    }

    private static Dictionary<SpaceId, int?> ValidateModules(
        ModuleStateV2 modules,
        ValidatedGameProfile profile,
        HashSet<int> playerIds)
    {
        Ensure(
            modules.OwnershipVersion == GameSaveFormat.OwnershipModuleVersion,
            GameStateValidationErrorKind.UnsupportedModuleVersion,
            "moduleState.ownership.version",
            "The ownership module version is not supported.");
        Ensure(
            modules.StatusVersion == GameSaveFormat.StatusModuleVersion,
            GameStateValidationErrorKind.UnsupportedModuleVersion,
            "moduleState.statuses.version",
            "The status module version is not supported.");
        Ensure(
            modules.Statuses.Count == 0,
            GameStateValidationErrorKind.InvalidValue,
            "moduleState.statuses.entries",
            "Runtime statuses are not supported by the current public capability baseline.");

        HashSet<SpaceId> expectedOwnable = profile.RuleGraph.Spaces
            .Where(space => space.Capabilities.Contains(CapabilityKinds.Ownable))
            .Select(space => space.Id)
            .ToHashSet();
        Dictionary<SpaceId, int?> ownership = [];
        for (int index = 0; index < modules.Ownership.Count; index++)
        {
            OwnershipStateV2 entry = modules.Ownership[index];
            string path = $"moduleState.ownership.entries[{index}]";
            Ensure(expectedOwnable.Contains(entry.SpaceId), GameStateValidationErrorKind.BrokenReference, $"{path}.spaceId", "The ownership space is not an ownable profile space.");
            Ensure(ownership.TryAdd(entry.SpaceId, entry.OwnerPlayerId), GameStateValidationErrorKind.DuplicateEntry, $"{path}.spaceId", "Ownership is duplicated for a space.");
            Ensure(entry.OwnerPlayerId is null || playerIds.Contains(entry.OwnerPlayerId.Value), GameStateValidationErrorKind.BrokenReference, $"{path}.ownerPlayerId", "The owner does not belong to the match.");
        }
        Ensure(expectedOwnable.SetEquals(ownership.Keys), GameStateValidationErrorKind.InconsistentState, "moduleState.ownership.entries", "Ownership state must contain every ownable space exactly once.");
        return ownership;
    }

    private static HashSet<Guid> ValidateConsumedDecisions(GameStateV2 state)
    {
        HashSet<Guid> consumed = [];
        for (int index = 0; index < state.ConsumedDecisionIds.Count; index++)
        {
            Guid id = state.ConsumedDecisionIds[index];
            Ensure(id != Guid.Empty, GameStateValidationErrorKind.InvalidValue, $"consumedDecisionIds[{index}]", "Consumed decision IDs cannot be empty.");
            Ensure(consumed.Add(id), GameStateValidationErrorKind.DuplicateEntry, $"consumedDecisionIds[{index}]", "Consumed decision IDs must be unique.");
        }
        Ensure(
            state.LastConsumedDecisionId is null || consumed.Contains(state.LastConsumedDecisionId.Value),
            GameStateValidationErrorKind.InconsistentState,
            "lastConsumedDecisionId",
            "The last consumed decision ID must belong to the consumed set.");
        Ensure(
            state.PendingDecision is null || !consumed.Contains(state.PendingDecision.DecisionId),
            GameStateValidationErrorKind.InconsistentState,
            "pendingDecision.decisionId",
            "A pending decision cannot already be consumed.");
        return consumed;
    }

    private static DiceRoll? ValidateDiceRoll(DiceRollStateV2? saved, ValidatedGameProfile profile)
    {
        if (saved is null) return null;
        Ensure(saved.Purpose == RandomPurpose.TurnDice, GameStateValidationErrorKind.InvalidValue, "lastDiceRoll.purpose", "Only committed turn dice belong in the current save format.");
        Ensure(saved.Results.Count == profile.Setup.DiceCount, GameStateValidationErrorKind.InconsistentState, "lastDiceRoll.results", "The saved dice count does not match the profile.");
        try
        {
            return new DiceRoll(saved.Purpose, saved.Results, profile.Setup.DieSides);
        }
        catch (ArgumentException exception)
        {
            throw Error(GameStateValidationErrorKind.InvalidValue, "lastDiceRoll.results", "The saved dice results are outside the profile range.", exception);
        }
    }

    private static void ValidatePhaseAndTerminalState(
        GameStateV2 state,
        ValidatedGameProfile profile,
        IReadOnlyList<Player> players,
        DiceRoll? roll)
    {
        bool gameOver = state.Phase == GamePhase.GameOver;
        Ensure(gameOver == (state.WinnerPlayerId is not null), GameStateValidationErrorKind.InconsistentState, "winnerPlayerId", "The phase and winner state do not agree.");
        if (state.Phase == GamePhase.AwaitingDecision || gameOver)
            Ensure(roll is not null, GameStateValidationErrorKind.InconsistentState, "lastDiceRoll", "The saved phase requires a committed turn roll.");
        if (state.Phase == GamePhase.ReadyForTurn && roll is null)
            Ensure(state.CurrentPlayerId == state.RoundAnchorPlayerId && state.RoundNumber == 1, GameStateValidationErrorKind.InconsistentState, "lastDiceRoll", "A match without a committed roll must still be at its initial turn boundary.");

        if (!gameOver) return;
        Ensure(state.RoundNumber == profile.Policies.MatchEnd.RoundLimit, GameStateValidationErrorKind.InconsistentState, "roundNumber", "A terminal match must be in the declared final round.");
        int anchorIndex = players.ToList().FindIndex(player => player.Id == state.RoundAnchorPlayerId);
        int finalActorIndex = (anchorIndex - 1 + players.Count) % players.Count;
        Ensure(players[finalActorIndex].Id == state.CurrentPlayerId, GameStateValidationErrorKind.InconsistentState, "currentPlayerId", "The terminal current player is inconsistent with the completed round.");

        ResourceId score = profile.Policies.MatchEnd.ScoreResourceId;
        int expectedWinner = players
            .OrderByDescending(player => player.Resources[score])
            .ThenBy(player => player.Id)
            .First()
            .Id;
        Ensure(state.WinnerPlayerId == expectedWinner, GameStateValidationErrorKind.InconsistentState, "winnerPlayerId", "The saved winner does not satisfy the profile scoring and tie-break policy.");
    }

    private static (PendingDecision Decision, TurnContinuation Continuation) ValidateDecisionState(
        GameStateV2 state,
        ValidatedGameProfile profile,
        IReadOnlyList<Player> players,
        IReadOnlyDictionary<SpaceId, int?> ownership,
        HashSet<Guid> consumed,
        DiceRoll roll)
    {
        PendingDecisionStateV2 saved = state.PendingDecision ?? throw Error(
            GameStateValidationErrorKind.InconsistentState,
            "pendingDecision",
            "An awaiting-decision phase requires a pending decision.");
        TurnContinuationStateV2 savedContinuation = state.Continuation ?? throw Error(
            GameStateValidationErrorKind.InconsistentState,
            "continuation",
            "An awaiting-decision phase requires a continuation.");

        Ensure(saved.DecisionId != Guid.Empty && !consumed.Contains(saved.DecisionId), GameStateValidationErrorKind.InvalidValue, "pendingDecision.decisionId", "The pending decision ID is invalid.");
        Ensure(saved.Kind == DecisionKinds.Purchase, GameStateValidationErrorKind.InvalidValue, "pendingDecision.kind", "Only purchase decisions are supported by the current save format.");
        Ensure(saved.AllowedResponses.SequenceEqual([DecisionOptions.Accept, DecisionOptions.Decline]), GameStateValidationErrorKind.InconsistentState, "pendingDecision.allowedResponses", "The purchase decision responses do not match the Core contract.");
        Ensure(saved.PlayerId == state.CurrentPlayerId && savedContinuation.PlayerId == saved.PlayerId, GameStateValidationErrorKind.InconsistentState, "pendingDecision.playerId", "The pending decision actor is inconsistent with the current turn.");
        Player actor = players.Single(player => player.Id == saved.PlayerId);
        Ensure(actor.CurrentSpaceId == saved.SpaceId && savedContinuation.SpaceId == saved.SpaceId, GameStateValidationErrorKind.InconsistentState, "pendingDecision.spaceId", "The pending decision space is inconsistent with the actor and continuation.");

        SpaceDefinition space;
        try
        {
            space = profile.RuleGraph.Spaces.Single(candidate => candidate.Id == saved.SpaceId);
        }
        catch (InvalidOperationException exception)
        {
            throw Error(GameStateValidationErrorKind.BrokenReference, "pendingDecision.spaceId", "The pending decision space does not belong to the profile.", exception);
        }

        PurchasableCapabilityDefinition? purchase = space.Capabilities.Find<PurchasableCapabilityDefinition>();
        Ensure(space.Capabilities.Contains(CapabilityKinds.Ownable) && purchase is not null, GameStateValidationErrorKind.InconsistentState, "pendingDecision.spaceId", "The pending purchase does not reference an ownable purchasable space.");
        Ensure(purchase!.Price.ResourceId == saved.ResourceId && purchase.Price.Value == saved.ResourceAmount, GameStateValidationErrorKind.InconsistentState, "pendingDecision.price", "The pending price does not match the registered profile.");
        Ensure(ownership.TryGetValue(saved.SpaceId, out int? owner) && owner is null, GameStateValidationErrorKind.InconsistentState, "pendingDecision.spaceId", "The pending purchase space must still be unowned.");

        ProfileComponentRegistry registry = ProfileComponentRegistry.CreateExecutionBaseline();
        IReadOnlyList<CapabilityDefinition> ordered = registry.OrderLandingCapabilities(space.Capabilities);
        Ensure(savedContinuation.NextCapabilityIndex > 0 && savedContinuation.NextCapabilityIndex <= ordered.Count, GameStateValidationErrorKind.InvalidValue, "continuation.nextCapabilityIndex", "The continuation capability index is invalid.");
        Ensure(ordered[savedContinuation.NextCapabilityIndex - 1] is PurchasableCapabilityDefinition previous && previous.Price == purchase.Price, GameStateValidationErrorKind.InconsistentState, "continuation.nextCapabilityIndex", "The continuation does not follow the saved purchase capability.");

        PurchaseDecision decision = new(
            saved.DecisionId,
            saved.PlayerId,
            saved.SpaceId,
            new ResourceAmount(saved.ResourceId, saved.ResourceAmount),
            space.PresentationToken);
        return (decision, new TurnContinuation(savedContinuation.PlayerId, roll, savedContinuation.SpaceId, savedContinuation.NextCapabilityIndex));
    }

    private static void EnsurePlayerReference(int playerId, HashSet<int> playerIds, string path) =>
        Ensure(playerIds.Contains(playerId), GameStateValidationErrorKind.BrokenReference, path, "The player reference does not belong to the match.");

    private static void Ensure(bool condition, GameStateValidationErrorKind kind, string path, string message)
    {
        if (!condition) throw Error(kind, path, message);
    }

    private static GameStateValidationException Error(
        GameStateValidationErrorKind kind,
        string path,
        string message,
        Exception? innerException = null) =>
        new(kind, path, message, innerException);
}
