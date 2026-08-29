using System.Collections.ObjectModel;

namespace Monopoly.Core.Randomness;

/// <summary>An immutable, committed dice outcome with its rule purpose.</summary>
public sealed class DiceRoll
{
    private readonly ReadOnlyCollection<int> _results;

    internal DiceRoll(RandomPurpose purpose, IEnumerable<int> results, int dieSides)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (!IsDicePurpose(purpose))
            throw new ArgumentException("The random purpose does not describe a dice roll.", nameof(purpose));
        if (dieSides <= 0) throw new ArgumentOutOfRangeException(nameof(dieSides));

        int[] copiedResults = results.ToArray();
        if (copiedResults.Length == 0 || copiedResults.Any(result => result < 1 || result > dieSides))
            throw new ArgumentOutOfRangeException(nameof(results));

        Purpose = purpose;
        _results = Array.AsReadOnly(copiedResults);
        Sum = checked(copiedResults.Sum());
        IsDouble = copiedResults.Length >= 2 && copiedResults.All(result => result == copiedResults[0]);
    }

    public RandomPurpose Purpose { get; }
    public IReadOnlyList<int> Results => _results;
    public int Sum { get; }
    public bool IsDouble { get; }

    internal static bool IsDicePurpose(RandomPurpose purpose) =>
        purpose is RandomPurpose.TurnDice or
            RandomPurpose.DetentionDice or
            RandomPurpose.DedicatedRuleDice or
            RandomPurpose.SetupDice;
}
