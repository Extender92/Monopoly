using System.Collections.ObjectModel;

namespace Monopoly.Core.Models.Board;

/// <summary>An immutable structural view of the profile-defined ordered track.</summary>
public sealed class GameBoard
{
    private readonly ReadOnlyCollection<SpaceView> _spaces;
    private readonly ReadOnlyDictionary<SpaceId, SpaceDefinition> _definitions;

    internal GameBoard(IEnumerable<SpaceDefinition> spaces)
    {
        ArgumentNullException.ThrowIfNull(spaces);
        SpaceDefinition[] definitions = spaces.ToArray();
        if (definitions.Length == 0 || definitions.Any(space => space is null))
            throw new ArgumentException("A game board requires at least one non-null space definition.", nameof(spaces));
        if (definitions.Select(space => space.Id).Distinct().Count() != definitions.Length)
            throw new ArgumentException("Space IDs must be unique.", nameof(spaces));

        Track = new GameTrack(definitions.Select(space => space.Id));
        _spaces = Array.AsReadOnly(definitions
            .Select((space, index) => new SpaceView(space.Id, index, space.PresentationToken))
            .ToArray());
        _definitions = new ReadOnlyDictionary<SpaceId, SpaceDefinition>(
            definitions.ToDictionary(space => space.Id));
    }

    public GameTrack Track { get; }
    public IReadOnlyList<SpaceView> Spaces => _spaces;

    public SpaceView GetSpace(SpaceId id)
    {
        int index = Track.GetIndex(id);
        return _spaces[index];
    }

    internal SpaceDefinition GetDefinition(SpaceId id) =>
        !id.IsValid
            ? throw new ArgumentException("The space ID is invalid.", nameof(id))
            : _definitions.TryGetValue(id, out SpaceDefinition? definition)
                ? definition
                : throw new KeyNotFoundException($"Space ID '{id}' does not belong to this board.");
}
