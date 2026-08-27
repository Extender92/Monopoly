using Monopoly.Console.GUI;
using Monopoly.Core;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Persistence;

namespace Monopoly.Console;

internal sealed class ConsolePlayerDecisionProvider : IPlayerDecisionProvider
{
    private readonly ConsolePrinter _printer;
    private readonly Input _input;
    private readonly Game _game;
    private readonly IGameSaveStore _saveStore;

    internal ConsolePlayerDecisionProvider(ConsolePrinter printer, Input input, Game game, IGameSaveStore saveStore)
    {
        _printer = printer ?? throw new ArgumentNullException(nameof(printer));
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _saveStore = saveStore ?? throw new ArgumentNullException(nameof(saveStore));
    }

    internal DecisionResponse GetResponse(PendingDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        Player player = _game.Players.Single(candidate => candidate.Id == decision.PlayerId);

        DecisionOption response = decision switch
        {
            PropertyPurchaseDecision purchase => GetPurchaseResponse(purchase),
            JailReleaseDecision jail => GetJailResponse(player, jail),
            _ => throw new ArgumentOutOfRangeException(nameof(decision), "Unsupported pending decision type.")
        };

        return new DecisionResponse(decision.DecisionId, response);
    }

    public bool ResolveInsufficientFunds(Game game, Player player, int amount)
    {
        if (player.Money >= amount) return true;

        int moneyBefore = player.Money;
        _printer.PrintText($"{player.Name} does not have enough money; {amount}{game.Rules.CurrencySymbol} is required.");
        new PlayerActionMenu(game, player, _saveStore).DisplayPlayerActionRealEstateMenu(true);
        return player.Money > moneyBefore;
    }

    private DecisionOption GetPurchaseResponse(PropertyPurchaseDecision decision)
    {
        string squareName = _game.Board.GetSquareAtPosition(decision.SquarePosition).Name;
        _printer.PrintText($"Do you want to buy {squareName} for {decision.Price}{_game.Rules.CurrencySymbol}?");
        return _input.GetUserConfirmation() ? DecisionOption.Purchase : DecisionOption.Decline;
    }

    private DecisionOption GetJailResponse(Player player, JailReleaseDecision decision)
    {
        string releaseMethod = decision.HasGetOutOfJailCard
            ? "use a Get Out of Jail For Free card"
            : $"pay {decision.Fine}{_game.Rules.CurrencySymbol}";
        _printer.PrintText($"{player.Name}, do you want to {releaseMethod} instead of rolling for doubles?");
        return _input.GetUserConfirmation() ? DecisionOption.LeaveJail : DecisionOption.RollForDoubles;
    }

}
