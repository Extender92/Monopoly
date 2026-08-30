using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Infrastructure.Profiles;
using Monopoly.Core.Persistence;
using Monopoly.Tests.CoreTests;

namespace Monopoly.Tests.InfrastructureTests;

public sealed class JsonGameProfileParserTests
{
    private readonly JsonGameProfileParser _parser = new();

    [Theory]
    [InlineData("schema-conformance-v1.json", 3, 1)]
    [InlineData("synthetic-zero-decks-v1.json", 1, 0)]
    [InlineData("synthetic-multi-decks-v1.json", 4, 2)]
    public void OriginalAndStructurallyVariedFixturesValidate(string fileName, int spaces, int decks)
    {
        ValidatedGameProfile profile = _parser.Parse(FixtureBytes(fileName));

        Assert.Equal(spaces, profile.RuleGraph.Track.Count);
        Assert.Equal(decks, profile.RuleGraph.Decks.Count);
        Assert.True(profile.Fingerprint.IsValid);
    }

    [Fact]
    public void EquivalentJsonFormattingPropertyAndCatalogOrderHaveTheSameFingerprint()
    {
        JsonObject original = FixtureNode("synthetic-multi-decks-v1.json");
        JsonObject reordered = new();
        foreach ((string key, JsonNode? value) in original.Reverse()) reordered[key] = value?.DeepClone();
        foreach (string catalog in new[] { "presentation", "resources", "spaces", "decks", "statuses" })
        {
            JsonArray array = reordered[catalog]!.AsArray();
            reordered[catalog] = new JsonArray(array.Reverse().Select(item => item?.DeepClone()).ToArray());
        }

        byte[] compact = Encoding.UTF8.GetBytes(reordered.ToJsonString());
        byte[] withBom = [239, 187, 191, .. compact];

        Assert.Equal(_parser.Parse(FixtureBytes("synthetic-multi-decks-v1.json")).Fingerprint, _parser.Parse(withBom).Fingerprint);
    }

    [Fact]
    public void EverySemanticProfileCategoryContributesToFingerprint()
    {
        ProfileFingerprint baseline = _parser.Parse(FixtureBytes()).Fingerprint;
        List<JsonObject> variants = [];

        JsonObject presentation = FixtureNode();
        presentation["presentation"]![0]!["displayText"] = "Changed title";
        variants.Add(presentation);

        JsonObject setup = FixtureNode();
        setup["setup"]!["dieSides"] = 10;
        variants.Add(setup);

        JsonObject track = FixtureNode();
        JsonNode? first = track["track"]![0]!.DeepClone();
        track["track"]![0] = track["track"]![1]!.DeepClone();
        track["track"]![1] = first;
        track["setup"]!["startSpaceId"] = "space.market";
        variants.Add(track);

        JsonObject capability = FixtureNode();
        capability["spaces"]![1]!["capabilities"]![1]!["price"]!["value"] = 13;
        variants.Add(capability);

        JsonObject card = FixtureNode();
        card["decks"]![0]!["cards"]![0]!["effects"]![0]!["delta"] = 3;
        variants.Add(card);

        JsonObject policy = FixtureNode();
        policy["policies"]!["matchEnd"]!["roundLimit"] = 9;
        variants.Add(policy);

        JsonObject effectOrder = FixtureNode();
        JsonArray effects = effectOrder["decks"]![0]!["cards"]![0]!["effects"]!.AsArray();
        effectOrder["decks"]![0]!["cards"]![0]!["effects"] =
            new JsonArray(effects.Reverse().Select(effect => effect?.DeepClone()).ToArray());
        variants.Add(effectOrder);

        Assert.All(variants, variant => Assert.NotEqual(baseline, _parser.Parse(Utf8(variant)).Fingerprint));
    }

