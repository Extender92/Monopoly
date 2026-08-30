using System.Text;
using Monopoly.Core.Presentation;

namespace Monopoly.Core;

public enum ProfileValidationErrorKind
{
    UnsupportedSchemaVersion,
    LimitExceeded,
    DuplicateDefinition,
    BrokenReference,
    MissingPresentation,
    UnknownComponent,
    InvalidCombination,
    InvalidValue
}

public sealed class ProfileValidationException : Exception
{
    public ProfileValidationException(ProfileValidationErrorKind kind, string path, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Kind = kind;
        Path = path;
    }

    public ProfileValidationErrorKind Kind { get; }
    public string Path { get; }
}

public static class GameProfileValidator
{
    public static ValidatedGameProfile Validate(GameProfileDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.SchemaVersion != GameProfileSchema.Version1)
            throw Error(ProfileValidationErrorKind.UnsupportedSchemaVersion, "schemaVersion", $"Schema version {definition.SchemaVersion} is not supported.");
        if (definition.Track.Count > GameProfileSchema.MaximumSpaces)
            throw Limit("track", GameProfileSchema.MaximumSpaces);
        if (definition.Spaces.Count > GameProfileSchema.MaximumSpaces)
            throw Limit("spaces", GameProfileSchema.MaximumSpaces);
        if (definition.Decks.Count > GameProfileSchema.MaximumDecks)
            throw Limit("decks", GameProfileSchema.MaximumDecks);

        int cardCount = 0;
        foreach (DeckDefinition deck in definition.Decks)
        {
            cardCount = checked(cardCount + deck.Cards.Count);
            foreach (CardDefinition card in deck.Cards)
            {
                if (card.Effects.Count > GameProfileSchema.MaximumEffectsPerCard)
                    throw Limit($"decks.{deck.Id}.cards.{card.Id}.effects", GameProfileSchema.MaximumEffectsPerCard);
            }
        }
        if (cardCount > GameProfileSchema.MaximumCards)
            throw Limit("decks.cards", GameProfileSchema.MaximumCards);

        EnsurePresentationTextLimits(definition.Presentation);
        EnsureUniqueResources(definition.Resources);

        ProfileRuleGraph graph;
        try
        {
            graph = new ProfileRuleGraph(
                definition.Track,
                definition.Resources.Select(resource => resource.Id),
                definition.ProfileCapabilities,
                definition.Spaces,
                definition.Decks,
                definition.Statuses);
        }
        catch (ProfileContractException exception)
        {
            throw new ProfileValidationException(Map(exception.Kind), "ruleGraph", exception.Message, exception);
        }
        catch (ArgumentException exception)
        {
            throw new ProfileValidationException(ProfileValidationErrorKind.InvalidValue, "ruleGraph", exception.Message, exception);
        }

        EnsurePresentationReferences(definition, graph);
        EnsureSetup(definition, graph);
        EnsurePolicies(definition, graph);

