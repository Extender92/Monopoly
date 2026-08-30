namespace Monopoly.Core;

using System.Collections.ObjectModel;

internal delegate void CapabilityExecutionHandler(ProfileExecutionContext context, CapabilityDefinition definition, int capabilityIndex);
internal delegate void EffectExecutionHandler(ProfileExecutionContext context, EffectDefinition definition, string path);
internal delegate ProfilePolicyResult PurchasePolicyExecutionHandler(ProfileExecutionContext context, PurchaseNonPurchaseReason reason);
internal delegate void PolicyCapabilityExecutionHandler(ProfileExecutionContext context);

internal enum PurchaseNonPurchaseReason
{
    Declined,
    InsufficientResources
}

internal enum ProfilePolicyResultKind
{
    Continue,
    RequestCapability
}

internal readonly record struct ProfilePolicyResult
{
    private ProfilePolicyResult(ProfilePolicyResultKind kind, CapabilityId requestedCapabilityId)
    {
        Kind = kind;
        RequestedCapabilityId = requestedCapabilityId;
    }

    internal static ProfilePolicyResult Continue { get; } = new(ProfilePolicyResultKind.Continue, default);

    internal static ProfilePolicyResult RequestCapability(CapabilityId capabilityId)
    {
        if (!capabilityId.IsValid)
            throw new ArgumentException("The requested capability ID is invalid.", nameof(capabilityId));
        return new ProfilePolicyResult(ProfilePolicyResultKind.RequestCapability, capabilityId);
    }

    internal ProfilePolicyResultKind Kind { get; }
    internal CapabilityId RequestedCapabilityId { get; }
}

internal sealed class PurchasePolicyRegistration
{
    private readonly ReadOnlyCollection<CapabilityId> _possibleCapabilityRequests;

    internal PurchasePolicyRegistration(
        PurchaseDeclinePolicyKind policy,
        PurchasePolicyExecutionHandler handler,
        IEnumerable<CapabilityId> possibleCapabilityRequests)
    {
        if (!Enum.IsDefined(policy)) throw new ArgumentOutOfRangeException(nameof(policy));
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        ArgumentNullException.ThrowIfNull(possibleCapabilityRequests);

        CapabilityId[] requests = possibleCapabilityRequests.ToArray();
        if (requests.Any(id => !id.IsValid) || requests.Distinct().Count() != requests.Length)
            throw new ArgumentException("Possible policy capability requests must contain unique valid IDs.", nameof(possibleCapabilityRequests));

        Policy = policy;
        _possibleCapabilityRequests = Array.AsReadOnly(requests.OrderBy(id => id).ToArray());
    }

    internal PurchaseDeclinePolicyKind Policy { get; }
    internal PurchasePolicyExecutionHandler Handler { get; }
    internal IReadOnlyList<CapabilityId> PossibleCapabilityRequests => _possibleCapabilityRequests;

    internal static PurchasePolicyRegistration LeaveUnowned() => new(
        PurchaseDeclinePolicyKind.LeaveUnowned,
        static (context, _) =>
        {
            context.LeaveCurrentSpaceUnowned();
            return ProfilePolicyResult.Continue;
        },
        []);
}

internal sealed class PolicyCapabilityRegistration
{
    internal PolicyCapabilityRegistration(CapabilityId id, PolicyCapabilityExecutionHandler handler)
    {
        if (!id.IsValid) throw new ArgumentException("The policy capability ID is invalid.", nameof(id));
        Id = id;
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    internal CapabilityId Id { get; }
    internal PolicyCapabilityExecutionHandler Handler { get; }
}

/// <summary>The single trusted setup and execution registry for the public baseline.</summary>
internal sealed class ProfileComponentRegistry
{
    private static readonly CapabilityId[] LandingOrder =
    [
        CapabilityKinds.Ownable,
        CapabilityKinds.Purchasable,
        CapabilityKinds.UsageFee,
        CapabilityKinds.Draw
    ];

    private readonly Dictionary<CapabilityId, CapabilityExecutionHandler> _capabilities;
    private readonly Dictionary<EffectKindId, EffectExecutionHandler> _effects;
    private readonly HashSet<StatusId> _statuses;
    private readonly HashSet<StartingPlayerPolicyKind> _startingPlayerPolicies;
    private readonly Dictionary<PurchaseDeclinePolicyKind, PurchasePolicyRegistration> _purchaseDeclinePolicies;
    private readonly Dictionary<CapabilityId, PolicyCapabilityExecutionHandler> _policyCapabilities;
    private readonly Dictionary<MatchTieBreakPolicy, Func<ProfileExecutionContext, ResourceId, int>> _matchTieBreakPolicies;
    private readonly bool _supportsRoundLimitedScore;

