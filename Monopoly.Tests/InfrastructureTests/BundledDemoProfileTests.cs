using System.Reflection;
using Infrastructure.Profiles;
using Monopoly.Core;
using Monopoly.Core.Interface;

namespace Monopoly.Tests.InfrastructureTests;

public sealed class BundledDemoProfileTests
{
    private static readonly string DemoPath = Path.Combine(
        AppContext.BaseDirectory,
        "TestData",
        "Profiles",
        "Demo",
        "lantern-vale-v1.json");

    [Fact]
    public void BundledLanternValeProfileHasTheLockedOriginalContract()
    {
        ValidatedGameProfile profile = new JsonGameProfileParser().Parse(File.ReadAllBytes(DemoPath));

        Assert.Equal(new ProfileId("profile.demo-001"), profile.Id);
        Assert.Equal(new ProfileRevision(1), profile.Revision);
        Assert.Equal("Lantern Vale", profile.Presentation.Resolve(profile.PresentationToken).DisplayText);
        Assert.Equal(27, profile.RuleGraph.Track.Count);
        Assert.Equal(2, profile.Setup.MinimumPlayers);
        Assert.Equal(5, profile.Setup.MaximumPlayers);
        Assert.Equal(2, profile.Setup.DiceCount);
        Assert.Equal(8, profile.Setup.DieSides);
        Assert.Equal(StartingPlayerPolicyKind.FixedOrder, profile.Setup.StartingPlayerPolicy);
        Assert.Equal(
            [
                new ResourceAmount(new ResourceId("resource.lumen"), 120),
                new ResourceAmount(new ResourceId("resource.renown"), 0)
            ],
            profile.Setup.StartingResources);
        Assert.Equal(new ResourceAmount(new ResourceId("resource.lumen"), 12), profile.Policies.PassOriginReward);
        Assert.Equal(PurchaseDeclinePolicyKind.LeaveUnowned, profile.Policies.PurchaseDecline);
        Assert.Equal(12, profile.Policies.MatchEnd.RoundLimit);
        Assert.Equal(new ResourceId("resource.renown"), profile.Policies.MatchEnd.ScoreResourceId);
        Assert.Equal(MatchTieBreakPolicy.LowestPlayerId, profile.Policies.MatchEnd.TieBreak);
        Assert.Empty(profile.RuleGraph.Statuses);

        DeckDefinition deck = Assert.Single(profile.RuleGraph.Decks);
        Assert.Equal(new DeckId("deck.d001"), deck.Id);
        Assert.Equal("Vale Messages", profile.Presentation.Resolve(deck.PresentationToken).DisplayText);
        Assert.Equal(9, deck.Cards.Count);
        Assert.Equal(
            Enumerable.Range(1, 9).Select(index => new CardId($"card.c{index:000}")),
            deck.Cards.Select(card => card.Id));

        int[] groupSizes = profile.RuleGraph.Spaces
            .Select(space => space.Capabilities.Find<OwnableCapabilityDefinition>()?.GroupId)
            .Where(group => group is not null)
            .GroupBy(group => group!.Value)
            .Select(group => group.Count())
            .Order()
            .ToArray();
        Assert.Equal([2, 2, 3, 3, 4], groupSizes);
        Assert.All(
            profile.RuleGraph.Spaces.SelectMany(space => space.Capabilities.Entries),
            capability => Assert.Contains(
                capability.Id,
                new[] { CapabilityKinds.Ownable, CapabilityKinds.Purchasable, CapabilityKinds.UsageFee, CapabilityKinds.Draw }));
        Assert.All(
            deck.Cards.SelectMany(card => card.Effects.Entries),
            effect => Assert.Contains(effect.Kind, new[] { EffectKinds.Move, EffectKinds.ResourceChange }));

        Assert.Equal(
            "7ba140a86da1a20222f2580b7419ca7e3f52d7a392bcadf9269ed1fe5a456c7d",
            profile.Fingerprint.Value);
    }

    [Fact]
    public void PublicCoreSurfaceContainsNoRegionalFactoriesCardsOrEditionSelection()
    {
        Assembly core = typeof(Game).Assembly;
        string[] forbiddenTypes =
        [
            "Monopoly.Core.CoreGameSetup",
            "Monopoly.Core.Data.Data",
            "Monopoly.Core.Data.SquareBuilder",
            "Monopoly.Core.Data.FortuneCardBuilder"
        ];

        Assert.All(forbiddenTypes, typeName => Assert.Null(core.GetType(typeName)));
        string[] editionPrefixes = [new(['U', 'K']), new(['U', 'S'])];
        Assert.DoesNotContain(core.GetTypes(), type =>
            editionPrefixes.Any(prefix => type.Name.StartsWith(prefix, StringComparison.Ordinal)) ||
            type.IsEnum && type.GetEnumNames().Any(name => editionPrefixes.Contains(name, StringComparer.Ordinal)));
        Assert.Empty(typeof(Game).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(typeof(IGame).GetProperty("Rules"));
    }
}
