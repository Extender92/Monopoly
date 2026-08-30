namespace Infrastructure.Profiles;

internal sealed class ProfileDocumentDto
{
    public int? SchemaVersion { get; set; }
    public string? ProfileId { get; set; }
    public int? Revision { get; set; }
    public string? ProfilePresentationToken { get; set; }
    public List<PresentationDto>? Presentation { get; set; }
    public List<ResourceDto>? Resources { get; set; }
    public SetupDto? Setup { get; set; }
    public List<string>? Track { get; set; }
    public List<CapabilityDto>? ProfileCapabilities { get; set; }
    public List<SpaceDto>? Spaces { get; set; }
    public List<DeckDto>? Decks { get; set; }
    public List<StatusDto>? Statuses { get; set; }
    public PoliciesDto? Policies { get; set; }
}

internal sealed class PresentationDto
{
    public string? Token { get; set; }
    public string? DisplayText { get; set; }
    public string? ShortText { get; set; }
    public string? Description { get; set; }
    public string? Symbol { get; set; }
    public string? ColorToken { get; set; }
    public string? LayoutToken { get; set; }
}

internal sealed class ResourceDto
{
    public string? Id { get; set; }
    public string? PresentationToken { get; set; }
}

internal sealed class ResourceAmountDto
{
    public string? ResourceId { get; set; }
    public int? Value { get; set; }
}

internal sealed class SetupDto
{
    public int? MinimumPlayers { get; set; }
    public int? MaximumPlayers { get; set; }
    public int? DiceCount { get; set; }
    public int? DieSides { get; set; }
    public string? StartSpaceId { get; set; }
    public List<ResourceAmountDto>? StartingResources { get; set; }
    public string? StartingPlayerPolicy { get; set; }
}

internal sealed class CapabilityDto
{
    public string? Kind { get; set; }
    public string? GroupId { get; set; }
    public ResourceAmountDto? Price { get; set; }
    public ResourceAmountDto? Amount { get; set; }
    public string? DeckId { get; set; }
}

internal sealed class SpaceDto
{
    public string? Id { get; set; }
    public string? PresentationToken { get; set; }
    public List<CapabilityDto>? Capabilities { get; set; }
}

internal sealed class DeckDto
{
    public string? Id { get; set; }
    public string? PresentationToken { get; set; }
    public List<CardDto>? Cards { get; set; }
}

internal sealed class CardDto
{
    public string? Id { get; set; }
    public string? PresentationToken { get; set; }
    public List<EffectDto>? Effects { get; set; }
}

internal sealed class EffectDto
{
    public string? Kind { get; set; }
    public MoveTargetDto? Target { get; set; }
    public string? PassOriginPolicy { get; set; }
    public bool? ResolveDestination { get; set; }
    public string? ResourceId { get; set; }
    public int? Delta { get; set; }
    public string? StatusId { get; set; }
    public string? Operation { get; set; }
    public int? Value { get; set; }
}

internal sealed class MoveTargetDto
{
    public string? Kind { get; set; }
    public int? Offset { get; set; }
    public string? SpaceId { get; set; }
}

internal sealed class StatusDto
{
    public string? Id { get; set; }
    public string? PresentationToken { get; set; }
    public int? MaximumValue { get; set; }
}

internal sealed class PoliciesDto
{
    public ResourceAmountDto? PassOriginReward { get; set; }
    public string? PurchaseDecline { get; set; }
    public MatchEndDto? MatchEnd { get; set; }
}

internal sealed class MatchEndDto
{
    public string? Kind { get; set; }
    public int? RoundLimit { get; set; }
    public string? ResourceId { get; set; }
    public string? TieBreak { get; set; }
}
