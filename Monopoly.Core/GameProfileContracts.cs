using System.Collections.ObjectModel;
using Monopoly.Core.Presentation;

namespace Monopoly.Core;

public static class GameProfileSchema
{
    public const int Version1 = 1;
    public const int MaximumInputBytes = 5 * 1024 * 1024;
    public const int MaximumJsonDepth = 64;
    public const int MaximumSpaces = 512;
    public const int MaximumDecks = 32;
    public const int MaximumCards = 2048;
    public const int MaximumEffectsPerCard = 32;
    public const int MaximumPresentationTextLength = 4096;
}

public enum StartingPlayerPolicyKind
{
    FixedOrder,
    Random,
    HighestRoll
}

public enum PurchaseDeclinePolicyKind
{
    LeaveUnowned
}

public enum MatchTieBreakPolicy
{
    LowestPlayerId
}

public sealed class ProfileResourceDefinition
{
    public ProfileResourceDefinition(ResourceId id, PresentationToken presentationToken)
    {
        if (!id.IsValid) throw new ArgumentException("The resource ID is invalid.", nameof(id));
        if (!presentationToken.IsValid) throw new ArgumentException("The presentation token is invalid.", nameof(presentationToken));
        Id = id;
        PresentationToken = presentationToken;
    }

    public ResourceId Id { get; }
    public PresentationToken PresentationToken { get; }
}

public sealed class ProfileSetupDefinition
{
    private readonly ReadOnlyCollection<ResourceAmount> _startingResources;

    public ProfileSetupDefinition(
        int minimumPlayers,
        int maximumPlayers,
        int diceCount,
        int dieSides,
        SpaceId startSpaceId,
        IEnumerable<ResourceAmount> startingResources,
        StartingPlayerPolicyKind startingPlayerPolicy)
    {
        if (minimumPlayers <= 0) throw new ArgumentOutOfRangeException(nameof(minimumPlayers));
        if (maximumPlayers < minimumPlayers) throw new ArgumentOutOfRangeException(nameof(maximumPlayers));
        if (diceCount <= 0) throw new ArgumentOutOfRangeException(nameof(diceCount));
        if (dieSides <= 1) throw new ArgumentOutOfRangeException(nameof(dieSides));
        if (!startSpaceId.IsValid) throw new ArgumentException("The start space ID is invalid.", nameof(startSpaceId));
        ArgumentNullException.ThrowIfNull(startingResources);
        if (!Enum.IsDefined(startingPlayerPolicy)) throw new ArgumentOutOfRangeException(nameof(startingPlayerPolicy));

        ResourceAmount[] resources = startingResources.ToArray();
        if (resources.Any(resource => !resource.IsValid))
            throw new ArgumentException("Every starting resource amount must be valid.", nameof(startingResources));

        MinimumPlayers = minimumPlayers;
        MaximumPlayers = maximumPlayers;
        DiceCount = diceCount;
        DieSides = dieSides;
        StartSpaceId = startSpaceId;
        _startingResources = Array.AsReadOnly(resources.OrderBy(resource => resource.ResourceId).ToArray());
        StartingPlayerPolicy = startingPlayerPolicy;
    }

    public int MinimumPlayers { get; }
    public int MaximumPlayers { get; }
    public int DiceCount { get; }
    public int DieSides { get; }
    public SpaceId StartSpaceId { get; }
    public IReadOnlyList<ResourceAmount> StartingResources => _startingResources;
    public StartingPlayerPolicyKind StartingPlayerPolicy { get; }
}

public sealed class RoundLimitedScorePolicy
{
    public RoundLimitedScorePolicy(int roundLimit, ResourceId scoreResourceId, MatchTieBreakPolicy tieBreak)
    {
        if (roundLimit <= 0) throw new ArgumentOutOfRangeException(nameof(roundLimit));
        if (!scoreResourceId.IsValid) throw new ArgumentException("The score resource ID is invalid.", nameof(scoreResourceId));
        if (!Enum.IsDefined(tieBreak)) throw new ArgumentOutOfRangeException(nameof(tieBreak));
        RoundLimit = roundLimit;
        ScoreResourceId = scoreResourceId;
        TieBreak = tieBreak;
    }

    public int RoundLimit { get; }
    public ResourceId ScoreResourceId { get; }
    public MatchTieBreakPolicy TieBreak { get; }
}

public sealed class ProfilePolicySet
{
    public ProfilePolicySet(
        ResourceAmount? passOriginReward,
        PurchaseDeclinePolicyKind purchaseDecline,
        RoundLimitedScorePolicy matchEnd)
    {
        if (passOriginReward is { IsValid: false }) throw new ArgumentException("The pass-origin reward is invalid.", nameof(passOriginReward));
        if (!Enum.IsDefined(purchaseDecline)) throw new ArgumentOutOfRangeException(nameof(purchaseDecline));
        PassOriginReward = passOriginReward;
        PurchaseDecline = purchaseDecline;
        MatchEnd = matchEnd ?? throw new ArgumentNullException(nameof(matchEnd));
    }