    internal ProfileComponentRegistry(
        IEnumerable<CapabilityId> capabilities,
        IEnumerable<EffectKindId> effects,
        IEnumerable<StatusId> statuses,
        IEnumerable<StartingPlayerPolicyKind> startingPlayerPolicies,
        IEnumerable<PurchasePolicyRegistration> purchaseDeclinePolicies,
        IEnumerable<PolicyCapabilityRegistration> policyCapabilities,
        IEnumerable<MatchTieBreakPolicy> matchTieBreakPolicies,
        bool supportsRoundLimitedScore)
    {
        _capabilities = (capabilities ?? throw new ArgumentNullException(nameof(capabilities)))
            .Distinct()
            .ToDictionary(id => id, CreateCapabilityHandler);
        _effects = (effects ?? throw new ArgumentNullException(nameof(effects)))
            .Distinct()
            .ToDictionary(id => id, CreateEffectHandler);
        _statuses = new HashSet<StatusId>(statuses ?? throw new ArgumentNullException(nameof(statuses)));
        _startingPlayerPolicies = new HashSet<StartingPlayerPolicyKind>(startingPlayerPolicies ?? throw new ArgumentNullException(nameof(startingPlayerPolicies)));
        _purchaseDeclinePolicies = (purchaseDeclinePolicies ?? throw new ArgumentNullException(nameof(purchaseDeclinePolicies)))
            .ToDictionary(registration => registration.Policy);
        _policyCapabilities = (policyCapabilities ?? throw new ArgumentNullException(nameof(policyCapabilities)))
            .ToDictionary(registration => registration.Id, registration => registration.Handler);
        _matchTieBreakPolicies = (matchTieBreakPolicies ?? throw new ArgumentNullException(nameof(matchTieBreakPolicies)))
            .Distinct()
            .ToDictionary(policy => policy, CreateTieBreakHandler);
        _supportsRoundLimitedScore = supportsRoundLimitedScore;
    }

    internal static ProfileComponentRegistry CreateExecutionBaseline() => new(
        [CapabilityKinds.Move, CapabilityKinds.Ownable, CapabilityKinds.Purchasable, CapabilityKinds.UsageFee, CapabilityKinds.Draw],
        [EffectKinds.Move, EffectKinds.ResourceChange],
        [],
        [StartingPlayerPolicyKind.FixedOrder, StartingPlayerPolicyKind.Random, StartingPlayerPolicyKind.HighestRoll],
        [PurchasePolicyRegistration.LeaveUnowned()],
        [],
        [MatchTieBreakPolicy.LowestPlayerId],
        supportsRoundLimitedScore: true);

    internal IReadOnlyList<CapabilityDefinition> OrderLandingCapabilities(CapabilitySet capabilities) =>
        LandingOrder
            .Select(id => capabilities.ById.TryGetValue(id, out CapabilityDefinition? definition) ? definition : null)
            .Where(definition => definition is not null)
            .Cast<CapabilityDefinition>()
            .ToArray();

    internal void ExecuteCapability(ProfileExecutionContext context, CapabilityDefinition definition, int capabilityIndex) =>
        _capabilities[definition.Id](context, definition, capabilityIndex);

    internal void ExecuteEffect(ProfileExecutionContext context, EffectDefinition definition, string path) =>
        _effects[definition.Kind](context, definition, path);

    internal void ExecutePassOriginReward(ProfileExecutionContext context, int originPasses, string path) =>
        context.ApplyConfiguredOriginReward(originPasses, path);

