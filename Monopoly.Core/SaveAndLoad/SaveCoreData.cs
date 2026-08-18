using Monopoly.Core.Interface;

namespace Monopoly.Core.SaveAndLoad;

/// <summary>Compatibility facade for callers of the old save API.</summary>
public static class SaveCoreData
{
    public static void SaveData(IGame game, string filePath = "game_data.json")
    {
        if (game is not Game concreteGame)
            throw new ArgumentException("The game must be a Monopoly.Core.Game instance.", nameof(game));

        GameStateSerializer.Save(concreteGame, filePath);
    }
}
