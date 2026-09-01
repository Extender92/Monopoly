using System.Text.Json.Nodes;
using Infrastructure.Profiles;

namespace Monopoly.Tests.InfrastructureTests;

public sealed class PublicationConformanceTests
{
    private static readonly string ProfileFixtureRoot = Path.Combine(AppContext.BaseDirectory, "TestData", "Profiles");

    [Theory]
    [InlineData("Demo/lantern-vale-v1.json", 27, 1)]
    [InlineData("synthetic-zero-decks-v1.json", 1, 0)]
    [InlineData("synthetic-multi-decks-v1.json", 4, 2)]
    public void TrackedProfilesProveVariableTracksAndDeckCollections(string relativePath, int spaces, int decks)
    {
        ValidatedGameProfile profile = new JsonGameProfileParser().Parse(
            File.ReadAllBytes(Path.Combine(ProfileFixtureRoot, relativePath)));

        Assert.Equal(spaces, profile.RuleGraph.Track.Count);
        Assert.Equal(decks, profile.RuleGraph.Decks.Count);
        Assert.Equal(profile.RuleGraph.Track.Count, profile.RuleGraph.Spaces.Count);
    }

    [Fact]
    public void ProfileSchemaDefinesBoundedGenericTrackAndDeckArrays()
    {
        JsonObject schema = LoadSchema("profiles", "schema", "game-profile-v1.schema.json");
        JsonObject properties = schema["properties"]!.AsObject();
        JsonObject track = properties["track"]!.AsObject();
        JsonObject decks = properties["decks"]!.AsObject();

        Assert.Equal("array", track["type"]!.GetValue<string>());
        Assert.Equal(1, track["minItems"]!.GetValue<int>());
        Assert.Equal(512, track["maxItems"]!.GetValue<int>());
        Assert.Equal("array", decks["type"]!.GetValue<string>());
        Assert.Null(decks["minItems"]);
        Assert.Equal(32, decks["maxItems"]!.GetValue<int>());
    }

    [Fact]
    public void SaveSchemaUsesVersionTwoAndGenericDeckEntries()
    {
        JsonObject schema = LoadSchema("schemas", "game-save-v2.schema.json");
        JsonObject rootProperties = schema["properties"]!.AsObject();
        JsonObject definitions = schema["$defs"]!.AsObject();
        JsonObject matchProperties = definitions["match"]!["properties"]!.AsObject();
        JsonObject deckProperties = definitions["deck"]!["properties"]!.AsObject();

        Assert.Equal(2, rootProperties["formatVersion"]!["const"]!.GetValue<int>());
        Assert.Equal("array", matchProperties["decks"]!["type"]!.GetValue<string>());
        Assert.Equal(32, matchProperties["decks"]!["maxItems"]!.GetValue<int>());
        Assert.Equal(["cardIds", "deckId"], deckProperties.Select(property => property.Key).Order(StringComparer.Ordinal));
    }

    private static JsonObject LoadSchema(params string[] relativeSegments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.EnumerateFiles(directory.FullName, "*.sln").Any())
            directory = directory.Parent;

        Assert.NotNull(directory);
        string path = relativeSegments.Aggregate(directory!.FullName, Path.Combine);
        return JsonNode.Parse(File.ReadAllText(path))!.AsObject();
    }
}
