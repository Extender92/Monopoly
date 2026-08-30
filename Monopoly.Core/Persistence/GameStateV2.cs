using System.Collections.ObjectModel;
using Monopoly.Core.Randomness;

namespace Monopoly.Core.Persistence;

public static class GameSaveFormat
{
    public const int Version2 = 2;
    public const int OwnershipModuleVersion = 1;
    public const int StatusModuleVersion = 1;
    public const int MaximumInputBytes = 5 * 1024 * 1024;
    public const int MaximumJsonDepth = 64;
}

public sealed record ResourceBalanceStateV2(ResourceId ResourceId, int Value);

public sealed class PlayerStateV2
{
    private readonly ReadOnlyCollection<ResourceBalanceStateV2> _resources;

    public PlayerStateV2(int playerId, string name, SpaceId spaceId, IEnumerable<ResourceBalanceStateV2> resources)
    {
        PlayerId = playerId;
        Name = name;
        SpaceId = spaceId;
        _resources = Copy(resources, nameof(resources));
    }

    public int PlayerId { get; }
    public string Name { get; }
    public SpaceId SpaceId { get; }
    public IReadOnlyList<ResourceBalanceStateV2> Resources => _resources;

    private static ReadOnlyCollection<ResourceBalanceStateV2> Copy(
        IEnumerable<ResourceBalanceStateV2> entries,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(entries, parameterName);
        ResourceBalanceStateV2[] copied = entries.ToArray();
        if (copied.Any(entry => entry is null))
            throw new ArgumentException("Resource state cannot contain null entries.", parameterName);
        return Array.AsReadOnly(copied);
    }
}

public sealed class DeckStateV2
{
    private readonly ReadOnlyCollection<CardId> _cardIds;

    public DeckStateV2(DeckId deckId, IEnumerable<CardId> cardIds)
    {
        DeckId = deckId;
        ArgumentNullException.ThrowIfNull(cardIds);
        _cardIds = Array.AsReadOnly(cardIds.ToArray());
    }

    public DeckId DeckId { get; }
    public IReadOnlyList<CardId> CardIds => _cardIds;
}

public sealed record OwnershipStateV2(SpaceId SpaceId, int? OwnerPlayerId);

public sealed record PlayerStatusStateV2(int PlayerId, StatusId StatusId, int Value);

public sealed class ModuleStateV2
{
    private readonly ReadOnlyCollection<OwnershipStateV2> _ownership;
    private readonly ReadOnlyCollection<PlayerStatusStateV2> _statuses;

    public ModuleStateV2(
        int ownershipVersion,
        IEnumerable<OwnershipStateV2> ownership,
        int statusVersion,
        IEnumerable<PlayerStatusStateV2> statuses)
    {
        OwnershipVersion = ownershipVersion;
        StatusVersion = statusVersion;
        _ownership = Copy(ownership, nameof(ownership));
        _statuses = Copy(statuses, nameof(statuses));
    }

    public int OwnershipVersion { get; }
    public IReadOnlyList<OwnershipStateV2> Ownership => _ownership;
    public int StatusVersion { get; }
    public IReadOnlyList<PlayerStatusStateV2> Statuses => _statuses;

    private static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> entries, string parameterName) where T : class
    {
        ArgumentNullException.ThrowIfNull(entries, parameterName);
        T[] copied = entries.ToArray();
        if (copied.Any(entry => entry is null))
            throw new ArgumentException("Module state cannot contain null entries.", parameterName);
        return Array.AsReadOnly(copied);
    }
}

public sealed class DiceRollStateV2
{
    private readonly ReadOnlyCollection<int> _results;

    public DiceRollStateV2(RandomPurpose purpose, IEnumerable<int> results)
    {
        Purpose = purpose;
        ArgumentNullException.ThrowIfNull(results);
        _results = Array.AsReadOnly(results.ToArray());
    }

    public RandomPurpose Purpose { get; }
    public IReadOnlyList<int> Results => _results;
}

public sealed class PendingDecisionStateV2
{
    private readonly ReadOnlyCollection<DecisionOptionId> _allowedResponses;

