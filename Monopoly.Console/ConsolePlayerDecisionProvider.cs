using Monopoly.Console.GUI;
using Monopoly.Core;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;

namespace Monopoly.Console;

internal sealed class ConsolePlayerDecisionProvider : IPlayerDecisionProvider
{
    private readonly ConsolePrinter _printer;
    private readonly Input _input;
    private readonly GameRules _rules;

    internal ConsolePlayerDecisionProvider(ConsolePrinter printer, Input input, GameRules rules)
    {
        _printer = printer;
        _input = input;
        _rules = rules;
    }

    public bool ConfirmPurchase(Player player, Square square)
    {
        _printer.PrintText($"Do you want to buy {square.Name} for {square.Price}{_rules.CurrencySymbol}?");
        return _input.GetUserConfirmation();
    }

    public bool ConfirmJailBuyout(Player player)
    {
        _printer.PrintText($"{player.Name}, do you want to buy yourself out of jail for 50{_rules.CurrencySymbol}?");
        return _input.GetUserConfirmation();
    }

    public bool ResolveInsufficientFunds(Game game, Player player, int amount)
    {
        if (player.Money >= amount) return true;

        int moneyBefore = player.Money;
        _printer.PrintText($"{player.Name} does not have enough money; {amount}{game.Rules.CurrencySymbol} is required.");
        new PlayerActionMenu(game, player).DisplayPlayerActionRealEstateMenu(true);
        return player.Money > moneyBefore;
    }

}
