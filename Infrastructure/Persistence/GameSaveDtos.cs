namespace Infrastructure.Persistence;

internal sealed class GameSaveDocumentDto
{
    public required int? FormatVersion { get; set; }
    public required SavedProfileDto? Profile { get; set; }
    public required SavedMatchDto? Match { get; set; }
}

internal sealed class SavedProfileDto
{
    public required string? Id { get; set; }
    public required int? Revision { get; set; }
    public required string? Fingerprint { get; set; }
}

internal sealed class SavedMatchDto
{
    public required List<SavedPlayerDto?>? Players { get; set; }
    public required int? CurrentPlayerId { get; set; }
    public required int? RoundAnchorPlayerId { get; set; }
    public required int? RoundNumber { get; set; }
    public required string? Phase { get; set; }
    public required SavedDiceRollDto? LastDiceRoll { get; set; }
    public required int? WinnerPlayerId { get; set; }
    public required List<SavedDeckDto?>? Decks { get; set; }
    public required SavedModuleStateDto? ModuleState { get; set; }
    public required SavedPendingDecisionDto? PendingDecision { get; set; }
    public required SavedContinuationDto? Continuation { get; set; }
    public required List<Guid>? ConsumedDecisionIds { get; set; }
    public required Guid? LastConsumedDecisionId { get; set; }
}

internal sealed class SavedPlayerDto
{
    public required int? PlayerId { get; set; }
    public required string? Name { get; set; }
    public required string? SpaceId { get; set; }
    public required List<SavedResourceDto?>? Resources { get; set; }
}

internal sealed class SavedResourceDto
{
    public required string? ResourceId { get; set; }
    public required int? Value { get; set; }
}

internal sealed class SavedDeckDto
{
    public required string? DeckId { get; set; }
    public required List<string?>? CardIds { get; set; }
}

internal sealed class SavedModuleStateDto
{
    public required SavedOwnershipModuleDto? Ownership { get; set; }
    public required SavedStatusModuleDto? Statuses { get; set; }
}

internal sealed class SavedOwnershipModuleDto
{
    public required int? Version { get; set; }
    public required List<SavedOwnershipDto?>? Entries { get; set; }
}

internal sealed class SavedOwnershipDto
{
    public required string? SpaceId { get; set; }
    public required int? OwnerPlayerId { get; set; }
}

internal sealed class SavedStatusModuleDto
{
    public required int? Version { get; set; }
    public required List<SavedStatusDto?>? Entries { get; set; }
}

internal sealed class SavedStatusDto
{
    public required int? PlayerId { get; set; }
    public required string? StatusId { get; set; }
    public required int? Value { get; set; }
}

internal sealed class SavedDiceRollDto
{
    public required string? Purpose { get; set; }
    public required List<int>? Results { get; set; }
}

internal sealed class SavedPendingDecisionDto
{
    public required Guid? DecisionId { get; set; }
    public required string? Kind { get; set; }
    public required int? PlayerId { get; set; }
    public required List<string?>? AllowedResponses { get; set; }
    public required string? SpaceId { get; set; }
    public required SavedResourceDto? Price { get; set; }
}

internal sealed class SavedContinuationDto
{
    public required int? PlayerId { get; set; }
    public required string? SpaceId { get; set; }
    public required int? NextCapabilityIndex { get; set; }
}
