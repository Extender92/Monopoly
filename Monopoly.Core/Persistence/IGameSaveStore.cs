using Monopoly.Core.Interface;
using Monopoly.Core.Randomness;

namespace Monopoly.Core.Persistence;

public interface IGameSaveStore
{
    void Save(Game game);

    Game Load(
        IPlayerDecisionProvider? decisions = null,
        IMatchRandomSource? randomSource = null);
}
