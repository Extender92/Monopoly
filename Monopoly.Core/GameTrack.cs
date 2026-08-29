using System.Collections.ObjectModel;

namespace Monopoly.Core;

/// <summary>An immutable ordered route whose identity is independent of presentation and position.</summary>
public sealed class GameTrack
{
    private readonly ReadOnlyCollection<SpaceId> _spaceIds;
    private readonly ReadOnlyDictionary<SpaceId, int> _indices;

    public GameTrack(IEnumerable<SpaceId> spaceIds)
    {
        ArgumentNullException.ThrowIfNull(spaceIds);

        SpaceId[] ordered = spaceIds.ToArray();
        if (ordered.Length == 0)
            throw new ArgumentException("A game track requires at least one space.", nameof(spaceIds));
        if (ordered.Any(id => !id.IsValid))
            throw new ArgumentException("A game track contains an invalid space ID.", nameof(spaceIds));

        Dictionary<SpaceId, int> indices = [];
        for (int index = 0; index < ordered.Length; index++)
        {
            if (!indices.TryAdd(ordered[index], index))
                throw new ArgumentException($"Space ID '{ordered[index]}' is duplicated.", nameof(spaceIds));
        }

        _spaceIds = Array.AsReadOnly(ordered);
        _indices = new ReadOnlyDictionary<SpaceId, int>(indices);
    }

    public IReadOnlyList<SpaceId> SpaceIds => _spaceIds;
    public int Count => _spaceIds.Count;

    public SpaceId GetSpaceIdAt(int index) => _spaceIds[index];

    public int GetIndex(SpaceId id)
    {
        if (!id.IsValid) throw new ArgumentException("The space ID is invalid.", nameof(id));
        return _indices.TryGetValue(id, out int index)
            ? index
            : throw new KeyNotFoundException($"Space ID '{id}' does not belong to this track.");
    }

    public int NormalizeIndex(long index)
    {
        long remainder = index % Count;
        return (int)(remainder < 0 ? remainder + Count : remainder);
    }

    public SpaceId GetSpaceIdAfter(SpaceId origin, long offset) =>
        GetSpaceIdAt(NormalizeIndex((long)GetIndex(origin) + offset));
}
