using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

internal sealed class ProfileSpace : Square
{
    internal ProfileSpace(int position, SpaceDefinition definition, PresentationMetadata presentation)
        : base(definition?.Id ?? throw new ArgumentNullException(nameof(definition)), position, presentation)
    {
        Capabilities = definition.Capabilities;
    }

    internal CapabilitySet Capabilities { get; }

    internal override void LandOn(Player player, Game game) =>
        throw new InvalidOperationException("Profile capability execution is not available yet.");
}
