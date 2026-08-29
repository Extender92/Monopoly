using System.Collections.ObjectModel;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Presentation;

namespace Monopoly.Core;

public enum ProfileContractErrorKind
{
    UnknownComponent,
    DuplicateDefinition,
    BrokenReference,
    InvalidCombination
}

public sealed class ProfileContractException : Exception
{
    internal ProfileContractException(ProfileContractErrorKind kind, string message)
        : base(message) => Kind = kind;

    public ProfileContractErrorKind Kind { get; }
}

public static class CapabilityKinds
{
    public static CapabilityId Move { get; } = new("move");
    public static CapabilityId Ownable { get; } = new("ownable");
    public static CapabilityId Purchasable { get; } = new("purchasable");
    public static CapabilityId UsageFee { get; } = new("usage-fee");
    public static CapabilityId Draw { get; } = new("draw");

    private static readonly HashSet<CapabilityId> Known = [Move, Ownable, Purchasable, UsageFee, Draw];

    public static void EnsureKnown(CapabilityId id)
    {
        if (!id.IsValid || !Known.Contains(id))
            throw new ProfileContractException(ProfileContractErrorKind.UnknownComponent, $"Capability '{id}' is not defined by the public contract.");
    }
}

public static class EffectKinds
{
    public static EffectKindId Move { get; } = new("move");
    public static EffectKindId ResourceChange { get; } = new("resource-change");
    public static EffectKindId Status { get; } = new("status");

    private static readonly HashSet<EffectKindId> Known = [Move, ResourceChange, Status];

    public static void EnsureKnown(EffectKindId id)
    {
        if (!id.IsValid || !Known.Contains(id))
            throw new ProfileContractException(ProfileContractErrorKind.UnknownComponent, $"Effect '{id}' is not defined by the public contract.");
    }
}

public abstract class CapabilityDefinition
{
    private protected CapabilityDefinition(CapabilityId id)
    {
        CapabilityKinds.EnsureKnown(id);
        Id = id;
    }

    public CapabilityId Id { get; }
}

public sealed class MoveCapabilityDefinition() : CapabilityDefinition(CapabilityKinds.Move);

public sealed class OwnableCapabilityDefinition : CapabilityDefinition
{
    public OwnableCapabilityDefinition(GroupId? groupId = null)
        : base(CapabilityKinds.Ownable)
    {
        if (groupId is { IsValid: false }) throw new ArgumentException("The group ID is invalid.", nameof(groupId));
        GroupId = groupId;
    }

    public GroupId? GroupId { get; }
}

public sealed class PurchasableCapabilityDefinition : CapabilityDefinition
{
    public PurchasableCapabilityDefinition(ResourceAmount price)
        : base(CapabilityKinds.Purchasable)
    {
        if (!price.IsValid) throw new ArgumentException("The price is invalid.", nameof(price));
        Price = price;
    }

    public ResourceAmount Price { get; }
}

public sealed class UsageFeeCapabilityDefinition : CapabilityDefinition
{
    public UsageFeeCapabilityDefinition(ResourceAmount amount)
        : base(CapabilityKinds.UsageFee)
    {
        if (!amount.IsValid) throw new ArgumentException("The usage fee is invalid.", nameof(amount));
        Amount = amount;
    }

    public ResourceAmount Amount { get; }
}

public sealed class DrawCapabilityDefinition : CapabilityDefinition
{
    public DrawCapabilityDefinition(DeckId deckId)
        : base(CapabilityKinds.Draw)
    {
        if (!deckId.IsValid) throw new ArgumentException("The deck ID is invalid.", nameof(deckId));
        DeckId = deckId;
    }

    public DeckId DeckId { get; }
}

public sealed class CapabilitySet
{
    private readonly ReadOnlyCollection<CapabilityDefinition> _entries;
    private readonly ReadOnlyDictionary<CapabilityId, CapabilityDefinition> _byId;

    public CapabilitySet(IEnumerable<CapabilityDefinition> capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        CapabilityDefinition[] supplied = capabilities.ToArray();
        if (supplied.Any(capability => capability is null))
            throw new ArgumentException("A capability set cannot contain null entries.", nameof(capabilities));

        Dictionary<CapabilityId, CapabilityDefinition> byId = [];
        foreach (CapabilityDefinition capability in supplied)
        {
            CapabilityKinds.EnsureKnown(capability.Id);
            if (!byId.TryAdd(capability.Id, capability))
                throw new ProfileContractException(ProfileContractErrorKind.DuplicateDefinition, $"Capability '{capability.Id}' is duplicated.");
        }

        CapabilityDefinition[] sorted = supplied.OrderBy(capability => capability.Id).ToArray();
        _entries = Array.AsReadOnly(sorted);
        _byId = new ReadOnlyDictionary<CapabilityId, CapabilityDefinition>(sorted.ToDictionary(capability => capability.Id));
    }

