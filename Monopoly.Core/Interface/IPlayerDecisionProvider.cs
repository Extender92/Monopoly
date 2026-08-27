using Monopoly.Core.Models;

namespace Monopoly.Core.Interface;

/// <summary>
/// Transitional callback for raising cash while the resumable debt flow is not
/// yet implemented. Purchase and Jail choices use pending match decisions.
/// </summary>
public interface IPlayerDecisionProvider
{
    bool ResolveInsufficientFunds(Game game, Player player, int amount);
}

public sealed class DefaultPlayerDecisionProvider : IPlayerDecisionProvider
{
    public bool ResolveInsufficientFunds(Game game, Player player, int amount) => false;
}
