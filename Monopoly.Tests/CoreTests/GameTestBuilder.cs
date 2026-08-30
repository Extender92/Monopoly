using System.Text.Json;
using Monopoly.Core.Persistence;
using Monopoly.Tests.TestDoubles;

namespace Monopoly.Tests.CoreTests;

/// <summary>Small profile-based test composition; it is not distributed product data.</summary>
internal sealed class GameTestBuilder
{
    internal Game Build() => GameSetup.Create(
        GameProfileValidator.Validate(ProfileTestFactory.Create()),
        [new PlayerSetup(0, "Synthetic Player")],
        new ScriptedMatchRandomSource());
}

internal static class GameTestSnapshot
{
    internal static string Capture(Game game) => JsonSerializer.Serialize(new
    {
        Profile = new { game.Profile.Id, game.Profile.Revision, game.Profile.Fingerprint },
        Players = game.Players.Select(player => new
        {
            player.Id,
            player.Name,
            player.Position,
            player.CurrentSpaceId,
            Resources = player.Resources.OrderBy(entry => entry.Key).Select(entry => new { entry.Key, entry.Value })
        }),
        CurrentPlayer = game.CurrentPlayer.Id,
        game.RoundNumber,
        game.Phase,
        PendingDecision = game.PendingDecision?.DecisionId,
        game.LastDiceRoll,
        Ownership = game.Ownership.Entries,
        Decks = game.Decks.Entries.Select(deck => new { deck.Id, Cards = deck.Cards.Select(card => card.Id) }),
        Progress = GameProgressStateMapper.ToState(game),
        Winner = game.Winner?.Id
    });
}
