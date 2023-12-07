using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Monopoly.Core.Models.FortuneCard.UKChanceCard;

namespace Monopoly.Core.Models.FortuneCard
{
    internal class UKCommunityChestCard : ICommunityChestCard
    {
        public string Info { get; }
        public UKCommunityChestCardType CardType { get; }

        public UKCommunityChestCard(string info, UKCommunityChestCardType cardType)
        {
            Info = info;
            CardType = cardType;
        }

        public void ExecuteEffect(Player player)
        {
            // Implement logic specific to the UK version
            switch (CardType)
            {
                case UKCommunityChestCardType.AdvanceToGo:
                    // Logic for Advance to Go
                    break;
                case UKCommunityChestCardType.BankErrorInYourFavour:
                    // Logic for Bank error in your favor
                    break;
                case UKCommunityChestCardType.DoctorsFee:
                    // Logic for Doctor’s fee
                    break;
                case UKCommunityChestCardType.FromSaleOfStock:
                    // Logic for From sale of stock you get
                    break;
                case UKCommunityChestCardType.GetOutOfJailFree:
                    // Logic for Get Out of Jail Free
                    break;
                case UKCommunityChestCardType.GoToJail:
                    // Logic for Go to Jail
                    break;
                case UKCommunityChestCardType.HolidayFundMatures:
                    // Logic for Holiday fund matures
                    break;
                case UKCommunityChestCardType.IncomeTaxRefund:
                    // Logic for Income tax refund
                    break;
                case UKCommunityChestCardType.ItIsYourBirthday:
                    // Logic for It is your birthday
                    break;
                case UKCommunityChestCardType.LifeInsuranceMatures:
                    // Logic for Life insurance matures
                    break;
                case UKCommunityChestCardType.PayHospitalFees:
                    // Logic for Pay hospital fees
                    break;
                case UKCommunityChestCardType.PaySchoolFees:
                    // Logic for Pay school fees
                    break;
                case UKCommunityChestCardType.ReceiveConsultancyFee:
                    // Logic for Receive £25 consultancy fee
                    break;
                case UKCommunityChestCardType.AssessedForStreetRepairs:
                    // Logic for You are assessed for street repairs
                    break;
                case UKCommunityChestCardType.WonSecondPrizeInBeautyContest:
                    // Logic for You have won second prize in a beauty contest
                    break;
                case UKCommunityChestCardType.Inherit:
                    // Logic for You inherit £100
                    break;
                default:
                    // Handle any default case or unrecognized card types
                    break;
            }
        }

        public enum UKCommunityChestCardType
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