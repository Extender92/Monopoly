using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.FortuneCard
{
    internal class UKChanceCard : IChanceCard
    {
        public string Info { get; }
        public UKChanceCardType CardType { get; }

        public UKChanceCard(string info, UKChanceCardType cardType)
        {
            Info = info;
            CardType = cardType;
        }

        public void ExecuteEffect(Player player)
        {
            // Implement logic specific to the UK version
            switch (CardType)
            {
                case UKChanceCardType.AdvanceToGo:
                    // Logic for Advance to Go
                    break;
                case UKChanceCardType.AdvanceToTrafalgarSquare:
                    // Logic for Advance to Trafalgar Square
                    break;
                case UKChanceCardType.AdvanceToMayfair:
                    // Logic for Advance to Mayfair
                    break;
                case UKChanceCardType.AdvanceToPallMall:
                    // Logic for Advance to Pall Mall
                    break;
                case UKChanceCardType.AdvanceToNearestStation:
                    // Logic for Advance to the Nearest Station
                    break;
                case UKChanceCardType.AdvanceTokenToNearestUtility:
                    // Logic for Advance token to the Nearest Utility
                    break;
                case UKChanceCardType.BankPaysDividend:
                    // Logic for Bank pays dividend
                    break;
                case UKChanceCardType.GetOutOfJailFree:
                    // Logic for Get Out of Jail Free
                    break;
                case UKChanceCardType.GoBack3Spaces:
                    // Logic for Go Back 3 Spaces
                    break;
                case UKChanceCardType.GoToJail:
                    // Logic for Go to Jail
                    break;
                case UKChanceCardType.MakeGeneralRepairs:
                    // Logic for Make general repairs
                    break;
                case UKChanceCardType.SpeedingFine:
                    // Logic for Speeding fine
                    break;
                case UKChanceCardType.TakeTripToKingsCrossStation:
                    // Logic for Take a trip to Kings Cross Station
                    break;
                case UKChanceCardType.ElectedChairmanOfTheBoard:
                    // Logic for You have been elected Chairman of the Board
                    break;
                case UKChanceCardType.BuildingLoanMatures:
                    // Logic for Your building loan matures
                    break;
                default:
                    // Handle any default case or unrecognized card types
                    break;
            }
        }

        public enum UKChanceCardType
        {
            AdvanceToGo,
            AdvanceToTrafalgarSquare,
            AdvanceToMayfair,
            AdvanceToPallMall,
            AdvanceToNearestStation,
            AdvanceTokenToNearestUtility,
            BankPaysDividend,
            GetOutOfJailFree,
            GoBack3Spaces,
            GoToJail,
            MakeGeneralRepairs,
            SpeedingFine,
            TakeTripToKingsCrossStation,
            ElectedChairmanOfTheBoard,
            BuildingLoanMatures,
        }
    }
}
