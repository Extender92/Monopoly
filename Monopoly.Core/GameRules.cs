using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    public sealed class GameRules
    {
        public int NumberOfPlayers { get; }
        public int NumberOfDice { get; }
        public int DieSides { get; }
        public Language GameLanguage { get; }
        public string CurrencySymbol { get; }
        public int Salary { get; }
        public bool DoubleOnGo { get; }
        public Parking FreeParking { get; }
        public int MortgageInterestRate { get; }
        public int JailFine { get; }
        public int MaxTurnsInJail { get; }

        public GameRules(
            int numberOfPlayers,
            int numberOfDice,
            int dieSides,
            Language gameLanguage = Language.UK,
            int salary = 200,
            bool doubleOnGo = false,
            Parking freeParking = Parking.Classic,
            int mortgageInterestRate = 10,
            int jailFine = 50,
            int maxTurnsInJail = 3)
        {
            if (numberOfPlayers <= 0) throw new ArgumentOutOfRangeException(nameof(numberOfPlayers));
            if (numberOfDice <= 0) throw new ArgumentOutOfRangeException(nameof(numberOfDice));
            if (dieSides <= 0) throw new ArgumentOutOfRangeException(nameof(dieSides));
            if (!Enum.IsDefined(gameLanguage)) throw new ArgumentOutOfRangeException(nameof(gameLanguage));
            if (!Enum.IsDefined(freeParking)) throw new ArgumentOutOfRangeException(nameof(freeParking));
            if (salary < 0) throw new ArgumentOutOfRangeException(nameof(salary));
            if (mortgageInterestRate < 0) throw new ArgumentOutOfRangeException(nameof(mortgageInterestRate));
            if (jailFine < 0) throw new ArgumentOutOfRangeException(nameof(jailFine));
            if (maxTurnsInJail <= 0) throw new ArgumentOutOfRangeException(nameof(maxTurnsInJail));

            NumberOfPlayers = numberOfPlayers;
            NumberOfDice = numberOfDice;
            DieSides = dieSides;
            GameLanguage = gameLanguage;
            CurrencySymbol = gameLanguage switch
            {
                Language.UK => "£",
                Language.US => "$",
                _ => "M"
            };
            Salary = salary;
            DoubleOnGo = doubleOnGo;
            FreeParking = freeParking;
            MortgageInterestRate = mortgageInterestRate;
            JailFine = jailFine;
            MaxTurnsInJail = maxTurnsInJail;
        }

        public enum Language
        {
            UK,
            US
        }

        public enum Parking
        {
            Classic = 0,
            SetFee = 100,
            Fines
        }
    }
}
