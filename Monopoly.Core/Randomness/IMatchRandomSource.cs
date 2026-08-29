namespace Monopoly.Core.Randomness;

/// <summary>Supplies nondeterministic choices for exactly the match that receives it.</summary>
public interface IMatchRandomSource
{
    int NextInt(RandomRequest request);
}