    public IReadOnlyList<CapabilityDefinition> Entries => _entries;
    public IReadOnlyDictionary<CapabilityId, CapabilityDefinition> ById => _byId;
    public int Count => _entries.Count;
    public bool Contains(CapabilityId id) => id.IsValid && _byId.ContainsKey(id);
    public T? Find<T>() where T : CapabilityDefinition => _entries.OfType<T>().SingleOrDefault();
}

public abstract class EffectDefinition
{
    private protected EffectDefinition(EffectKindId kind)
    {
        EffectKinds.EnsureKnown(kind);
        Kind = kind;
    }

    public EffectKindId Kind { get; }
}

public abstract class MoveTarget
{
    private protected MoveTarget()
    {
    }
}

public sealed class RelativeMoveTarget : MoveTarget
{
    public RelativeMoveTarget(int offset)
    {
        if (offset == 0) throw new ArgumentOutOfRangeException(nameof(offset), "A relative move cannot have a zero offset.");
        Offset = offset;
    }

    public int Offset { get; }
}

public sealed class AbsoluteMoveTarget : MoveTarget
{
    public AbsoluteMoveTarget(SpaceId spaceId)
    {
        if (!spaceId.IsValid) throw new ArgumentException("The target space ID is invalid.", nameof(spaceId));
        SpaceId = spaceId;
    }

    public SpaceId SpaceId { get; }
}

public enum PassOriginPolicy
{
    Ignore,
    ApplyProfileReward
}

public sealed class MoveEffectDefinition : EffectDefinition
{
    public MoveEffectDefinition(MoveTarget target, PassOriginPolicy passOriginPolicy, bool resolveDestination)
        : base(EffectKinds.Move)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        if (!Enum.IsDefined(passOriginPolicy)) throw new ArgumentOutOfRangeException(nameof(passOriginPolicy));
        PassOriginPolicy = passOriginPolicy;
        ResolveDestination = resolveDestination;
    }

    public MoveTarget Target { get; }
    public PassOriginPolicy PassOriginPolicy { get; }
    public bool ResolveDestination { get; }
}

public sealed class ResourceChangeEffectDefinition : EffectDefinition
{
    public ResourceChangeEffectDefinition(ResourceId resourceId, int delta)
        : base(EffectKinds.ResourceChange)
    {
        if (!resourceId.IsValid) throw new ArgumentException("The resource ID is invalid.", nameof(resourceId));
        if (delta == 0) throw new ArgumentOutOfRangeException(nameof(delta), "A resource change cannot be zero.");
        ResourceId = resourceId;
        Delta = delta;
    }

    public ResourceId ResourceId { get; }
    public int Delta { get; }
}

public enum StatusEffectOperation
{
    Apply,
    Remove
}

public sealed class StatusEffectDefinition : EffectDefinition
{
    public StatusEffectDefinition(StatusId statusId, StatusEffectOperation operation, int value = 0)
        : base(EffectKinds.Status)
    {
        if (!statusId.IsValid) throw new ArgumentException("The status ID is invalid.", nameof(statusId));
        if (!Enum.IsDefined(operation)) throw new ArgumentOutOfRangeException(nameof(operation));
        if (value < 0 || (operation == StatusEffectOperation.Remove && value != 0))
            throw new ArgumentOutOfRangeException(nameof(value));
        StatusId = statusId;
        Operation = operation;
        Value = value;
    }

    public StatusId StatusId { get; }
    public StatusEffectOperation Operation { get; }
    public int Value { get; }
}

public sealed class EffectSequence
{
    private readonly ReadOnlyCollection<EffectDefinition> _entries;

    public EffectSequence(IEnumerable<EffectDefinition> effects)
    {
        ArgumentNullException.ThrowIfNull(effects);
        EffectDefinition[] entries = effects.ToArray();
        if (entries.Any(effect => effect is null))
            throw new ArgumentException("An effect sequence cannot contain null entries.", nameof(effects));
        foreach (EffectDefinition effect in entries) EffectKinds.EnsureKnown(effect.Kind);
        _entries = Array.AsReadOnly(entries);
    }

