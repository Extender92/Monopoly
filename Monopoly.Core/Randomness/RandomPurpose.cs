namespace Monopoly.Core.Randomness;

/// <summary>Identifies why Core is requesting a nondeterministic choice.</summary>
public enum RandomPurpose
{
    DeckShuffle,
    TurnDice,
    DedicatedRuleDice,
    SetupStartingPlayer,
    SetupDice
}
