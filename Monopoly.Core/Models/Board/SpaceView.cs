using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

/// <summary>An immutable public projection of one position on the ordered track.</summary>
public sealed record SpaceView
{
    public SpaceView(SpaceId id, int index, PresentationToken presentationToken)
    {
        if (!id.IsValid) throw new ArgumentException("The space ID is invalid.", nameof(id));
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        if (!presentationToken.IsValid) throw new ArgumentException("The presentation token is invalid.", nameof(presentationToken));
        Id = id;
        Index = index;
        PresentationToken = presentationToken;
    }

    public SpaceId Id { get; }
    public int Index { get; }
    public PresentationToken PresentationToken { get; }
}