    public IReadOnlyList<EffectDefinition> Entries => _entries;
    public int Count => _entries.Count;
}

public sealed class StatusDefinition
{
    public StatusDefinition(StatusId id, PresentationToken presentationToken, int maximumValue)
    {
        if (!id.IsValid) throw new ArgumentException("The status ID is invalid.", nameof(id));
        if (!presentationToken.IsValid) throw new ArgumentException("The presentation token is invalid.", nameof(presentationToken));
        if (maximumValue < 0) throw new ArgumentOutOfRangeException(nameof(maximumValue));
        Id = id;
        PresentationToken = presentationToken;
        MaximumValue = maximumValue;
    }

    public StatusId Id { get; }
    public PresentationToken PresentationToken { get; }
    public int MaximumValue { get; }
}

public sealed record StatusView
{
    public StatusView(StatusId id, PresentationToken presentationToken, int value)
    {
        if (!id.IsValid) throw new ArgumentException("The status ID is invalid.", nameof(id));
        if (!presentationToken.IsValid) throw new ArgumentException("The presentation token is invalid.", nameof(presentationToken));
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        Id = id;
        PresentationToken = presentationToken;
        Value = value;
    }

    public StatusId Id { get; }
    public PresentationToken PresentationToken { get; }
    public int Value { get; }
}

public sealed record PlayerStatusView
{
    public PlayerStatusView(int playerId, StatusView status)
    {
        if (playerId < 0) throw new ArgumentOutOfRangeException(nameof(playerId));
        PlayerId = playerId;
        Status = status ?? throw new ArgumentNullException(nameof(status));
    }

    public int PlayerId { get; }
    public StatusView Status { get; }
}

public sealed class StatusCollection
{
    private readonly ReadOnlyCollection<PlayerStatusView> _entries;

    public StatusCollection(IEnumerable<PlayerStatusView> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);
        PlayerStatusView[] entries = statuses.ToArray();
        if (entries.Any(status => status is null))
            throw new ArgumentException("A status collection cannot contain null entries.", nameof(statuses));
        if (entries.Select(entry => (entry.PlayerId, entry.Status.Id)).Distinct().Count() != entries.Length)
            throw new ProfileContractException(ProfileContractErrorKind.DuplicateDefinition, "A player status is duplicated.");
        _entries = Array.AsReadOnly(entries
            .OrderBy(entry => entry.PlayerId)
            .ThenBy(entry => entry.Status.Id)
            .ToArray());
    }

    public IReadOnlyList<PlayerStatusView> Entries => _entries;
    public int Count => _entries.Count;
}

public enum StatusTransitionKind
{
    Applied,
    Updated,
    Removed
}

public sealed record StatusTransition
{
    public StatusTransition(int playerId, StatusId statusId, StatusTransitionKind kind)
    {
        if (playerId < 0) throw new ArgumentOutOfRangeException(nameof(playerId));
        if (!statusId.IsValid) throw new ArgumentException("The status ID is invalid.", nameof(statusId));
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        PlayerId = playerId;
        StatusId = statusId;
        Kind = kind;
    }

    public int PlayerId { get; }
    public StatusId StatusId { get; }
    public StatusTransitionKind Kind { get; }
}

public sealed class SpaceDefinition
{
    public SpaceDefinition(SpaceId id, PresentationToken presentationToken, CapabilitySet capabilities)
    {
        if (!id.IsValid) throw new ArgumentException("The space ID is invalid.", nameof(id));
        if (!presentationToken.IsValid) throw new ArgumentException("The presentation token is invalid.", nameof(presentationToken));
        Id = id;
        PresentationToken = presentationToken;
        Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    public SpaceId Id { get; }
    public PresentationToken PresentationToken { get; }
    public CapabilitySet Capabilities { get; }
}

public sealed class CardDefinition
{
    public CardDefinition(CardId id, PresentationToken presentationToken, EffectSequence effects)
    {
        if (!id.IsValid) throw new ArgumentException("The card ID is invalid.", nameof(id));
        if (!presentationToken.IsValid) throw new ArgumentException("The presentation token is invalid.", nameof(presentationToken));
        Id = id;
        PresentationToken = presentationToken;
        Effects = effects ?? throw new ArgumentNullException(nameof(effects));
    }

    public CardId Id { get; }
    public PresentationToken PresentationToken { get; }
    public EffectSequence Effects { get; }
}

public sealed class DeckDefinition
{
    private readonly ReadOnlyCollection<CardDefinition> _cards;