    internal void ExecutePurchaseNonPurchase(
        ProfileExecutionContext context,
        PurchaseDeclinePolicyKind policy,
        PurchaseNonPurchaseReason reason)
    {
        PurchasePolicyRegistration registration = _purchaseDeclinePolicies[policy];
        ProfilePolicyResult result = registration.Handler(context, reason);
        if (result.Kind == ProfilePolicyResultKind.Continue)
            return;

        CapabilityId request = result.RequestedCapabilityId;
        if (!request.IsValid || !registration.PossibleCapabilityRequests.Contains(request))
        {
            throw new ProfileExecutionException(
                ProfileExecutionErrorKind.UnsupportedExecutionShape,
                "policy.purchase-decline.result",
                $"Purchase policy '{policy}' returned undeclared capability request '{request}'.");
        }
        if (!_policyCapabilities.TryGetValue(request, out PolicyCapabilityExecutionHandler? handler))
        {
            throw new ProfileExecutionException(
                ProfileExecutionErrorKind.UnsupportedExecutionShape,
                "policy.purchase-decline.result.capabilityId",
                $"Requested capability '{request}' is not registered for policy execution.");
        }

        handler(context);
    }

    internal int SelectRoundLimitedWinner(ProfileExecutionContext context, RoundLimitedScorePolicy policy)
    {
        if (!_supportsRoundLimitedScore)
            throw new InvalidOperationException("The round-limited score handler is not registered.");
        return _matchTieBreakPolicies[policy.TieBreak](context, policy.ScoreResourceId);
    }

    internal void Validate(ValidatedGameProfile profile)
    {
        for (int index = 0; index < profile.RuleGraph.ProfileCapabilities.Entries.Count; index++)
            EnsureCapability(profile.RuleGraph.ProfileCapabilities.Entries[index].Id, $"profile.profileCapabilities[{index}].kind");

        for (int spaceIndex = 0; spaceIndex < profile.RuleGraph.Spaces.Count; spaceIndex++)
        {
            SpaceDefinition space = profile.RuleGraph.Spaces[spaceIndex];
            for (int capabilityIndex = 0; capabilityIndex < space.Capabilities.Entries.Count; capabilityIndex++)
                EnsureCapability(space.Capabilities.Entries[capabilityIndex].Id, $"profile.spaces[{spaceIndex}].capabilities[{capabilityIndex}].kind");
        }

        for (int deckIndex = 0; deckIndex < profile.RuleGraph.Decks.Count; deckIndex++)
        {
            DeckDefinition deck = profile.RuleGraph.Decks[deckIndex];
            for (int cardIndex = 0; cardIndex < deck.Cards.Count; cardIndex++)
            {
                CardDefinition card = deck.Cards[cardIndex];
                MoveEffectDefinition[] moves = card.Effects.Entries.OfType<MoveEffectDefinition>().ToArray();
                if (moves.Length > 1)
                    throw Unsupported($"profile.decks[{deckIndex}].cards[{cardIndex}].effects", "The baseline supports at most one movement effect per card.");

                for (int effectIndex = 0; effectIndex < card.Effects.Entries.Count; effectIndex++)
                {
                    EffectDefinition effect = card.Effects.Entries[effectIndex];
                    if (!_effects.ContainsKey(effect.Kind))
                        throw Unsupported($"profile.decks[{deckIndex}].cards[{cardIndex}].effects[{effectIndex}].kind", $"Effect '{effect.Kind}' is not registered for execution.");
                    if (effect is MoveEffectDefinition { ResolveDestination: true } && effectIndex != card.Effects.Entries.Count - 1)
                        throw Unsupported($"profile.decks[{deckIndex}].cards[{cardIndex}].effects[{effectIndex}]", "A destination-resolving movement effect must be the final baseline effect.");
                }
            }
        }

        for (int index = 0; index < profile.RuleGraph.Statuses.Count; index++)
        {
            StatusDefinition status = profile.RuleGraph.Statuses[index];
            if (!_statuses.Contains(status.Id))
                throw Unsupported($"profile.statuses[{index}].id", $"Status '{status.Id}' is not registered for execution.");
        }

        if (!_startingPlayerPolicies.Contains(profile.Setup.StartingPlayerPolicy))
            throw UnsupportedPolicy("profile.setup.startingPlayerPolicy", profile.Setup.StartingPlayerPolicy);
        if (!_purchaseDeclinePolicies.ContainsKey(profile.Policies.PurchaseDecline))
            throw UnsupportedPolicy("profile.policies.purchaseDecline", profile.Policies.PurchaseDecline);
        foreach (CapabilityId request in _purchaseDeclinePolicies[profile.Policies.PurchaseDecline].PossibleCapabilityRequests)
        {
            if (!_policyCapabilities.ContainsKey(request))
            {
                throw Unsupported(
                    "profile.policies.purchaseDecline",
                    $"Purchase policy '{profile.Policies.PurchaseDecline}' can request capability '{request}', but it is not registered for policy execution.");
            }
        }
        if (!_supportsRoundLimitedScore)
            throw new GameSetupException(GameSetupErrorKind.UnsupportedPolicy, "profile.policies.matchEnd", "The round-limited score policy is not registered for execution.");
        if (!_matchTieBreakPolicies.ContainsKey(profile.Policies.MatchEnd.TieBreak))
            throw UnsupportedPolicy("profile.policies.matchEnd.tieBreak", profile.Policies.MatchEnd.TieBreak);

        ValidateNoNestedDrawDestinations(profile);
    }

