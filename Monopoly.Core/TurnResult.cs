using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Randomness;

namespace Monopoly.Core;

public sealed class TurnResult
{
    public Player Player { get; init; } = null!;
    public DiceRoll? Roll { get; init; }
    public IReadOnlyList<int> DiceResults => Roll?.Results ?? Array.Empty<int>();
    public int DiceSum => Roll?.Sum ?? 0;
    public Square? LandedSquare { get; init; }
    public bool WasDouble => Roll?.IsDouble ?? false;
    public bool WasSentToJail { get; init; }
    public bool WasReleasedFromJailByDouble { get; init; }
    public bool ExtraTurn { get; init; }
    public bool PlayerBankrupt { get; init; }
    public bool GameOver { get; init; }
    public Player? Winner { get; init; }
}
