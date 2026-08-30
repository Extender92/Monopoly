using Monopoly.Core.Presentation;

namespace Monopoly.Core;

internal sealed class GameRules
{
    internal GameRules(
        int numberOfPlayers,
        int numberOfDice,
        int dieSides,
        int salary = 12,
        bool doubleOnGo = false,
        Parking freeParking = Parking.None,
        int mortgageInterestRate = 10,
        int jailFine = 8,
        int maxTurnsInJail = 3)
    {
        if (numberOfPlayers <= 0) throw new ArgumentOutOfRangeException(nameof(numberOfPlayers));
        if (numberOfDice <= 0) throw new ArgumentOutOfRangeException(nameof(numberOfDice));
        if (dieSides <= 0) throw new ArgumentOutOfRangeException(nameof(dieSides));
        if (!Enum.IsDefined(freeParking)) throw new ArgumentOutOfRangeException(nameof(freeParking));
        if (salary < 0) throw new ArgumentOutOfRangeException(nameof(salary));
        if (mortgageInterestRate < 0) throw new ArgumentOutOfRangeException(nameof(mortgageInterestRate));
        if (jailFine < 0) throw new ArgumentOutOfRangeException(nameof(jailFine));
        if (maxTurnsInJail <= 0) throw new ArgumentOutOfRangeException(nameof(maxTurnsInJail));

        NumberOfPlayers = numberOfPlayers;
        NumberOfDice = numberOfDice;
        DieSides = dieSides;
        Salary = salary;
        DoubleOnGo = doubleOnGo;
        FreeParking = freeParking;
        MortgageInterestRate = mortgageInterestRate;
        JailFine = jailFine;
        MaxTurnsInJail = maxTurnsInJail;
    }

    internal int NumberOfPlayers { get; }
    internal int NumberOfDice { get; }
    internal int DieSides { get; }
    internal PresentationToken PrimaryResourcePresentationToken => PresentationTokens.PrimaryResource;
    internal int Salary { get; }
    internal bool DoubleOnGo { get; }
    internal Parking FreeParking { get; }
    internal int MortgageInterestRate { get; }
    internal int JailFine { get; }
    internal int MaxTurnsInJail { get; }

    internal enum Parking
    {
        None = 0,
        SetFee = 100,
        Fines
    }
}