    private void ValidateNoNestedDrawDestinations(ValidatedGameProfile profile)
    {
        Dictionary<DeckId, DeckDefinition> decks = profile.RuleGraph.Decks.ToDictionary(deck => deck.Id);
        Dictionary<SpaceId, SpaceDefinition> spaces = profile.RuleGraph.Spaces.ToDictionary(space => space.Id);
        foreach (SpaceDefinition source in profile.RuleGraph.Spaces)
        {
            DrawCapabilityDefinition? draw = source.Capabilities.Find<DrawCapabilityDefinition>();
            if (draw is null) continue;
            int sourceIndex = profile.RuleGraph.Track.GetIndex(source.Id);
            foreach (CardDefinition card in decks[draw.DeckId].Cards)
            {
                MoveEffectDefinition? move = card.Effects.Entries.OfType<MoveEffectDefinition>().SingleOrDefault();
                if (move is not { ResolveDestination: true }) continue;
                SpaceId target = move.Target switch
                {
                    RelativeMoveTarget relative => profile.RuleGraph.Track.GetSpaceIdAt(
                        profile.RuleGraph.Track.NormalizeIndex((long)sourceIndex + relative.Offset)),
                    AbsoluteMoveTarget absolute => absolute.SpaceId,
                    _ => throw new InvalidOperationException("The validated movement target is unsupported.")
                };
                if (spaces[target].Capabilities.Contains(CapabilityKinds.Draw))
                    throw Unsupported($"profile.decks[{draw.DeckId}].cards[{card.Id}]", "Nested draw destinations are deferred to issue #36.");
            }
        }
    }

    private void EnsureCapability(CapabilityId id, string path)
    {
        if (!_capabilities.ContainsKey(id))
            throw Unsupported(path, $"Capability '{id}' is not registered for execution.");
    }

    private static CapabilityExecutionHandler CreateCapabilityHandler(CapabilityId id)
    {
        if (id == CapabilityKinds.Move || id == CapabilityKinds.Ownable)
            return static (_, _, _) => { };
        if (id == CapabilityKinds.Purchasable)
            return static (context, definition, index) => context.ApplyPurchasable((PurchasableCapabilityDefinition)definition, index);
        if (id == CapabilityKinds.UsageFee)
            return static (context, definition, _) => context.ApplyUsageFee((UsageFeeCapabilityDefinition)definition);
        if (id == CapabilityKinds.Draw)
            return static (context, definition, _) => context.ApplyDraw((DrawCapabilityDefinition)definition);
        throw new ArgumentException($"Capability '{id}' has no trusted handler.", nameof(id));
    }

    private static EffectExecutionHandler CreateEffectHandler(EffectKindId id)
    {
        if (id == EffectKinds.Move)
            return static (context, definition, path) => context.ApplyMoveEffect((MoveEffectDefinition)definition, path);
        if (id == EffectKinds.ResourceChange)
            return static (context, definition, path) => context.ApplyResourceChange((ResourceChangeEffectDefinition)definition, path);
        throw new ArgumentException($"Effect '{id}' has no trusted handler.", nameof(id));
    }

    private static Func<ProfileExecutionContext, ResourceId, int> CreateTieBreakHandler(MatchTieBreakPolicy policy) =>
        policy == MatchTieBreakPolicy.LowestPlayerId
            ? static (context, scoreResourceId) => context.SelectHighestResourceWinner(scoreResourceId)
            : throw new ArgumentException($"Match tie-break policy '{policy}' has no trusted handler.", nameof(policy));

    private static GameSetupException Unsupported(string path, string message) =>
        new(GameSetupErrorKind.UnsupportedComponent, path, message);

    private static GameSetupException UnsupportedPolicy<T>(string path, T policy) where T : struct, Enum =>
        new(GameSetupErrorKind.UnsupportedPolicy, path, $"Policy '{policy}' is not registered for execution.");
}
