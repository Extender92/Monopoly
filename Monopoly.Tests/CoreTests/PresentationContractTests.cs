using System.Reflection;
using System.Text.Json;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models.FortuneCard;
using Monopoly.Core.Notifications;
using Monopoly.Core.Persistence;
using Monopoly.Core.Presentation;

namespace Monopoly.Tests.CoreTests;

public sealed class PresentationContractTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Upper")]
    [InlineData("two words")]
    [InlineData("leading_underscore")]
    [InlineData("double..segment")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    public void PresentationTokenRejectsInvalidValues(string value) =>
        Assert.Throws<ArgumentException>(() => new PresentationToken(value));

    [Fact]
    public void PresentationTokenIsOrdinalStableAndLengthLimited()
    {
        PresentationToken first = new("space.lantern-vale");
        PresentationToken second = new("space.lantern-vale");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal("space.lantern-vale", first.Value);
        Assert.True(new PresentationToken("a" + new string('b', PresentationToken.MaximumLength - 1)).IsValid);
        Assert.Throws<ArgumentException>(() => new PresentationToken("a" + new string('b', PresentationToken.MaximumLength)));
    }

    [Fact]
    public void ProfilePresentationIsSortedImmutableAndUsesDocumentedTextFallback()
    {
        PresentationMetadata later = new(new PresentationToken("space.zinc"), displayText: "Zinc Terrace");
        PresentationMetadata earlier = new(new PresentationToken("space.amber"), shortText: "Amber");
        PresentationMetadata fallback = new(new PresentationToken("space.cedar"));
        PresentationMetadata[] source = [later, earlier, fallback];

        ProfilePresentation catalog = new(source);
        source[0] = earlier;

        Assert.Equal(
            ["space.amber", "space.cedar", "space.zinc"],
            catalog.Entries.Select(entry => entry.Token.Value));
        Assert.Equal("Amber", catalog.ResolveDisplayText(earlier.Token));
        Assert.Equal("space.cedar", catalog.ResolveDisplayText(fallback.Token));
        Assert.Equal("Zinc Terrace", catalog.ResolveDisplayText(later.Token));
        Assert.True(catalog.TryResolve(earlier.Token, out PresentationMetadata? resolved));
        Assert.Same(earlier, resolved);
        Assert.False(catalog.TryResolve(new PresentationToken("space.missing"), out _));
        Assert.Throws<NotSupportedException>(() => ((IList<PresentationMetadata>)catalog.Entries)[0] = later);
    }

    [Fact]
    public void ProfilePresentationRejectsDuplicatesConflictsAndMissingHintReferences()
    {
        PresentationToken token = new("space.lantern");

        Assert.Throws<ArgumentException>(() => new ProfilePresentation(
            [new PresentationMetadata(token), new PresentationMetadata(token)]));
        Assert.Throws<ArgumentException>(() => new ProfilePresentation(
            [new PresentationMetadata(token, displayText: "One"), new PresentationMetadata(token, displayText: "Two")]));
        Assert.Throws<ArgumentException>(() => new ProfilePresentation(
            [new PresentationMetadata(token, colorToken: new PresentationToken("accent.missing"))]));
    }

    [Fact]
    public void GameRejectsMissingReferencedPresentationBeforeItIsReturned()
    {
        Game baseline = CoreGameSetup.Setup(new GameRules(2, 2, 6));
        ProfilePresentation incomplete = new(
            baseline.Presentation.Entries.Where(entry => entry.Token != new PresentationToken("space.0")));
        Player first = new("First", 0);
        Player second = new("Second", 1);

        Assert.Throws<ArgumentException>(() =>
            new Game([first, second], first, new GameRules(2, 2, 6), presentation: incomplete));
    }

    [Fact]
    public void PresentationChangesDoNotChangeMovementPurchasesFeesDecisionsOrV1State()
    {
        GameRules rules = new(2, 2, 6);
        Game baseline = CreateGame(rules);
        ProfilePresentation variantPresentation = CreateVariant(baseline.Presentation);
        Game variant = CreateGame(rules, variantPresentation);

        GameStateV1 initial = GameStateV1Mapper.ToState(baseline);
        variant.CardHandler.RestoreLegacyDeckOrder(initial.ChanceDeck, initial.CommunityChestDeck);
        AssertEquivalentV1(baseline, variant);

        PurchaseDecision baselineDecision = Assert.IsType<PurchaseDecision>(baseline.PlayTurn().PendingDecision);
        PurchaseDecision variantDecision = Assert.IsType<PurchaseDecision>(variant.PlayTurn().PendingDecision);
        Assert.Equal(baselineDecision.SpaceId, variantDecision.SpaceId);
        Assert.Equal(baselineDecision.Price, variantDecision.Price);
        Assert.Equal(baselineDecision.AllowedResponses, variantDecision.AllowedResponses);

        baseline.SubmitDecision(new DecisionResponse(baselineDecision.DecisionId, DecisionOptions.Accept));
        variant.SubmitDecision(new DecisionResponse(variantDecision.DecisionId, DecisionOptions.Accept));
        baseline.PlayTurnToCompletion();
        variant.PlayTurnToCompletion();

        Assert.Equal(baseline.Players.Select(player => (player.Position, player.Money)), variant.Players.Select(player => (player.Position, player.Money)));
        Assert.Equal(baseline.Board.Squares.Select(square => square.Owner?.Id), variant.Board.Squares.Select(square => square.Owner?.Id));
        AssertEquivalentV1(baseline, variant);
    }

    [Fact]
    public void IdenticalColorHintsCannotMergeDifferentAuthoritativeGroups()
    {
        PresentationToken sharedColor = new("accent.shared");
        PresentationMetadata firstGroupPresentation = new(new PresentationToken("group.first"), colorToken: sharedColor);
        PresentationMetadata secondGroupPresentation = new(new PresentationToken("group.second"), colorToken: sharedColor);
        PropertySquare first = CreateProperty(new GroupId("group-id.first"), firstGroupPresentation, 1);
        PropertySquare second = CreateProperty(new GroupId("group-id.second"), secondGroupPresentation, 2);
        Player firstOwner = new("First", 0);
        Player secondOwner = new("Second", 1);
        first.AssignOwner(firstOwner);
        second.AssignOwner(secondOwner);

        Assert.True(first.OwnerHasGroup([first, second]));
        Assert.True(second.OwnerHasGroup([first, second]));
        Assert.NotEqual(first.GroupId, second.GroupId);
        Assert.Equal(firstGroupPresentation.ColorToken, secondGroupPresentation.ColorToken);
    }

    [Fact]
    public void SpacesCardsDecksStatusesDecisionsAndNotificationsExposeResolvableTokens()
    {
        Game purchaseGame = new GameTestBuilder().WithRandomValues(1, 2).Build();
        PendingDecision decision = Assert.IsType<PurchaseDecision>(purchaseGame.PlayTurn().PendingDecision);
        Assert.NotNull(purchaseGame.Presentation.Resolve(decision.PresentationToken));
        Assert.All(purchaseGame.Board.Squares, square => Assert.NotNull(purchaseGame.Presentation.Resolve(square.PresentationToken)));
        Assert.All(purchaseGame.Decks.Entries.SelectMany(deck => deck.Cards),
            card => Assert.NotNull(purchaseGame.Presentation.Resolve(card.PresentationToken)));
        Assert.All(purchaseGame.Decks.Entries,
            deck => Assert.NotNull(purchaseGame.Presentation.Resolve(deck.PresentationToken)));

        Game detained = new GameTestBuilder().WithPlayerInJail(0).Build();
        Assert.NotNull(detained.Presentation.Resolve(detained.TheJail.GetJailInfo(detained.Players[0]).PresentationToken));

        Game notificationGame = new GameTestBuilder().WithRandomValues(3, 4).Build();
        List<GameNotification> notifications = [];
        using IDisposable subscription = notificationGame.Notifications.Subscribe(notifications.Add);
        notificationGame.PlayTurn();
        Assert.Contains(notifications, notification => notification is SpaceReachedNotification);
        CardDrawnNotification drawn = Assert.Single(notifications.OfType<CardDrawnNotification>());
        Assert.Equal(LegacyStructureIds.PrimaryDeck, drawn.DeckId);
        Assert.Contains(drawn.Card.Id,
            notificationGame.Decks.Resolve(drawn.DeckId).Cards.Select(card => card.Id));
        Assert.All(notifications, notification => Assert.NotNull(notificationGame.Presentation.Resolve(notification.PresentationToken)));
    }

    [Fact]
    public void VersionOneStateContainsNoPresentationMetadata()
    {
        string json = JsonSerializer.Serialize(GameStateV1Mapper.ToState(CoreGameSetup.Setup(new GameRules(2, 2, 6))));

        Assert.DoesNotContain("Presentation", json, StringComparison.Ordinal);
        Assert.DoesNotContain("DisplayText", json, StringComparison.Ordinal);
        Assert.DoesNotContain("ColorToken", json, StringComparison.Ordinal);
        Assert.DoesNotContain("LayoutToken", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Symbol", json, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicCoreContractsExposeNoFrontendTypesOrRemovedPresentationMembers()
    {
        Assembly core = typeof(Game).Assembly;
        foreach (Type type in core.GetExportedTypes())
        {
            AssertFrontendNeutral(type.BaseType, type);
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                AssertFrontendNeutral(property.PropertyType, type);
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                AssertFrontendNeutral(field.FieldType, type);
            foreach (MethodBase method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Cast<MethodBase>()
                         .Concat(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)))
            {
                if (method is MethodInfo methodInfo)
                    AssertFrontendNeutral(methodInfo.ReturnType, type);
                foreach (ParameterInfo parameter in method.GetParameters())
                    AssertFrontendNeutral(parameter.ParameterType, type);
            }
        }

        Assert.Null(typeof(Square).GetProperty("Name"));
        Assert.Null(typeof(Square).GetProperty("Info"));
        Assert.Null(typeof(ICardView).GetProperty("Info"));
        Assert.Null(typeof(JailSquare).GetProperty("InJailInfo"));
        Assert.Null(typeof(GameRules).GetProperty("CurrencySymbol"));
        Assert.Null(typeof(PropertySquare).GetProperty("Color"));
        Assert.DoesNotContain(typeof(PropertySquare).GetConstructors(), constructor =>
            constructor.GetParameters().Any(parameter => parameter.ParameterType == typeof(ConsoleColor)));
        Assert.Null(core.GetType("Monopoly.Core.Models.Board.PropertyGroup"));
    }

    private static Game CreateGame(GameRules rules, ProfilePresentation? presentation = null)
    {
        Player first = new("First", 0);
        Player second = new("Second", 1);
        return new Game(
            [first, second], first, rules,
            decisions: null,
            presentation: presentation,
            randomSource: ScriptedMatchRandomSource.ForDice(1, 2, 1, 2));
    }

    private static ProfilePresentation CreateVariant(ProfilePresentation source)
    {
        PresentationToken variantColor = new("accent.variant");
        PresentationToken variantLayout = new("layout.variant");
        List<PresentationMetadata> entries =
        [
            .. source.Entries.Select(entry => new PresentationMetadata(
                entry.Token,
                displayText: entry.DisplayText is null ? null : $"Variant {entry.Token}",
                shortText: entry.ShortText,
                description: entry.Description is null ? null : $"Variant description {entry.Token}",
                symbol: entry.Token == PresentationTokens.PrimaryResource ? "¤" : entry.Symbol,
                colorToken: entry.Token.Value.StartsWith("group.", StringComparison.Ordinal) ? variantColor : entry.ColorToken,
                layoutToken: entry.Token.Value.StartsWith("space.", StringComparison.Ordinal) ? variantLayout : entry.LayoutToken)),
            new(variantColor),
            new(variantLayout)
        ];
        return new ProfilePresentation(entries);
    }

    private static PropertySquare CreateProperty(GroupId groupId, PresentationMetadata groupPresentation, int position) =>
        new(
            groupId,
            groupPresentation,
            new PresentationMetadata(new PresentationToken($"space.synthetic-{position}"), displayText: $"Synthetic {position}"),
            2, 4, 10, 30, 90, 160, 250, 50, 50, 60, 30, position);

    private static void AssertEquivalentV1(Game expected, Game actual) =>
        Assert.Equal(
            JsonSerializer.Serialize(GameStateV1Mapper.ToState(expected)),
            JsonSerializer.Serialize(GameStateV1Mapper.ToState(actual)));

    private static void AssertFrontendNeutral(Type? candidate, Type owner)
    {
        if (candidate is null) return;
        Type inspected = candidate.IsByRef || candidate.IsArray ? candidate.GetElementType()! : candidate;
        string fullName = inspected.FullName ?? inspected.Name;
        Assert.False(fullName == typeof(ConsoleColor).FullName, $"{owner.FullName} exposes {fullName}.");
        Assert.False(inspected.Namespace?.StartsWith("System.Drawing", StringComparison.Ordinal) == true, $"{owner.FullName} exposes {fullName}.");
        Assert.False(inspected.Namespace?.StartsWith("Monopoly.Console", StringComparison.Ordinal) == true, $"{owner.FullName} exposes {fullName}.");
        if (inspected.IsGenericType)
        {
            foreach (Type argument in inspected.GetGenericArguments())
                AssertFrontendNeutral(argument, owner);
        }
    }

}
