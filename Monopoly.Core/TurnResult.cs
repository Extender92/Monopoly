using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;

namespace Monopoly.Core;

public sealed class TurnResult
{
    public Player Player { get; init; } = null!;
    public IReadOnlyList<int> DiceResults { get; init; } = Array.Empty<int>();
    public int DiceSum { get; init; }
    public Square? LandedSquare { get; init; }
    public bool WasDouble { get; init; }
    public bool WasSentToJail { get; init; }
    public bool WasReleasedFromJailByDouble { get; init; }
    public bool ExtraTurn { get; init; }
    public bool PlayerBankrupt { get; init; }
    public bool GameOver { get; init; }
    public Player? Winner { get; init; }
}
