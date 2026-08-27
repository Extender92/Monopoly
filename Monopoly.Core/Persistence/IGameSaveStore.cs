using Monopoly.Core.Interface;

namespace Monopoly.Core.Persistence;

public interface IGameSaveStore
{
    void Save(Game game);

    Game Load(IPlayerDecisionProvider? decisions = null);
}
