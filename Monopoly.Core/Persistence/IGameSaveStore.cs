using Monopoly.Core.Randomness;

namespace Monopoly.Core.Persistence;

public interface IGameSaveStore
{
    void Save(Game game);

    Game Load(IMatchRandomSource? randomSource = null);
}