    public DeckDefinition(DeckId id, PresentationToken presentationToken, IEnumerable<CardDefinition> cards)
    {
        if (!id.IsValid) throw new ArgumentException("The deck ID is invalid.", nameof(id));
        if (!presentationToken.IsValid) throw new ArgumentException("The presentation token is invalid.", nameof(presentationToken));
        ArgumentNullException.ThrowIfNull(cards);
        CardDefinition[] supplied = cards.ToArray();
        if (supplied.Length == 0 || supplied.Any(card => card is null))
            throw new ArgumentException("A declared deck requires at least one non-null card.", nameof(cards));
        if (supplied.Select(card => card.Id).Distinct().Count() != supplied.Length)
            throw new ProfileContractException(ProfileContractErrorKind.DuplicateDefinition, $"Deck '{id}' contains duplicate card IDs.");
        Id = id;
        PresentationToken = presentationToken;
        _cards = Array.AsReadOnly(supplied);
    }

    public DeckId Id { get; }
    public PresentationToken PresentationToken { get; }
    public IReadOnlyList<CardDefinition> Cards => _cards;
}

public sealed class ProfileRuleGraph
{
    private readonly ReadOnlyCollection<ResourceId> _resources;
    private readonly ReadOnlyCollection<SpaceDefinition> _spaces;
    private readonly ReadOnlyCollection<DeckDefinition> _decks;
    private readonly ReadOnlyCollection<StatusDefinition> _statuses;

    public ProfileRuleGraph(
        GameTrack track,
        IEnumerable<ResourceId> resources,
        CapabilitySet profileCapabilities,
        IEnumerable<SpaceDefinition> spaces,
        IEnumerable<DeckDefinition> decks,
        IEnumerable<StatusDefinition> statuses)
    {
        Track = track ?? throw new ArgumentNullException(nameof(track));
        ArgumentNullException.ThrowIfNull(resources);
        ProfileCapabilities = profileCapabilities ?? throw new ArgumentNullException(nameof(profileCapabilities));
        ArgumentNullException.ThrowIfNull(spaces);
        ArgumentNullException.ThrowIfNull(decks);
        ArgumentNullException.ThrowIfNull(statuses);

        ResourceId[] resourceEntries = resources.ToArray();
        SpaceDefinition[] spaceEntries = spaces.ToArray();
        DeckDefinition[] deckEntries = decks.ToArray();
        StatusDefinition[] statusEntries = statuses.ToArray();
        RejectNulls(spaceEntries, nameof(spaces));
        RejectNulls(deckEntries, nameof(decks));
        RejectNulls(statusEntries, nameof(statuses));
        EnsureUnique(resourceEntries, nameof(resources));
        EnsureUnique(spaceEntries.Select(space => space.Id), nameof(spaces));
        EnsureUnique(deckEntries.Select(deck => deck.Id), nameof(decks));
        EnsureUnique(statusEntries.Select(status => status.Id), nameof(statuses));

        if (ProfileCapabilities.Entries.Any(capability => capability is not MoveCapabilityDefinition))
            throw InvalidCombination("Only Move is valid at profile capability scope.");

        Dictionary<SpaceId, SpaceDefinition> spacesById = spaceEntries.ToDictionary(space => space.Id);
        if (spacesById.Count != Track.Count || Track.SpaceIds.Any(id => !spacesById.ContainsKey(id)))
            throw BrokenReference("Every track space must have exactly one space definition.");

        if (resourceEntries.Any(resource => !resource.IsValid))
            throw InvalidCombination("Every declared resource requires a valid ID.");

        HashSet<ResourceId> resourceIds = resourceEntries.ToHashSet();
        HashSet<DeckId> deckIds = deckEntries.Select(deck => deck.Id).ToHashSet();
        Dictionary<StatusId, StatusDefinition> statusesById = statusEntries.ToDictionary(status => status.Id);
        HashSet<CardId> cardIds = [];
        foreach (DeckDefinition deck in deckEntries)
        {
            foreach (CardDefinition card in deck.Cards)
            {
                if (!cardIds.Add(card.Id))
                    throw new ProfileContractException(ProfileContractErrorKind.DuplicateDefinition, $"Card ID '{card.Id}' occurs in more than one deck.");
                ValidateEffects(card.Effects, Track, resourceIds, statusesById);
            }
        }

        foreach (SpaceDefinition space in spaceEntries)
            ValidateSpace(space, resourceIds, deckIds);

        _resources = Array.AsReadOnly(resourceEntries.OrderBy(id => id).ToArray());
        _spaces = Array.AsReadOnly(Track.SpaceIds.Select(id => spacesById[id]).ToArray());
        _decks = Array.AsReadOnly(deckEntries.OrderBy(deck => deck.Id).ToArray());
        _statuses = Array.AsReadOnly(statusEntries.OrderBy(status => status.Id).ToArray());
    }

