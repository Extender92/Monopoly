using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    public class GameRules
    {
        public int NumberOfPlayers { get; set; }
        public int NumberOfDice { get; set; }
        public int DieSides { get; set; }
        private Language _gameLanguage = Language.UK;
        public Language GameLanguage
        {
            get => _gameLanguage;
            set
            {
                _gameLanguage = value;
                SetCurrencySymbol();
            }
        }
        public string CurrencySymbol { get; private set; } = "£";
        public int Salary { get; set; }
        public bool DoubleOnGo { get; set; }
        public Parking FreeParking { get; set; }
        public int MortgageInterestRate { get; set; }
        public int JailFine { get; set; }
        public int MaxTurnsInJail { get; set; }


        public GameRules(int numberOfPlayers, int numberOfDice, int dieSides)
        {
            NumberOfPlayers = numberOfPlayers;
            NumberOfDice = numberOfDice;
            DieSides = dieSides;
            GameLanguage = Language.UK;
            Salary = 200;
            MortgageInterestRate = 10;
            SetCurrencySymbol();
            JailFine = 50;
            MaxTurnsInJail = 3;
        }

        public void SetLanguage(Language language) => GameLanguage = language;

        private void SetCurrencySymbol()
        {
            switch (GameLanguage)
            {
                case GameRules.Language.UK:
                    CurrencySymbol = "£";
                    break;
                case GameRules.Language.US:
                    CurrencySymbol = "$";
                    break;
                default:
                    CurrencySymbol = "M";
                    break;
            }
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