    public PendingDecisionStateV2(
        Guid decisionId,
        DecisionKindId kind,
        int playerId,
        IEnumerable<DecisionOptionId> allowedResponses,
        SpaceId spaceId,
        ResourceId resourceId,
        int resourceAmount)
    {
        DecisionId = decisionId;
        Kind = kind;
        PlayerId = playerId;
        ArgumentNullException.ThrowIfNull(allowedResponses);
        _allowedResponses = Array.AsReadOnly(allowedResponses.ToArray());
        SpaceId = spaceId;
        ResourceId = resourceId;
        ResourceAmount = resourceAmount;
    }

    public Guid DecisionId { get; }
    public DecisionKindId Kind { get; }
    public int PlayerId { get; }
    public IReadOnlyList<DecisionOptionId> AllowedResponses => _allowedResponses;
    public SpaceId SpaceId { get; }
    public ResourceId ResourceId { get; }
    public int ResourceAmount { get; }
}

public sealed record TurnContinuationStateV2(int PlayerId, SpaceId SpaceId, int NextCapabilityIndex);

public sealed class GameStateV2
{
    private readonly ReadOnlyCollection<PlayerStateV2> _players;
    private readonly ReadOnlyCollection<DeckStateV2> _decks;
    private readonly ReadOnlyCollection<Guid> _consumedDecisionIds;

    public GameStateV2(
        int formatVersion,
        ProfileId profileId,
        ProfileRevision profileRevision,
        ProfileFingerprint profileFingerprint,
        IEnumerable<PlayerStateV2> players,
        int currentPlayerId,
        int roundAnchorPlayerId,
        int roundNumber,
        GamePhase phase,
        DiceRollStateV2? lastDiceRoll,
        int? winnerPlayerId,
        IEnumerable<DeckStateV2> decks,
        ModuleStateV2 moduleState,
        PendingDecisionStateV2? pendingDecision,
        TurnContinuationStateV2? continuation,
        IEnumerable<Guid> consumedDecisionIds,
        Guid? lastConsumedDecisionId)
    {
        FormatVersion = formatVersion;
        ProfileId = profileId;
        ProfileRevision = profileRevision;
        ProfileFingerprint = profileFingerprint;
        _players = Copy(players, nameof(players));
        CurrentPlayerId = currentPlayerId;
        RoundAnchorPlayerId = roundAnchorPlayerId;
        RoundNumber = roundNumber;
        Phase = phase;
        LastDiceRoll = lastDiceRoll;
        WinnerPlayerId = winnerPlayerId;
        _decks = Copy(decks, nameof(decks));
        ModuleState = moduleState ?? throw new ArgumentNullException(nameof(moduleState));
        PendingDecision = pendingDecision;
        Continuation = continuation;
        ArgumentNullException.ThrowIfNull(consumedDecisionIds);
        _consumedDecisionIds = Array.AsReadOnly(consumedDecisionIds.ToArray());
        LastConsumedDecisionId = lastConsumedDecisionId;
    }

    public int FormatVersion { get; }
    public ProfileId ProfileId { get; }
    public ProfileRevision ProfileRevision { get; }
    public ProfileFingerprint ProfileFingerprint { get; }
    public IReadOnlyList<PlayerStateV2> Players => _players;
    public int CurrentPlayerId { get; }
    public int RoundAnchorPlayerId { get; }
    public int RoundNumber { get; }
    public GamePhase Phase { get; }
    public DiceRollStateV2? LastDiceRoll { get; }
    public int? WinnerPlayerId { get; }
    public IReadOnlyList<DeckStateV2> Decks => _decks;
    public ModuleStateV2 ModuleState { get; }
    public PendingDecisionStateV2? PendingDecision { get; }
    public TurnContinuationStateV2? Continuation { get; }
    public IReadOnlyList<Guid> ConsumedDecisionIds => _consumedDecisionIds;
    public Guid? LastConsumedDecisionId { get; }

    private static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> entries, string parameterName) where T : class
    {
        ArgumentNullException.ThrowIfNull(entries, parameterName);
        T[] copied = entries.ToArray();
        if (copied.Any(entry => entry is null))
            throw new ArgumentException("Game state cannot contain null entries.", parameterName);
        return Array.AsReadOnly(copied);
    }
}