    public GameTrack Track { get; }
    public CapabilitySet ProfileCapabilities { get; }
    public IReadOnlyList<ResourceId> Resources => _resources;
    public IReadOnlyList<SpaceDefinition> Spaces => _spaces;
    public IReadOnlyList<DeckDefinition> Decks => _decks;
    public IReadOnlyList<StatusDefinition> Statuses => _statuses;

    private static void ValidateSpace(SpaceDefinition space, HashSet<ResourceId> resources, HashSet<DeckId> decks)
    {
        if (space.Capabilities.Entries.Any(capability => capability is MoveCapabilityDefinition))
            throw InvalidCombination($"Space '{space.Id}' contains a profile-scoped Move capability.");
        bool ownable = space.Capabilities.Contains(CapabilityKinds.Ownable);
        if (!ownable && (space.Capabilities.Contains(CapabilityKinds.Purchasable) || space.Capabilities.Contains(CapabilityKinds.UsageFee)))
            throw InvalidCombination($"Space '{space.Id}' must be Ownable before it can be Purchasable or charge a UsageFee.");

        foreach (CapabilityDefinition capability in space.Capabilities.Entries)
        {
            switch (capability)
            {
                case PurchasableCapabilityDefinition purchasable when !resources.Contains(purchasable.Price.ResourceId):
                    throw BrokenReference($"Space '{space.Id}' references missing resource '{purchasable.Price.ResourceId}'.");
                case UsageFeeCapabilityDefinition fee when !resources.Contains(fee.Amount.ResourceId):
                    throw BrokenReference($"Space '{space.Id}' references missing resource '{fee.Amount.ResourceId}'.");
                case DrawCapabilityDefinition draw when !decks.Contains(draw.DeckId):
                    throw BrokenReference($"Space '{space.Id}' references missing deck '{draw.DeckId}'.");
            }
        }
    }

    private static void ValidateEffects(
        EffectSequence effects,
        GameTrack track,
        HashSet<ResourceId> resources,
        Dictionary<StatusId, StatusDefinition> statuses)
    {
        foreach (EffectDefinition effect in effects.Entries)
        {
            switch (effect)
            {
                case MoveEffectDefinition { Target: AbsoluteMoveTarget absolute }:
                    try { _ = track.GetIndex(absolute.SpaceId); }
                    catch (KeyNotFoundException) { throw BrokenReference($"Move effect references missing space '{absolute.SpaceId}'."); }
                    break;
                case ResourceChangeEffectDefinition resource when !resources.Contains(resource.ResourceId):
                    throw BrokenReference($"ResourceChange effect references missing resource '{resource.ResourceId}'.");
                case StatusEffectDefinition status:
                    if (!statuses.TryGetValue(status.StatusId, out StatusDefinition? definition))
                        throw BrokenReference($"Status effect references missing status '{status.StatusId}'.");
                    if (status.Operation == StatusEffectOperation.Apply && status.Value > definition.MaximumValue)
                        throw InvalidCombination($"Status effect value exceeds status '{status.StatusId}' maximum.");
                    break;
            }
        }
    }

    private static void RejectNulls<T>(IEnumerable<T> values, string parameterName) where T : class
    {
        if (values.Any(value => value is null)) throw new ArgumentException("A profile collection cannot contain null entries.", parameterName);
    }

    private static void EnsureUnique<T>(IEnumerable<T> values, string collectionName) where T : notnull
    {
        HashSet<T> seen = [];
        foreach (T value in values)
        {
            if (!seen.Add(value))
                throw new ProfileContractException(ProfileContractErrorKind.DuplicateDefinition, $"'{value}' is duplicated in {collectionName}.");
        }
    }

    private static ProfileContractException BrokenReference(string message) =>
        new(ProfileContractErrorKind.BrokenReference, message);

    private static ProfileContractException InvalidCombination(string message) =>
        new(ProfileContractErrorKind.InvalidCombination, message);
}
