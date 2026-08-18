using Monopoly.Core.Interface;

namespace Monopoly.Core.SaveAndLoad;

/// <summary>Compatibility facade for callers of the old load API.</summary>
public static class LoadCoreData
{
    public static Game LoadGame(string filePath = "game_data.json", IPlayerDecisionProvider? decisions = null)
        => GameStateSerializer.Load(filePath, decisions);

    public static void LoadData(IGame target, string filePath = "game_data.json")
    {
        if (target is not Game targetGame)
            throw new ArgumentException("The game must be a Monopoly.Core.Game instance.", nameof(target));

        Game loaded = LoadGame(filePath, targetGame.Decisions);
        targetGame.Board = loaded.Board;
        targetGame.Players = loaded.Players;
        targetGame.CurrentPlayer = loaded.CurrentPlayer;
        targetGame.Dice = loaded.Dice;
        targetGame.Rules = loaded.Rules;
        targetGame.TheJail = new Jail(targetGame, loaded.TheJail.JailPosition);
        foreach (var jailEntry in loaded.TheJail.playersInJail)
            targetGame.TheJail.RestorePlayerInJail(jailEntry.Key, jailEntry.Value.TurnsInJail);
        targetGame.FortuneCard = loaded.FortuneCard;
        targetGame.Fines = loaded.Fines;
        targetGame.CurrentTurn = loaded.CurrentTurn;
        targetGame.Handler = new GameHandler(targetGame);
        targetGame.Transactions = new Transaction(targetGame);
        targetGame.RestoreConsecutiveDoubles(loaded.ConsecutiveDoubles);
        targetGame.RestoreWinner(loaded.Winner);
    }
}
