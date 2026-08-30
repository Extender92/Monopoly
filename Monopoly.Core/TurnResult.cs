using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Randomness;

namespace Monopoly.Core;

public sealed class TurnResult
{
    public Player Player { get; init; } = null!;
    public DiceRoll Roll { get; init; } = null!;
    public IReadOnlyList<int> DiceResults => Roll.Results;
    public int DiceSum => Roll.Sum;
    public bool WasDouble => Roll.IsDouble;
    public SpaceView LandedSpace { get; init; } = null!;
    public bool GameOver { get; init; }
    public Player? Winner { get; init; }
}