    [Fact]
    public void CardOrderIsFingerprintSignificant()
    {
        JsonObject firstOrder = FixtureNode();
        firstOrder["presentation"]!.AsArray().Add(new JsonObject
        {
            ["token"] = "card.crosswind",
            ["shortText"] = "A crosswind changes your score."
        });
        JsonObject secondCard = firstOrder["decks"]![0]!["cards"]![0]!.DeepClone().AsObject();
        secondCard["id"] = "card.crosswind";
        secondCard["presentationToken"] = "card.crosswind";
        secondCard["effects"]![0]!["delta"] = 4;
        firstOrder["decks"]![0]!["cards"]!.AsArray().Add(secondCard);

        JsonObject secondOrder = firstOrder.DeepClone().AsObject();
        JsonArray cards = secondOrder["decks"]![0]!["cards"]!.AsArray();
        secondOrder["decks"]![0]!["cards"] = new JsonArray(cards.Reverse().Select(card => card?.DeepClone()).ToArray());

        Assert.NotEqual(_parser.Parse(Utf8(firstOrder)).Fingerprint, _parser.Parse(Utf8(secondOrder)).Fingerprint);
    }

    [Fact]
    public void FingerprintIsStableAcrossCulturesAndRepeatedParsing()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
            ProfileFingerprint swedish = _parser.Parse(FixtureBytes()).Fingerprint;
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
            ProfileFingerprint turkish = _parser.Parse(FixtureBytes()).Fingerprint;

