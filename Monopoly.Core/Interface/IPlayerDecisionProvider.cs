using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;

namespace Monopoly.Core.Interface;

/// <summary>
/// Supplies decisions that cannot be made by the rules engine itself.
/// A frontend may implement this interface for a console, web or desktop UI.
/// </summary>
public interface IPlayerDecisionProvider
{
    bool ConfirmPurchase(Player player, Square square);
    bool ConfirmJailBuyout(Player player);
    bool ResolveInsufficientFunds(Game game, Player player, int amount);
}

public sealed class DefaultPlayerDecisionProvider : IPlayerDecisionProvider
{
    public bool ConfirmPurchase(Player player, Square square) => false;

    public bool ConfirmJailBuyout(Player player) => false;

    public bool ResolveInsufficientFunds(Game game, Player player, int amount) => false;
}
