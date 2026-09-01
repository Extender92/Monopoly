using Monopoly.Console.GUI;
using Monopoly.Core;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Presentation;

namespace Monopoly.Console;

internal sealed class ConsoleProjectionBuilder
{
    internal ConsoleMatchProjection Build(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        ConsolePresentationResolver presentation = new(game.Presentation);

        Dictionary<int, Player> playersById = game.Players.ToDictionary(player => player.Id);
        Dictionary<SpaceId, SpaceDefinition> definitions = game.Profile.RuleGraph.Spaces
            .ToDictionary(space => space.Id);
        Dictionary<DeckId, DeckDefinition> deckDefinitions = game.Profile.RuleGraph.Decks
            .ToDictionary(deck => deck.Id);
        Dictionary<SpaceId, int?> ownership = game.Ownership.Entries
            .ToDictionary(entry => entry.SpaceId, entry => entry.OwnerPlayerId);

        ConsolePlayerProjection[] players = game.Players.Select(player =>
        {
            SpaceView space = game.Board.GetSpace(player.CurrentSpaceId);
            ConsoleResourceProjection[] resources = game.Profile.Resources.Select(resource =>
            {
                if (!player.Resources.TryGetValue(resource.Id, out int value))
                    throw Inconsistent($"Player '{player.Id}' is missing resource '{resource.Id}'.");
                return new ConsoleResourceProjection(
                    presentation.GetDisplayText(resource.PresentationToken),
                    presentation.FormatAmount(value, resource.PresentationToken));
            }).ToArray();

            return new ConsolePlayerProjection(
                player.Id,
                ConsoleText.Sanitize(player.Name),
                presentation.GetDisplayText(space.PresentationToken),
                player.Id == game.CurrentPlayer.Id,
                Array.AsReadOnly(resources));
        }).ToArray();

        ConsoleSpaceProjection[] spaces = game.Board.Spaces.Select(space =>
        {
            if (!definitions.TryGetValue(space.Id, out SpaceDefinition? definition))
                throw Inconsistent($"Space '{space.Id}' has no profile definition.");

            string[] occupants = game.Players
                .Where(player => player.CurrentSpaceId == space.Id)
                .Select(player => ConsoleText.Sanitize(player.Name))
                .ToArray();
            string? owner = null;
            if (ownership.TryGetValue(space.Id, out int? ownerId))
            {
                if (ownerId is int id)
                {
                    if (!playersById.TryGetValue(id, out Player? ownerPlayer))
                        throw Inconsistent($"Space '{space.Id}' references an unknown owner.");
                    owner = ConsoleText.Sanitize(ownerPlayer.Name);
                }
                else
                {
                    owner = "unowned";
                }
            }

            string[] capabilities = definition.Capabilities.Entries
                .Select(capability => DescribeCapability(capability, game, presentation, deckDefinitions))
                .ToArray();
            return new ConsoleSpaceProjection(
                space.Index,
                presentation.GetDisplayText(space.PresentationToken),
                presentation.GetColor(space.PresentationToken),
                Array.AsReadOnly(occupants),
                owner,
                Array.AsReadOnly(capabilities));
        }).ToArray();

        ConsoleDeckProjection[] decks = game.Decks.Entries
            .OrderBy(deck => deck.Id)
            .Select(deck => new ConsoleDeckProjection(
                deck.Id.Value,
                presentation.GetDisplayText(deck.PresentationToken),
                deck.Cards.Count))
            .ToArray();

        return new ConsoleMatchProjection(
            presentation.GetDisplayText(game.Profile.PresentationToken),
            PhaseText(game.Phase),
            game.RoundNumber,
            ConsoleText.Sanitize(game.CurrentPlayer.Name),
            game.LastDiceRoll is null
                ? null
                : $"{string.Join(" + ", game.LastDiceRoll.Results)} = {game.LastDiceRoll.Sum}",
            Array.AsReadOnly(players),
            Array.AsReadOnly(spaces),
            Array.AsReadOnly(decks),
            BuildDecision(game, presentation, playersById),
            game.Winner is null ? null : ConsoleText.Sanitize(game.Winner.Name));
    }

