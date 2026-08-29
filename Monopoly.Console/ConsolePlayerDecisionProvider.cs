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
    private readonly ConsolePresentationResolver _presentation;

    internal ConsolePlayerDecisionProvider(ConsolePrinter printer, Input input, Game game, IGameSaveStore saveStore)
    {
        _printer = printer ?? throw new ArgumentNullException(nameof(printer));
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _saveStore = saveStore ?? throw new ArgumentNullException(nameof(saveStore));
        _presentation = new ConsolePresentationResolver(game.Presentation);
    }

    internal DecisionResponse GetResponse(PendingDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        Player player = _game.Players.Single(candidate => candidate.Id == decision.PlayerId);

        DecisionOptionId response = decision switch
        {
            PurchaseDecision purchase => GetPurchaseResponse(purchase),
            StatusDecision status => GetStatusResponse(player, status),
            _ => throw new ArgumentOutOfRangeException(nameof(decision), "Unsupported pending decision type.")
        };

        return new DecisionResponse(decision.DecisionId, response);
    }

    public bool ResolveInsufficientFunds(Game game, Player player, int amount)
    {
        if (player.Money >= amount) return true;

        int moneyBefore = player.Money;
        string required = _presentation.FormatAmount(amount, game.Rules.PrimaryResourcePresentationToken);
        _printer.PrintText($"{player.Name} does not have enough money; {required} is required.");
        new PlayerActionMenu(game, player, _saveStore).DisplayPlayerActionRealEstateMenu(true);
        return player.Money > moneyBefore;
    }

    private DecisionOptionId GetPurchaseResponse(PurchaseDecision decision)
    {
        var square = _game.Board.GetSquare(decision.SpaceId);
        string squareName = _presentation.GetDisplayText(square.PresentationToken);
        string price = _presentation.FormatAmount(decision.Price.Value, _game.Rules.PrimaryResourcePresentationToken);
        _printer.PrintText($"Do you want to buy {squareName} for {price}?");
        return _input.GetUserConfirmation() ? DecisionOptions.Accept : DecisionOptions.Decline;
    }

    private DecisionOptionId GetStatusResponse(Player player, StatusDecision decision)
    {
        string releaseMethod = decision.HasAlternative
            ? "use a Get Out of Jail For Free card"
            : $"pay {_presentation.FormatAmount(decision.Cost.Value, _game.Rules.PrimaryResourcePresentationToken)}";
        _printer.PrintText($"{player.Name}, do you want to {releaseMethod} instead of rolling for doubles?");
        return _input.GetUserConfirmation() ? DecisionOptions.Resolve : DecisionOptions.Roll;
    }

}
