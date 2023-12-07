using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.FortuneCard
{
    internal class USCommunityChestCard : ICommunityChestCard
    {
        public string Info { get; }
        public USCommunityChestCardType CardType { get; }

        public USCommunityChestCard(string info, USCommunityChestCardType cardType)
        {
            Info = info;
            CardType = cardType;
        }

        public void ExecuteEffect(Player player)
        {
            // Implement logic specific to the US version
            switch (CardType)
            {
                case USCommunityChestCardType.AdvanceToGo:
                    // Logic for Advance to Go
                    break;
                case USCommunityChestCardType.BankErrorInYourFavour:
                    // Logic for Bank error in your favor
                    break;
                case USCommunityChestCardType.DoctorsFee:
                    // Logic for Doctor’s fee
                    break;
                case USCommunityChestCardType.FromSaleOfStock:
                    // Logic for From sale of stock you get
                    break;
                case USCommunityChestCardType.GetOutOfJailFree:
                    // Logic for Get Out of Jail Free
                    break;
                case USCommunityChestCardType.GoToJail:
                    // Logic for Go to Jail
                    break;
                case USCommunityChestCardType.HolidayFundMatures:
                    // Logic for Holiday fund matures
                    break;
                case USCommunityChestCardType.IncomeTaxRefund:
                    // Logic for Income tax refund
                    break;
                case USCommunityChestCardType.ItIsYourBirthday:
                    // Logic for It is your birthday
                    break;
                case USCommunityChestCardType.LifeInsuranceMatures:
                    // Logic for Life insurance matures
                    break;
                case USCommunityChestCardType.PayHospitalFees:
                    // Logic for Pay hospital fees
                    break;
                case USCommunityChestCardType.PaySchoolFees:
                    // Logic for Pay school fees
                    break;
                case USCommunityChestCardType.ReceiveConsultancyFee:
                    // Logic for Receive $25 consultancy fee
                    break;
                case USCommunityChestCardType.AssessedForStreetRepairs:
                    // Logic for You are assessed for street repairs
                    break;
                case USCommunityChestCardType.WonSecondPrizeInBeautyContest:
                    // Logic for You have won second prize in a beauty contest
                    break;
                case USCommunityChestCardType.Inherit:
                    // Logic for You inherit $100
                    break;
                default:
                    // Handle any default case or unrecognized card types
                    break;
            }
        }

        public enum USCommunityChestCardType
        {
            AdvanceToGo,
            BankErrorInYourFavour,
            DoctorsFee,
            FromSaleOfStock,
            GetOutOfJailFree,
            GoToJail,
            HolidayFundMatures,
            IncomeTaxRefund,
            ItIsYourBirthday,
            LifeInsuranceMatures,
            PayHospitalFees,
            PaySchoolFees,
            ReceiveConsultancyFee,
            AssessedForStreetRepairs,
            WonSecondPrizeInBeautyContest,
            Inherit
        }
    }
}