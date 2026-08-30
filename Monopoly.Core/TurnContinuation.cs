using Monopoly.Core.Randomness;

namespace Monopoly.Core;

/// <summary>Primitive-only state needed to resume a capability pipeline after a decision.</summary>
internal sealed class TurnContinuation
{
    internal TurnContinuation(int playerId, DiceRoll roll, SpaceId spaceId, int nextCapabilityIndex)
    {
        if (playerId < 0) throw new ArgumentOutOfRangeException(nameof(playerId));
        ArgumentNullException.ThrowIfNull(roll);
        if (!spaceId.IsValid) throw new ArgumentException("The continuation space ID is invalid.", nameof(spaceId));
        if (nextCapabilityIndex < 0) throw new ArgumentOutOfRangeException(nameof(nextCapabilityIndex));
        PlayerId = playerId;
        Roll = roll;
        SpaceId = spaceId;
        NextCapabilityIndex = nextCapabilityIndex;
    }

    internal int PlayerId { get; }
    internal DiceRoll Roll { get; }
    internal SpaceId SpaceId { get; }
    internal int NextCapabilityIndex { get; }
}
