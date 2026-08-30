using System.Collections.ObjectModel;

namespace Monopoly.Core;

public sealed record SpaceOwnershipView
{
    public SpaceOwnershipView(SpaceId spaceId, int? ownerPlayerId)
    {
        if (!spaceId.IsValid) throw new ArgumentException("The space ID is invalid.", nameof(spaceId));
        if (ownerPlayerId < 0) throw new ArgumentOutOfRangeException(nameof(ownerPlayerId));
        SpaceId = spaceId;
        OwnerPlayerId = ownerPlayerId;
    }

    public SpaceId SpaceId { get; }
    public int? OwnerPlayerId { get; }
}

public sealed class OwnershipCollection
{
    private readonly ReadOnlyCollection<SpaceOwnershipView> _entries;
    private readonly ReadOnlyDictionary<SpaceId, SpaceOwnershipView> _bySpaceId;

    public OwnershipCollection(IEnumerable<SpaceOwnershipView> ownership)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        SpaceOwnershipView[] entries = ownership.ToArray();
        if (entries.Any(entry => entry is null))
            throw new ArgumentException("Ownership cannot contain null entries.", nameof(ownership));
        if (entries.Select(entry => entry.SpaceId).Distinct().Count() != entries.Length)
            throw new ArgumentException("Ownership space IDs must be unique.", nameof(ownership));

        SpaceOwnershipView[] sorted = entries.OrderBy(entry => entry.SpaceId).ToArray();
        _entries = Array.AsReadOnly(sorted);
        _bySpaceId = new ReadOnlyDictionary<SpaceId, SpaceOwnershipView>(
            sorted.ToDictionary(entry => entry.SpaceId));
    }

    public IReadOnlyList<SpaceOwnershipView> Entries => _entries;
    public IReadOnlyDictionary<SpaceId, SpaceOwnershipView> BySpaceId => _bySpaceId;
    public int Count => _entries.Count;
}

public sealed class ProfileModuleState
{
    public ProfileModuleState(OwnershipCollection ownership, StatusCollection statuses)
    {
        Ownership = ownership ?? throw new ArgumentNullException(nameof(ownership));
        Statuses = statuses ?? throw new ArgumentNullException(nameof(statuses));
    }

    public OwnershipCollection Ownership { get; }
    public StatusCollection Statuses { get; }
}