    private static ConsoleDecisionProjection? BuildDecision(
        Game game,
        ConsolePresentationResolver presentation,
        IReadOnlyDictionary<int, Player> players)
    {
        if (game.PendingDecision is null) return null;
        if (game.PendingDecision is not PurchaseDecision purchase || purchase.Kind != DecisionKinds.Purchase)
        {
            throw new ConsoleProjectionException(
                ConsoleProjectionErrorKind.UnsupportedDecision,
                $"Decision '{game.PendingDecision.Kind}' is not supported by the Console baseline.");
        }
        if (!players.TryGetValue(purchase.PlayerId, out Player? player))
            throw Inconsistent("The pending decision references an unknown player.");

        PresentationToken resourceToken = ResourceToken(game.Profile, purchase.Price.ResourceId);
        string prompt = $"{ConsoleText.Sanitize(player.Name)} may acquire " +
            $"{presentation.GetDisplayText(purchase.PresentationToken)} for " +
            $"{presentation.FormatAmount(purchase.Price.Value, resourceToken)}.";
        ConsoleDecisionOptionProjection[] options = purchase.AllowedResponses.Select(option => option switch
        {
            var id when id == DecisionOptions.Accept => new ConsoleDecisionOptionProjection(id, "Accept"),
            var id when id == DecisionOptions.Decline => new ConsoleDecisionOptionProjection(id, "Decline"),
            _ => throw new ConsoleProjectionException(
                ConsoleProjectionErrorKind.UnsupportedDecision,
                $"Decision option '{option}' is not supported by the Console baseline.")
        }).ToArray();

        return new ConsoleDecisionProjection(
            purchase.DecisionId,
            purchase.PlayerId,
            prompt,
            Array.AsReadOnly(options));
    }

    private static string DescribeCapability(
        CapabilityDefinition capability,
        Game game,
        ConsolePresentationResolver presentation,
        IReadOnlyDictionary<DeckId, DeckDefinition> decks) => capability switch
        {
            MoveCapabilityDefinition => "movement",
            OwnableCapabilityDefinition ownable => ownable.GroupId is null
                ? "ownable"
                : $"ownable ({ownable.GroupId.Value.Value})",
            PurchasableCapabilityDefinition purchasable =>
                $"price {presentation.FormatAmount(purchasable.Price.Value, ResourceToken(game.Profile, purchasable.Price.ResourceId))}",
            UsageFeeCapabilityDefinition fee =>
                $"usage fee {presentation.FormatAmount(fee.Amount.Value, ResourceToken(game.Profile, fee.Amount.ResourceId))}",
            DrawCapabilityDefinition draw when decks.TryGetValue(draw.DeckId, out DeckDefinition? deck) =>
                $"draw from {presentation.GetDisplayText(deck.PresentationToken)}",
            DrawCapabilityDefinition draw => throw Inconsistent($"Draw capability references unknown deck '{draw.DeckId}'."),
            _ => throw new ConsoleProjectionException(
                ConsoleProjectionErrorKind.UnsupportedCapability,
                $"Capability '{capability.Id}' is not supported by the Console baseline.")
        };

    internal static PresentationToken ResourceToken(ValidatedGameProfile profile, ResourceId resourceId)
    {
        ProfileResourceDefinition? resource = profile.Resources.SingleOrDefault(entry => entry.Id == resourceId);
        return resource?.PresentationToken ?? throw Inconsistent($"Resource '{resourceId}' has no profile definition.");
    }

    private static string PhaseText(GamePhase phase) => phase switch
    {
        GamePhase.ReadyForTurn => "Ready for turn",
        GamePhase.AwaitingDecision => "Awaiting decision",
        GamePhase.GameOver => "Game over",
        _ => throw Inconsistent("The match has an unknown phase.")
    };

    private static ConsoleProjectionException Inconsistent(string message) =>
        new(ConsoleProjectionErrorKind.InconsistentState, message);
}
