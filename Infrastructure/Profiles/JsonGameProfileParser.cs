using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Monopoly.Core;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Presentation;

namespace Infrastructure.Profiles;

public sealed class JsonGameProfileParser
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = false,
        MaxDepth = GameProfileSchema.MaximumJsonDepth,
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public ValidatedGameProfile Parse(ReadOnlyMemory<byte> input)
    {
        if (input.Length > GameProfileSchema.MaximumInputBytes)
            throw JsonError(ProfileJsonErrorKind.InputTooLarge, "$", $"Profile input exceeds {GameProfileSchema.MaximumInputBytes} bytes.");
        if (input.IsEmpty)
            throw JsonError(ProfileJsonErrorKind.MalformedJson, "$", "Profile input is empty.");

        ReadOnlySpan<byte> json = input.Span;
        if (json.StartsWith(new byte[] { 0xef, 0xbb, 0xbf })) json = json[3..];
        if (json.StartsWith(new byte[] { 0xff, 0xfe }) || json.StartsWith(new byte[] { 0xfe, 0xff }))
            throw JsonError(ProfileJsonErrorKind.InvalidEncoding, "$", "Profile input must use UTF-8.");
        if (json.Length >= 4 &&
            ((json[0] != 0 && json[1] == 0 && json[3] == 0) ||
             (json[0] == 0 && json[1] != 0 && json[2] == 0)))
            throw JsonError(ProfileJsonErrorKind.InvalidEncoding, "$", "Profile input appears to use UTF-16; UTF-8 is required.");

        try
        {
            _ = StrictUtf8.GetCharCount(json);
        }
        catch (DecoderFallbackException exception)
        {
            throw JsonError(ProfileJsonErrorKind.InvalidEncoding, "$", "Profile input contains invalid UTF-8.", exception);
        }

        RejectDuplicateMembers(json);

        ProfileDocumentDto document;
        try
        {
            document = JsonSerializer.Deserialize<ProfileDocumentDto>(json, JsonOptions)
                ?? throw JsonError(ProfileJsonErrorKind.MalformedJson, "$", "Profile input must contain one JSON object.");
        }
        catch (ProfileJsonException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw TranslateJsonException(exception);
        }

        if (document.SchemaVersion is null)
            throw JsonError(ProfileJsonErrorKind.InvalidWireValue, "schemaVersion", "The required schemaVersion field is missing.");
        if (document.SchemaVersion != GameProfileSchema.Version1)
            throw JsonError(ProfileJsonErrorKind.UnsupportedSchemaVersion, "schemaVersion", $"Schema version {document.SchemaVersion} is not supported.");

        return Map(document);
    }

    public ValidatedGameProfile Parse(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.CanRead) throw JsonError(ProfileJsonErrorKind.InvalidWireValue, "$", "The profile stream is not readable.");

        using MemoryStream buffer = new();
        byte[] chunk = new byte[81920];
        while (true)
        {
            int read;
            try
            {
                read = input.Read(chunk, 0, Math.Min(chunk.Length, GameProfileSchema.MaximumInputBytes + 1 - checked((int)buffer.Length)));
            }
            catch (Exception exception) when (exception is IOException or NotSupportedException or ObjectDisposedException)
            {
                throw JsonError(ProfileJsonErrorKind.InvalidWireValue, "$", "The profile stream could not be read.", exception);
            }
            if (read == 0) break;
            buffer.Write(chunk, 0, read);
            if (buffer.Length > GameProfileSchema.MaximumInputBytes)
                throw JsonError(ProfileJsonErrorKind.InputTooLarge, "$", $"Profile input exceeds {GameProfileSchema.MaximumInputBytes} bytes.");
        }

        return Parse(buffer.ToArray());
    }

    private static ValidatedGameProfile Map(ProfileDocumentDto document)
    {
        try
        {
            ProfilePresentation presentation = MapPresentation(Required(document.Presentation, "presentation"));
            ProfileResourceDefinition[] resources = Required(document.Resources, "resources")
                .Select((resource, index) =>
                {
                    if (resource is null) throw Validation(ProfileValidationErrorKind.InvalidValue, $"resources[{index}]", "Resources cannot be null.");
                    return new ProfileResourceDefinition(
                        Id<ResourceId>(resource.Id, $"resources[{index}].id", value => new ResourceId(value)),
                        Token(resource.PresentationToken, $"resources[{index}].presentationToken"));
                })
                .ToArray();
            ProfileSetupDefinition setup = MapSetup(Required(document.Setup, "setup"));
            GameTrack track = MapTrack(Required(document.Track, "track"));
            CapabilitySet profileCapabilities = MapCapabilities(Required(document.ProfileCapabilities, "profileCapabilities"), "profileCapabilities");
            SpaceDefinition[] spaces = Required(document.Spaces, "spaces")
                .Select((space, index) =>
                {
                    if (space is null) throw Validation(ProfileValidationErrorKind.InvalidValue, $"spaces[{index}]", "Spaces cannot be null.");
                    return new SpaceDefinition(
                        Id<SpaceId>(space.Id, $"spaces[{index}].id", value => new SpaceId(value)),
                        Token(space.PresentationToken, $"spaces[{index}].presentationToken"),
                        MapCapabilities(Required(space.Capabilities, $"spaces[{index}].capabilities"), $"spaces[{index}].capabilities"));
                })
                .ToArray();
            DeckDefinition[] decks = MapDecks(Required(document.Decks, "decks"));
            StatusDefinition[] statuses = Required(document.Statuses, "statuses")
                .Select((status, index) =>
                {
                    if (status is null) throw Validation(ProfileValidationErrorKind.InvalidValue, $"statuses[{index}]", "Statuses cannot be null.");
                    return new StatusDefinition(
                        Id<StatusId>(status.Id, $"statuses[{index}].id", value => new StatusId(value)),
                        Token(status.PresentationToken, $"statuses[{index}].presentationToken"),
                        Required(status.MaximumValue, $"statuses[{index}].maximumValue"));
                })
                .ToArray();
            ProfilePolicySet policies = MapPolicies(Required(document.Policies, "policies"));

            GameProfileDefinition definition = new(
                Required(document.SchemaVersion, "schemaVersion"),
                Id<ProfileId>(document.ProfileId, "profileId", value => new ProfileId(value)),
                Construct("revision", () => new ProfileRevision(Required(document.Revision, "revision"))),
                Token(document.ProfilePresentationToken, "profilePresentationToken"),
                presentation,
                resources,
                setup,
                track,
                profileCapabilities,
                spaces,
                decks,
                statuses,
                policies);

            return GameProfileValidator.Validate(definition);
        }
        catch (ProfileValidationException)
        {
            throw;
        }
        catch (ProfileContractException exception)
        {
            throw new ProfileValidationException(Map(exception.Kind), "profile", exception.Message, exception);
        }
        catch (ArgumentException exception)
        {
            throw new ProfileValidationException(ProfileValidationErrorKind.InvalidValue, "profile", exception.Message, exception);
        }
    }

    private static ProfilePresentation MapPresentation(IReadOnlyList<PresentationDto> entries)
    {
        List<PresentationMetadata> metadata = [];
        HashSet<PresentationToken> seen = [];
        for (int index = 0; index < entries.Count; index++)
        {
            PresentationDto entry = entries[index] ?? throw Validation(ProfileValidationErrorKind.InvalidValue, $"presentation[{index}]", "Presentation entries cannot be null.");
            PresentationToken token = Token(entry.Token, $"presentation[{index}].token");
            if (!seen.Add(token))
                throw Validation(ProfileValidationErrorKind.DuplicateDefinition, $"presentation[{index}].token", $"Presentation token '{token}' is duplicated.");
            metadata.Add(Construct($"presentation[{index}]", () => new PresentationMetadata(
                token,
                entry.DisplayText,
                entry.ShortText,
                entry.Description,
                entry.Symbol,
                OptionalToken(entry.ColorToken, $"presentation[{index}].colorToken"),
                OptionalToken(entry.LayoutToken, $"presentation[{index}].layoutToken"))));
        }

        try
        {
            return new ProfilePresentation(metadata);
        }
        catch (ArgumentException exception)
        {
            throw new ProfileValidationException(ProfileValidationErrorKind.BrokenReference, "presentation", exception.Message, exception);
        }
    }

    private static ProfileSetupDefinition MapSetup(SetupDto setup) => Construct("setup", () => new ProfileSetupDefinition(
        Required(setup.MinimumPlayers, "setup.minimumPlayers"),
        Required(setup.MaximumPlayers, "setup.maximumPlayers"),
        Required(setup.DiceCount, "setup.diceCount"),
        Required(setup.DieSides, "setup.dieSides"),
        Id<SpaceId>(setup.StartSpaceId, "setup.startSpaceId", value => new SpaceId(value)),
        Required(setup.StartingResources, "setup.startingResources").Select((amount, index) =>
            MapAmount(amount, $"setup.startingResources[{index}]")),
        ParseStartingPlayerPolicy(setup.StartingPlayerPolicy, "setup.startingPlayerPolicy")));

    private static GameTrack MapTrack(IReadOnlyList<string> ids)
    {
        HashSet<SpaceId> seen = [];
        SpaceId[] mapped = ids.Select((id, index) =>
        {
            SpaceId spaceId = Id<SpaceId>(id, $"track[{index}]", value => new SpaceId(value));
            if (!seen.Add(spaceId))
                throw Validation(ProfileValidationErrorKind.DuplicateDefinition, $"track[{index}]", $"Space ID '{spaceId}' is duplicated.");
            return spaceId;
        }).ToArray();
        return Construct("track", () => new GameTrack(mapped));
    }

    private static CapabilitySet MapCapabilities(IReadOnlyList<CapabilityDto> entries, string path) =>
        Construct(path, () => new CapabilitySet(entries.Select((entry, index) => MapCapability(entry, $"{path}[{index}]"))));

    private static CapabilityDefinition MapCapability(CapabilityDto capability, string path)
    {
        if (capability is null) throw Validation(ProfileValidationErrorKind.InvalidValue, path, "Capabilities cannot be null.");
        string kind = Required(capability.Kind, $"{path}.kind");
        return kind switch
        {
            "move" => WithoutCapabilityPayload(capability, path, () => new MoveCapabilityDefinition()),
            "ownable" => WithCapabilityPayload(capability, path, allowGroupId: true, create: () =>
                new OwnableCapabilityDefinition(capability.GroupId is null ? null : new GroupId(capability.GroupId))),
            "purchasable" => WithCapabilityPayload(capability, path, allowPrice: true, create: () =>
                new PurchasableCapabilityDefinition(MapAmount(Required(capability.Price, $"{path}.price"), $"{path}.price"))),
            "usage-fee" => WithCapabilityPayload(capability, path, allowAmount: true, create: () =>
                new UsageFeeCapabilityDefinition(MapAmount(Required(capability.Amount, $"{path}.amount"), $"{path}.amount"))),
            "draw" => WithCapabilityPayload(capability, path, allowDeckId: true, create: () =>
                new DrawCapabilityDefinition(Id<DeckId>(capability.DeckId, $"{path}.deckId", value => new DeckId(value)))),
            _ => throw Validation(ProfileValidationErrorKind.UnknownComponent, $"{path}.kind", $"Capability '{kind}' is not supported by schema version 1.")
        };
    }

    private static T WithoutCapabilityPayload<T>(CapabilityDto capability, string path, Func<T> create) where T : CapabilityDefinition =>
        WithCapabilityPayload(capability, path, create: create);

    private static T WithCapabilityPayload<T>(
        CapabilityDto capability,
        string path,
        bool allowGroupId = false,
        bool allowPrice = false,
        bool allowAmount = false,
        bool allowDeckId = false,
        Func<T>? create = null) where T : CapabilityDefinition
    {
        if ((!allowGroupId && capability.GroupId is not null) || (!allowPrice && capability.Price is not null) ||
            (!allowAmount && capability.Amount is not null) || (!allowDeckId && capability.DeckId is not null))
            throw Validation(ProfileValidationErrorKind.InvalidCombination, path, $"Capability '{capability.Kind}' contains fields for another capability kind.");
        return Construct(path, create ?? throw new ArgumentNullException(nameof(create)));
    }

    private static DeckDefinition[] MapDecks(IReadOnlyList<DeckDto> entries) => entries.Select((deck, deckIndex) =>
    {
        if (deck is null) throw Validation(ProfileValidationErrorKind.InvalidValue, $"decks[{deckIndex}]", "Decks cannot be null.");
        CardDefinition[] cards = Required(deck.Cards, $"decks[{deckIndex}].cards").Select((card, cardIndex) =>
        {
            if (card is null) throw Validation(ProfileValidationErrorKind.InvalidValue, $"decks[{deckIndex}].cards[{cardIndex}]", "Cards cannot be null.");
            return new CardDefinition(
                Id<CardId>(card.Id, $"decks[{deckIndex}].cards[{cardIndex}].id", value => new CardId(value)),
                Token(card.PresentationToken, $"decks[{deckIndex}].cards[{cardIndex}].presentationToken"),
                new EffectSequence(Required(card.Effects, $"decks[{deckIndex}].cards[{cardIndex}].effects")
                    .Select((effect, effectIndex) => MapEffect(effect, $"decks[{deckIndex}].cards[{cardIndex}].effects[{effectIndex}]"))));
        }).ToArray();
        return Construct($"decks[{deckIndex}]", () => new DeckDefinition(
            Id<DeckId>(deck.Id, $"decks[{deckIndex}].id", value => new DeckId(value)),
            Token(deck.PresentationToken, $"decks[{deckIndex}].presentationToken"),
            cards));
    }).ToArray();

    private static EffectDefinition MapEffect(EffectDto effect, string path)
    {
        if (effect is null) throw Validation(ProfileValidationErrorKind.InvalidValue, path, "Effects cannot be null.");
        string kind = Required(effect.Kind, $"{path}.kind");
        return kind switch
        {
            "move" => MapMoveEffect(effect, path),
            "resource-change" => WithEffectPayload(effect, path, allowResourceId: true, allowDelta: true, create: () =>
                new ResourceChangeEffectDefinition(
                    Id<ResourceId>(effect.ResourceId, $"{path}.resourceId", value => new ResourceId(value)),
                    Required(effect.Delta, $"{path}.delta"))),
            "status" => WithEffectPayload(effect, path, allowStatusId: true, allowOperation: true, allowValue: true, create: () =>
                new StatusEffectDefinition(
                    Id<StatusId>(effect.StatusId, $"{path}.statusId", value => new StatusId(value)),
                    ParseStatusOperation(effect.Operation, $"{path}.operation"),
                    Required(effect.Value, $"{path}.value"))),
            _ => throw Validation(ProfileValidationErrorKind.UnknownComponent, $"{path}.kind", $"Effect '{kind}' is not supported by schema version 1.")
        };
    }

    private static EffectDefinition MapMoveEffect(EffectDto effect, string path) => WithEffectPayload(
        effect,
        path,
        allowTarget: true,
        allowPassOriginPolicy: true,
        allowResolveDestination: true,
        create: () => new MoveEffectDefinition(
            MapMoveTarget(Required(effect.Target, $"{path}.target"), $"{path}.target"),
            ParsePassOriginPolicy(effect.PassOriginPolicy, $"{path}.passOriginPolicy"),
            Required(effect.ResolveDestination, $"{path}.resolveDestination")));

    private static MoveTarget MapMoveTarget(MoveTargetDto target, string path)
    {
        string kind = Required(target.Kind, $"{path}.kind");
        return kind switch
        {
            "relative" when target.Offset is not null && target.SpaceId is null =>
                Construct(path, () => new RelativeMoveTarget(target.Offset.Value)),
            "absolute" when target.Offset is null && target.SpaceId is not null =>
                new AbsoluteMoveTarget(Id<SpaceId>(target.SpaceId, $"{path}.spaceId", value => new SpaceId(value))),
            "relative" or "absolute" => throw Validation(ProfileValidationErrorKind.InvalidCombination, path, $"Move target '{kind}' has inconsistent fields."),
            _ => throw Validation(ProfileValidationErrorKind.UnknownComponent, $"{path}.kind", $"Move target '{kind}' is not supported by schema version 1.")
        };
    }

    private static T WithEffectPayload<T>(
        EffectDto effect,
        string path,
        bool allowTarget = false,
        bool allowPassOriginPolicy = false,
        bool allowResolveDestination = false,
        bool allowResourceId = false,
        bool allowDelta = false,
        bool allowStatusId = false,
        bool allowOperation = false,
        bool allowValue = false,
        Func<T>? create = null) where T : EffectDefinition
    {
        if ((!allowTarget && effect.Target is not null) || (!allowPassOriginPolicy && effect.PassOriginPolicy is not null) ||
            (!allowResolveDestination && effect.ResolveDestination is not null) || (!allowResourceId && effect.ResourceId is not null) ||
            (!allowDelta && effect.Delta is not null) || (!allowStatusId && effect.StatusId is not null) ||
            (!allowOperation && effect.Operation is not null) || (!allowValue && effect.Value is not null))
            throw Validation(ProfileValidationErrorKind.InvalidCombination, path, $"Effect '{effect.Kind}' contains fields for another effect kind.");
        return Construct(path, create ?? throw new ArgumentNullException(nameof(create)));
    }

    private static ProfilePolicySet MapPolicies(PoliciesDto policies)
    {
        MatchEndDto matchEnd = Required(policies.MatchEnd, "policies.matchEnd");
        string matchKind = Required(matchEnd.Kind, "policies.matchEnd.kind");
        if (matchKind != "highest-resource-after-rounds")
            throw Validation(ProfileValidationErrorKind.UnknownComponent, "policies.matchEnd.kind", $"Match-end policy '{matchKind}' is not supported by schema version 1.");

        return Construct("policies", () => new ProfilePolicySet(
            policies.PassOriginReward is null ? null : MapAmount(policies.PassOriginReward, "policies.passOriginReward"),
            ParsePurchaseDecline(policies.PurchaseDecline, "policies.purchaseDecline"),
            new RoundLimitedScorePolicy(
                Required(matchEnd.RoundLimit, "policies.matchEnd.roundLimit"),
                Id<ResourceId>(matchEnd.ResourceId, "policies.matchEnd.resourceId", value => new ResourceId(value)),
                ParseTieBreak(matchEnd.TieBreak, "policies.matchEnd.tieBreak"))));
    }

    private static ResourceAmount MapAmount(ResourceAmountDto amount, string path)
    {
        if (amount is null) throw Validation(ProfileValidationErrorKind.InvalidValue, path, "Resource amounts cannot be null.");
        return Construct(path, () => new ResourceAmount(
            Id<ResourceId>(amount.ResourceId, $"{path}.resourceId", value => new ResourceId(value)),
            Required(amount.Value, $"{path}.value")));
    }

    private static StartingPlayerPolicyKind ParseStartingPlayerPolicy(string? value, string path) => Required(value, path) switch
    {
        "fixed-order" => StartingPlayerPolicyKind.FixedOrder,
        "random" => StartingPlayerPolicyKind.Random,
        "highest-roll" => StartingPlayerPolicyKind.HighestRoll,
        string unknown => throw Validation(ProfileValidationErrorKind.UnknownComponent, path, $"Starting-player policy '{unknown}' is not supported by schema version 1.")
    };

    private static PurchaseDeclinePolicyKind ParsePurchaseDecline(string? value, string path) => Required(value, path) switch
    {
        "leave-unowned" => PurchaseDeclinePolicyKind.LeaveUnowned,
        string unknown => throw Validation(ProfileValidationErrorKind.UnknownComponent, path, $"Purchase-decline policy '{unknown}' is not supported by schema version 1.")
    };

    private static MatchTieBreakPolicy ParseTieBreak(string? value, string path) => Required(value, path) switch
    {
        "lowest-player-id" => MatchTieBreakPolicy.LowestPlayerId,
        string unknown => throw Validation(ProfileValidationErrorKind.UnknownComponent, path, $"Tie-break policy '{unknown}' is not supported by schema version 1.")
    };

    private static PassOriginPolicy ParsePassOriginPolicy(string? value, string path) => Required(value, path) switch
    {
        "ignore" => PassOriginPolicy.Ignore,
        "apply-profile-reward" => PassOriginPolicy.ApplyProfileReward,
        string unknown => throw Validation(ProfileValidationErrorKind.UnknownComponent, path, $"Pass-origin policy '{unknown}' is not supported by schema version 1.")
    };

    private static StatusEffectOperation ParseStatusOperation(string? value, string path) => Required(value, path) switch
    {
        "apply" => StatusEffectOperation.Apply,
        "remove" => StatusEffectOperation.Remove,
        string unknown => throw Validation(ProfileValidationErrorKind.UnknownComponent, path, $"Status operation '{unknown}' is not supported by schema version 1.")
    };

    private static PresentationToken Token(string? value, string path) =>
        Construct(path, () => new PresentationToken(Required(value, path)));

    private static PresentationToken? OptionalToken(string? value, string path) =>
        value is null ? null : Token(value, path);

    private static T Id<T>(string? value, string path, Func<string, T> create) where T : struct =>
        Construct(path, () => create(Required(value, path)));

    private static T Construct<T>(string path, Func<T> create)
    {
        try
        {
            return create();
        }
        catch (ProfileValidationException)
        {
            throw;
        }
        catch (ProfileContractException exception)
        {
            throw new ProfileValidationException(Map(exception.Kind), path, exception.Message, exception);
        }
        catch (ArgumentException exception)
        {
            throw new ProfileValidationException(ProfileValidationErrorKind.InvalidValue, path, exception.Message, exception);
        }
    }

    private static T Required<T>(T? value, string path) where T : class =>
        value ?? throw JsonError(ProfileJsonErrorKind.InvalidWireValue, path, $"The required '{path}' field is missing or null.");

    private static string Required(string? value, string path) =>
        value ?? throw JsonError(ProfileJsonErrorKind.InvalidWireValue, path, $"The required '{path}' field is missing or null.");

    private static int Required(int? value, string path) =>
        value ?? throw JsonError(ProfileJsonErrorKind.InvalidWireValue, path, $"The required '{path}' field is missing or null.");

    private static bool Required(bool? value, string path) =>
        value ?? throw JsonError(ProfileJsonErrorKind.InvalidWireValue, path, $"The required '{path}' field is missing or null.");

    private static void RejectDuplicateMembers(ReadOnlySpan<byte> json)
    {
        try
        {
            Utf8JsonReader reader = new(json, new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = GameProfileSchema.MaximumJsonDepth
            });
            Stack<HashSet<string>> objects = [];
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                        objects.Push(new HashSet<string>(StringComparer.Ordinal));
                        break;
                    case JsonTokenType.PropertyName:
                        string name = reader.GetString()!;
                        if (!objects.Peek().Add(name))
                            throw JsonError(ProfileJsonErrorKind.DuplicateMember, name, $"JSON member '{name}' is duplicated in the same object.");
                        break;
                    case JsonTokenType.EndObject:
                        objects.Pop();
                        break;
                }
            }
        }
        catch (ProfileJsonException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw TranslateJsonException(exception);
        }
    }

    private static ProfileJsonException TranslateJsonException(JsonException exception)
    {
        string path = string.IsNullOrWhiteSpace(exception.Path) ? "$" : exception.Path;
        if (exception.Message.Contains("maximum configured depth", StringComparison.OrdinalIgnoreCase))
            return JsonError(ProfileJsonErrorKind.DepthExceeded, path, $"Profile JSON exceeds depth {GameProfileSchema.MaximumJsonDepth}.", exception);
        if (exception.Message.Contains("could not be mapped", StringComparison.OrdinalIgnoreCase))
            return JsonError(ProfileJsonErrorKind.UnknownMember, path, "Profile JSON contains an unknown member.", exception);
        if (exception.Message.Contains("could not be converted", StringComparison.OrdinalIgnoreCase))
            return JsonError(ProfileJsonErrorKind.InvalidWireValue, path, "Profile JSON contains a value with the wrong wire type.", exception);
        return JsonError(ProfileJsonErrorKind.MalformedJson, path, "Profile JSON is malformed.", exception);
    }

    private static ProfileValidationErrorKind Map(ProfileContractErrorKind kind) => kind switch
    {
        ProfileContractErrorKind.UnknownComponent => ProfileValidationErrorKind.UnknownComponent,
        ProfileContractErrorKind.DuplicateDefinition => ProfileValidationErrorKind.DuplicateDefinition,
        ProfileContractErrorKind.BrokenReference => ProfileValidationErrorKind.BrokenReference,
        ProfileContractErrorKind.InvalidCombination => ProfileValidationErrorKind.InvalidCombination,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static ProfileValidationException Validation(ProfileValidationErrorKind kind, string path, string message) =>
        new(kind, path, message);

    private static ProfileJsonException JsonError(ProfileJsonErrorKind kind, string path, string message, Exception? inner = null) =>
        new(kind, path, message, inner);
}