    public ResourceAmount? PassOriginReward { get; }
    public PurchaseDeclinePolicyKind PurchaseDecline { get; }
    public RoundLimitedScorePolicy MatchEnd { get; }
}

public sealed class GameProfileDefinition
{
    private readonly ReadOnlyCollection<ProfileResourceDefinition> _resources;
    private readonly ReadOnlyCollection<SpaceDefinition> _spaces;
    private readonly ReadOnlyCollection<DeckDefinition> _decks;
    private readonly ReadOnlyCollection<StatusDefinition> _statuses;

    public GameProfileDefinition(
        int schemaVersion,
        ProfileId id,
        ProfileRevision revision,
        PresentationToken presentationToken,
        ProfilePresentation presentation,
        IEnumerable<ProfileResourceDefinition> resources,
        ProfileSetupDefinition setup,
        GameTrack track,
        CapabilitySet profileCapabilities,
        IEnumerable<SpaceDefinition> spaces,
        IEnumerable<DeckDefinition> decks,
        IEnumerable<StatusDefinition> statuses,
        ProfilePolicySet policies)
    {
        if (!id.IsValid) throw new ArgumentException("The profile ID is invalid.", nameof(id));
        if (!revision.IsValid) throw new ArgumentException("The profile revision is invalid.", nameof(revision));
        if (!presentationToken.IsValid) throw new ArgumentException("The profile presentation token is invalid.", nameof(presentationToken));
        Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        ArgumentNullException.ThrowIfNull(resources);
        Setup = setup ?? throw new ArgumentNullException(nameof(setup));
        Track = track ?? throw new ArgumentNullException(nameof(track));
        ProfileCapabilities = profileCapabilities ?? throw new ArgumentNullException(nameof(profileCapabilities));
        ArgumentNullException.ThrowIfNull(spaces);
        ArgumentNullException.ThrowIfNull(decks);
        ArgumentNullException.ThrowIfNull(statuses);
        Policies = policies ?? throw new ArgumentNullException(nameof(policies));

        ProfileResourceDefinition[] resourceEntries = resources.ToArray();
        SpaceDefinition[] spaceEntries = spaces.ToArray();
        DeckDefinition[] deckEntries = decks.ToArray();
        StatusDefinition[] statusEntries = statuses.ToArray();
        if (resourceEntries.Any(entry => entry is null) || spaceEntries.Any(entry => entry is null) ||
            deckEntries.Any(entry => entry is null) || statusEntries.Any(entry => entry is null))
            throw new ArgumentException("Profile collections cannot contain null entries.");

        SchemaVersion = schemaVersion;
        Id = id;
        Revision = revision;
        PresentationToken = presentationToken;
        _resources = Array.AsReadOnly(resourceEntries);
        _spaces = Array.AsReadOnly(spaceEntries);
        _decks = Array.AsReadOnly(deckEntries);
        _statuses = Array.AsReadOnly(statusEntries);
    }

    public int SchemaVersion { get; }
    public ProfileId Id { get; }
    public ProfileRevision Revision { get; }
    public PresentationToken PresentationToken { get; }
    public ProfilePresentation Presentation { get; }
    public IReadOnlyList<ProfileResourceDefinition> Resources => _resources;
    public ProfileSetupDefinition Setup { get; }
    public GameTrack Track { get; }
    public CapabilitySet ProfileCapabilities { get; }
    public IReadOnlyList<SpaceDefinition> Spaces => _spaces;
    public IReadOnlyList<DeckDefinition> Decks => _decks;
    public IReadOnlyList<StatusDefinition> Statuses => _statuses;
    public ProfilePolicySet Policies { get; }
}

public sealed class ValidatedGameProfile
{
    private readonly ReadOnlyCollection<ProfileResourceDefinition> _resources;

    internal ValidatedGameProfile(GameProfileDefinition definition, ProfileRuleGraph ruleGraph, ProfileFingerprint fingerprint)
    {
        SchemaVersion = definition.SchemaVersion;
        Id = definition.Id;
        Revision = definition.Revision;
        Fingerprint = fingerprint;
        PresentationToken = definition.PresentationToken;
        Presentation = definition.Presentation;
        _resources = Array.AsReadOnly(definition.Resources.OrderBy(resource => resource.Id).ToArray());
        Setup = definition.Setup;
        RuleGraph = ruleGraph;
        Policies = definition.Policies;
    }

    public int SchemaVersion { get; }
    public ProfileId Id { get; }
    public ProfileRevision Revision { get; }
    public ProfileFingerprint Fingerprint { get; }
    public PresentationToken PresentationToken { get; }
    public ProfilePresentation Presentation { get; }
    public IReadOnlyList<ProfileResourceDefinition> Resources => _resources;
    public ProfileSetupDefinition Setup { get; }
    public ProfileRuleGraph RuleGraph { get; }
    public ProfilePolicySet Policies { get; }
}
