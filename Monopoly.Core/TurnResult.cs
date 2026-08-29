using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Randomness;

namespace Monopoly.Core;

public sealed class TurnResult
{
    private IReadOnlyList<StatusTransition>? _statusTransitions;

    public Player Player { get; init; } = null!;
    public DiceRoll? Roll { get; init; }
    public IReadOnlyList<int> DiceResults => Roll?.Results ?? Array.Empty<int>();
    public int DiceSum => Roll?.Sum ?? 0;
    internal Square? LandedSquare { get; init; }
    public SpaceView? LandedSpace => LandedSquare?.CreateView();
    public bool WasDouble => Roll?.IsDouble ?? false;
    internal bool WasSentToJail { get; init; }
    internal bool WasReleasedFromJailByDouble { get; init; }
    internal bool WasStatusRemoved { get; init; }
    public IReadOnlyList<StatusTransition> StatusTransitions
    {
        get
        {
            if (_statusTransitions is not null) return _statusTransitions;
            List<StatusTransition> transitions = [];
            if (WasReleasedFromJailByDouble || WasStatusRemoved)
                transitions.Add(new StatusTransition(Player.Id, LegacyStatusIds.Detained, StatusTransitionKind.Removed));
            if (WasSentToJail)
                transitions.Add(new StatusTransition(Player.Id, LegacyStatusIds.Detained, StatusTransitionKind.Applied));
            return transitions.Count == 0
                ? Array.Empty<StatusTransition>()
                : Array.AsReadOnly(transitions.ToArray());
        }
        init => _statusTransitions = value is null
            ? throw new ArgumentNullException(nameof(value))
            : Array.AsReadOnly(value.ToArray());
    }
    public bool ExtraTurn { get; init; }
    public bool PlayerBankrupt { get; init; }
    public bool GameOver { get; init; }
    public Player? Winner { get; init; }
}
