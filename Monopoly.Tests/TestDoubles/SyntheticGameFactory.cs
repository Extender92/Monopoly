using Monopoly.Core.Interface;
using Monopoly.Core.Randomness;
using Monopoly.Tests.CoreTests;

namespace Monopoly.Core;

internal static class SyntheticGameFactory
{
    internal static Game Setup(
        GameRules gameRules,
        IPlayerDecisionProvider? decisions = null,
        IMatchRandomSource? randomSource = null)
    {
        GameTestBuilder builder = new(gameRules);
        if (decisions is not null)
            builder.WithDecisions(decisions);
        if (randomSource is not null)
            builder.WithRandomSource(randomSource);
        return builder.Build();
    }
}