            Assert.Equal(swedish, turkish);
            Assert.Matches("^[0-9a-f]{64}$", swedish.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void StrictParserClassifiesUnknownDuplicateMalformedVersionTypeEncodingDepthAndSizeErrors()
    {
        JsonObject unknown = FixtureNode();
        unknown["unexpected"] = true;
        AssertJson(ProfileJsonErrorKind.UnknownMember, () => _parser.Parse(Utf8(unknown)));

        string duplicate = Encoding.UTF8.GetString(FixtureBytes())
            .Replace("\"revision\": 1,", "\"revision\": 1,\n  \"revision\": 1,", StringComparison.Ordinal);
        AssertJson(ProfileJsonErrorKind.DuplicateMember, () => _parser.Parse(Encoding.UTF8.GetBytes(duplicate)));
        AssertJson(ProfileJsonErrorKind.MalformedJson, () => _parser.Parse(Encoding.UTF8.GetBytes("{not-json")));
        AssertJson(ProfileJsonErrorKind.MalformedJson, () => _parser.Parse(Encoding.UTF8.GetBytes("{/*comment*/}")));

        string trailingComma = Encoding.UTF8.GetString(FixtureBytes()).Replace("\n}", "\n,}", StringComparison.Ordinal);
        AssertJson(ProfileJsonErrorKind.MalformedJson, () => _parser.Parse(Encoding.UTF8.GetBytes(trailingComma)));

        JsonObject version = FixtureNode();
        version["schemaVersion"] = 2;
        AssertJson(ProfileJsonErrorKind.UnsupportedSchemaVersion, () => _parser.Parse(Utf8(version)));

        JsonObject wrongType = FixtureNode();
        wrongType["revision"] = "one";
        AssertJson(ProfileJsonErrorKind.InvalidWireValue, () => _parser.Parse(Utf8(wrongType)));
        AssertJson(ProfileJsonErrorKind.InvalidEncoding, () => _parser.Parse(Encoding.Unicode.GetBytes(FixtureNode().ToJsonString())));

        string deep = string.Concat(Enumerable.Repeat("{\"a\":", GameProfileSchema.MaximumJsonDepth + 1)) + "0" +
            string.Concat(Enumerable.Repeat("}", GameProfileSchema.MaximumJsonDepth + 1));
        AssertJson(ProfileJsonErrorKind.DepthExceeded, () => _parser.Parse(Encoding.UTF8.GetBytes(deep)));
        AssertJson(ProfileJsonErrorKind.InputTooLarge, () => _parser.Parse(new byte[GameProfileSchema.MaximumInputBytes + 1]));
    }

    [Fact]
    public void SemanticErrorsAreTypedAndFailureDoesNotMutateAnActiveMatch()
    {
        JsonObject unknownCapability = FixtureNode();
        unknownCapability["spaces"]![1]!["capabilities"]![0]!["kind"] = "teleport";
        ProfileValidationException unknown = Assert.Throws<ProfileValidationException>(() => _parser.Parse(Utf8(unknownCapability)));
        Assert.Equal(ProfileValidationErrorKind.UnknownComponent, unknown.Kind);

        JsonObject brokenReference = FixtureNode();
        brokenReference["setup"]!["startSpaceId"] = "space.missing";
        ProfileValidationException broken = Assert.Throws<ProfileValidationException>(() => _parser.Parse(Utf8(brokenReference)));
        Assert.Equal(ProfileValidationErrorKind.BrokenReference, broken.Kind);

        JsonObject excessiveText = FixtureNode();
        excessiveText["presentation"]![0]!["displayText"] = string.Concat(
            Enumerable.Repeat("\U0001F4A1", GameProfileSchema.MaximumPresentationTextLength + 1));
        ProfileValidationException excessive = Assert.Throws<ProfileValidationException>(() => _parser.Parse(Utf8(excessiveText)));
        Assert.Equal(ProfileValidationErrorKind.LimitExceeded, excessive.Kind);

        Game game = new Monopoly.Tests.CoreTests.GameTestBuilder().Build();
        string before = GameTestSnapshot.Capture(game);
        Assert.Throws<ProfileValidationException>(() => _parser.Parse(Utf8(brokenReference)));
        Assert.Equal(before, GameTestSnapshot.Capture(game));
    }

    [Fact]
    public void StreamInputUsesTheSameBoundedStrictContract()
    {
        using MemoryStream stream = new(FixtureBytes());
        Assert.True(_parser.Parse(stream).Fingerprint.IsValid);

        byte[] exactLimit = new byte[GameProfileSchema.MaximumInputBytes];
        byte[] fixture = FixtureBytes();
        fixture.CopyTo(exactLimit, 0);
        Array.Fill(exactLimit, (byte)' ', fixture.Length, exactLimit.Length - fixture.Length);
        Assert.True(_parser.Parse(exactLimit).Fingerprint.IsValid);

        using MemoryStream oversized = new(new byte[GameProfileSchema.MaximumInputBytes + 1]);
        AssertJson(ProfileJsonErrorKind.InputTooLarge, () => _parser.Parse(oversized));
    }

    [Fact]
    public void TrackedSchemaDeclaresClosedObjectsDiscriminatorsAndSafetyLimits()
    {
        string schemaPath = Path.Combine(RepositoryRoot(), "profiles", "schema", "game-profile-v1.schema.json");
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllBytes(schemaPath));
        JsonElement root = schema.RootElement;

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", root.GetProperty("$schema").GetString());
        Assert.Equal(1, root.GetProperty("properties").GetProperty("schemaVersion").GetProperty("const").GetInt32());
        Assert.Equal(GameProfileSchema.MaximumSpaces, root.GetProperty("properties").GetProperty("track").GetProperty("maxItems").GetInt32());
        Assert.Equal(GameProfileSchema.MaximumDecks, root.GetProperty("properties").GetProperty("decks").GetProperty("maxItems").GetInt32());
        Assert.Equal(GameProfileSchema.MaximumPresentationTextLength,
            root.GetProperty("$defs").GetProperty("presentationText").GetProperty("maxLength").GetInt32());
        AssertClosedObjects(root);
    }

    private static void AssertClosedObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("type", out JsonElement type) && type.ValueKind == JsonValueKind.String && type.GetString() == "object")
            {
                Assert.True(element.TryGetProperty("additionalProperties", out JsonElement additional));
                Assert.False(additional.GetBoolean());
            }
            foreach (JsonProperty property in element.EnumerateObject()) AssertClosedObjects(property.Value);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in element.EnumerateArray()) AssertClosedObjects(child);
        }
    }

    private static void AssertJson(ProfileJsonErrorKind kind, Action action) =>
        Assert.Equal(kind, Assert.Throws<ProfileJsonException>(action).Kind);

    private static JsonObject FixtureNode(string fileName = "schema-conformance-v1.json") => JsonNode.Parse(FixtureBytes(fileName))!.AsObject();
    private static byte[] Utf8(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());
    private static byte[] FixtureBytes(string fileName = "schema-conformance-v1.json") =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "Profiles", fileName));

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Monopoly.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
