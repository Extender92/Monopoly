using System.Collections.ObjectModel;

namespace Monopoly.Core.Models;

/// <summary>A participant projection whose authoritative state is mutated only by Core.</summary>
public sealed class Player
{
    private readonly Dictionary<ResourceId, int> _resources = [];
    private readonly ReadOnlyDictionary<ResourceId, int> _resourcesView;

    internal Player(string name, int id)
    {
        Id = id >= 0 ? id : throw new ArgumentOutOfRangeException(nameof(id));
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new ArgumentException("Player name cannot be empty.", nameof(name));
        _resourcesView = new ReadOnlyDictionary<ResourceId, int>(_resources);
    }

    public int Id { get; }
    public string Name { get; }
    public int Position { get; private set; }
    public SpaceId CurrentSpaceId { get; private set; }
    public IReadOnlyDictionary<ResourceId, int> Resources => _resourcesView;

    internal void InitializeProfileState(IEnumerable<ResourceAmount> resources, SpaceId spaceId, int position)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ResourceAmount[] supplied = resources.ToArray();
        if (supplied.Any(resource => !resource.IsValid) ||
            supplied.Select(resource => resource.ResourceId).Distinct().Count() != supplied.Length)
        {
            throw new ArgumentException("Profile resources must contain unique valid resource amounts.", nameof(resources));
        }

        ApplyState(
            supplied.ToDictionary(resource => resource.ResourceId, resource => resource.Value),
            spaceId,
            position);
    }

    internal void ApplyState(IReadOnlyDictionary<ResourceId, int> resources, SpaceId spaceId, int position)
    {
        ArgumentNullException.ThrowIfNull(resources);
        if (!spaceId.IsValid) throw new ArgumentException("The current space ID is invalid.", nameof(spaceId));
        if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));
        if (resources.Any(entry => !entry.Key.IsValid || entry.Value < 0))
            throw new ArgumentException("Player resources must use valid IDs and non-negative values.", nameof(resources));

        _resources.Clear();
        foreach ((ResourceId id, int value) in resources.OrderBy(entry => entry.Key))
            _resources.Add(id, value);
        Position = position;
        CurrentSpaceId = spaceId;
    }
}