        ProfileFingerprint fingerprint = ProfileFingerprintCalculator.Calculate(definition, graph);
        return new ValidatedGameProfile(definition, graph, fingerprint);
    }

    private static void EnsureUniqueResources(IReadOnlyList<ProfileResourceDefinition> resources)
    {
        HashSet<ResourceId> seen = [];
        for (int index = 0; index < resources.Count; index++)
        {
            if (!seen.Add(resources[index].Id))
                throw Error(ProfileValidationErrorKind.DuplicateDefinition, $"resources[{index}].id", $"Resource '{resources[index].Id}' is duplicated.");
        }
    }

    private static void EnsurePresentationTextLimits(ProfilePresentation presentation)
    {
        foreach (PresentationMetadata metadata in presentation.Entries)
        {
            EnsureText(metadata.DisplayText, $"presentation.{metadata.Token}.displayText");
            EnsureText(metadata.ShortText, $"presentation.{metadata.Token}.shortText");
            EnsureText(metadata.Description, $"presentation.{metadata.Token}.description");
            EnsureText(metadata.Symbol, $"presentation.{metadata.Token}.symbol");
        }
    }

    private static void EnsureText(string? value, string path)
    {
        if (value is not null && value.EnumerateRunes().Count() > GameProfileSchema.MaximumPresentationTextLength)
            throw Limit(path, GameProfileSchema.MaximumPresentationTextLength);
    }

    private static void EnsurePresentationReferences(GameProfileDefinition definition, ProfileRuleGraph graph)
    {
        IEnumerable<(PresentationToken Token, string Path)> references =
        [
            (definition.PresentationToken, "profilePresentationToken"),
            .. definition.Resources.Select((resource, index) => (resource.PresentationToken, $"resources[{index}].presentationToken")),
            .. graph.Spaces.Select((space, index) => (space.PresentationToken, $"spaces[{index}].presentationToken")),
            .. graph.Decks.SelectMany((deck, deckIndex) =>
                new[] { (deck.PresentationToken, $"decks[{deckIndex}].presentationToken") }
                    .Concat(deck.Cards.Select((card, cardIndex) => (card.PresentationToken, $"decks[{deckIndex}].cards[{cardIndex}].presentationToken")))),
            .. graph.Statuses.Select((status, index) => (status.PresentationToken, $"statuses[{index}].presentationToken"))
        ];

        foreach ((PresentationToken token, string path) in references)
        {
            if (!definition.Presentation.TryResolve(token, out _))
                throw Error(ProfileValidationErrorKind.MissingPresentation, path, $"Presentation token '{token}' is not defined by the profile.");
        }
    }

    private static void EnsureSetup(GameProfileDefinition definition, ProfileRuleGraph graph)
    {
        try
        {
            _ = graph.Track.GetIndex(definition.Setup.StartSpaceId);
        }
        catch (KeyNotFoundException exception)
        {
            throw new ProfileValidationException(ProfileValidationErrorKind.BrokenReference, "setup.startSpaceId", exception.Message, exception);
        }

        HashSet<ResourceId> declared = graph.Resources.ToHashSet();
        HashSet<ResourceId> supplied = [];
        for (int index = 0; index < definition.Setup.StartingResources.Count; index++)
        {
            ResourceAmount amount = definition.Setup.StartingResources[index];
            if (!declared.Contains(amount.ResourceId))
                throw Error(ProfileValidationErrorKind.BrokenReference, $"setup.startingResources[{index}].resourceId", $"Starting resource '{amount.ResourceId}' is not declared.");
            if (!supplied.Add(amount.ResourceId))
                throw Error(ProfileValidationErrorKind.DuplicateDefinition, $"setup.startingResources[{index}].resourceId", $"Starting resource '{amount.ResourceId}' is duplicated.");
        }

        if (!declared.SetEquals(supplied))
            throw Error(ProfileValidationErrorKind.InvalidCombination, "setup.startingResources", "Every declared resource requires exactly one starting amount.");
    }

    private static void EnsurePolicies(GameProfileDefinition definition, ProfileRuleGraph graph)
    {
        HashSet<ResourceId> resources = graph.Resources.ToHashSet();
        if (definition.Policies.PassOriginReward is { } reward && !resources.Contains(reward.ResourceId))
            throw Error(ProfileValidationErrorKind.BrokenReference, "policies.passOriginReward.resourceId", $"Pass-origin reward references missing resource '{reward.ResourceId}'.");
        if (!resources.Contains(definition.Policies.MatchEnd.ScoreResourceId))
            throw Error(ProfileValidationErrorKind.BrokenReference, "policies.matchEnd.resourceId", $"Match-end policy references missing resource '{definition.Policies.MatchEnd.ScoreResourceId}'.");
        if (definition.Policies.PassOriginReward is null && graph.Decks
            .SelectMany(deck => deck.Cards)
            .SelectMany(card => card.Effects.Entries)
            .OfType<MoveEffectDefinition>()
            .Any(effect => effect.PassOriginPolicy == PassOriginPolicy.ApplyProfileReward))
            throw Error(ProfileValidationErrorKind.InvalidCombination, "policies.passOriginReward", "A move effect requests the profile reward, but no pass-origin reward is defined.");
    }

    private static ProfileValidationException Limit(string path, int maximum) =>
        Error(ProfileValidationErrorKind.LimitExceeded, path, $"'{path}' exceeds the maximum of {maximum}.");

    private static ProfileValidationException Error(ProfileValidationErrorKind kind, string path, string message) =>
        new(kind, path, message);

    private static ProfileValidationErrorKind Map(ProfileContractErrorKind kind) => kind switch
    {
        ProfileContractErrorKind.UnknownComponent => ProfileValidationErrorKind.UnknownComponent,
        ProfileContractErrorKind.DuplicateDefinition => ProfileValidationErrorKind.DuplicateDefinition,
        ProfileContractErrorKind.BrokenReference => ProfileValidationErrorKind.BrokenReference,
        ProfileContractErrorKind.InvalidCombination => ProfileValidationErrorKind.InvalidCombination,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
