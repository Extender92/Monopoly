using System;

namespace Monopoly.Core.Models.FortuneCard
{
    internal class USChanceCard : IChanceCard
    {
        public string Info { get; }
        public USChanceCardType CardType { get; }

        public USChanceCard(string info, USChanceCardType cardType)
        {
            Info = info;
            CardType = cardType;
        }

        public void ExecuteEffect(Player player)
        {
            // Implement logic specific to the US version
            switch (CardType)
            {
                case USChanceCardType.AdvanceToBoardwalk:
                    // Logic for Advance to Boardwalk
                    break;
                case USChanceCardType.AdvanceToGo:
                    // Logic for Advance to Go (Collect $200)
                    break;
                case USChanceCardType.AdvanceToIllinoisAvenue:
                    // Logic for Advance to Illinois Avenue. If you pass Go, collect $200
                    break;
                case USChanceCardType.AdvanceToStCharlesPlace:
                    // Logic for Advance to St. Charles Place. If you pass Go, collect $200
                    break;
                case USChanceCardType.AdvanceToNearestRailroad:
                    // Logic for Advance to the nearest Railroad. If unowned, you may buy it from the Bank. If owned, pay owner twice the rental.
                    break;
                case USChanceCardType.AdvanceToNearestUtility:
                    // Logic for Advance token to nearest Utility. If unowned, you may buy it from the Bank. If owned, throw dice and pay owner a total ten times amount thrown.
                    break;
                case USChanceCardType.BankPaysDividend:
                    // Logic for Bank pays you dividend of $50
                    break;
                case USChanceCardType.GetOutOfJailFree:
                    // Logic for Get Out of Jail Free
                    break;
                case USChanceCardType.GoBack3Spaces:
                    // Logic for Go Back 3 Spaces
                    break;
                case USChanceCardType.GoToJail:
                    // Logic for Go to Jail. Go directly to Jail, do not pass Go, do not collect $200
                    break;
                case USChanceCardType.MakeGeneralRepairs:
                    // Logic for Make general repairs on all your property. For each house pay $25. For each hotel pay $100
                    break;
                case USChanceCardType.SpeedingFine:
                    // Logic for Speeding fine $15
                    break;
                case USChanceCardType.TakeTripToReadingRailroad:
                    // Logic for Take a trip to Reading Railroad. If you pass Go, collect $200
                    break;
                case USChanceCardType.ElectedChairmanOfTheBoard:
                    // Logic for You have been elected Chairman of the Board. Pay each player $50
                    break;
                case USChanceCardType.BuildingLoanMatures:
                    // Logic for Your building loan matures. Collect $150
                    break;
                default:
                    // Handle any default case or unrecognized card types
                    break;
            }
        }

        public enum USChanceCardType
        {
            AdvanceToBoardwalk,
            AdvanceToGo,
            AdvanceToIllinoisAvenue,
            AdvanceToStCharlesPlace,
            AdvanceToNearestRailroad,
            AdvanceToNearestUtility,
            BankPaysDividend,
            GetOutOfJailFree,
            GoBack3Spaces,
            GoToJail,
            MakeGeneralRepairs,
            SpeedingFine,
            TakeTripToReadingRailroad,
            ElectedChairmanOfTheBoard,
            BuildingLoanMatures
        }
    }
}
