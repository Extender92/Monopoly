using Monopoly.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.FortuneCard
{
    internal sealed class USCommunityChestCard : ICommunityChestCard
    {
        public string Info { get; }
        public USCommunityChestCardType CardType { get; }

        public USCommunityChestCard(string info, USCommunityChestCardType cardType)
        {
            Info = info;
            CardType = cardType;
        }

        void ICommunityChestCard.ExecuteEffect(Player player, Game game)
        {
            // Implement logic specific to the US version
            switch (CardType)
            {
                case USCommunityChestCardType.AdvanceToGo:
                    AdvanceToGo(player, game);
                    break;
                case USCommunityChestCardType.BankErrorInYourFavour:
                    BankErrorInYourFavour(player, game);
                    break;
                case USCommunityChestCardType.DoctorsFee:
                    DoctorsFee(player, game);
                    break;
                case USCommunityChestCardType.FromSaleOfStock:
                    FromSaleOfStock(player, game);
                    break;
                case USCommunityChestCardType.GetOutOfJailFree:
                    GetOutOfJailFree(player, game);
                    break;
                case USCommunityChestCardType.GoToJail:
                    GoToJail(player, game);
                    break;
                case USCommunityChestCardType.HolidayFundMatures:
                    HolidayFundMatures(player, game);
                    break;
                case USCommunityChestCardType.IncomeTaxRefund:
                    IncomeTaxRefund(player, game);
                    break;
                case USCommunityChestCardType.ItIsYourBirthday:
                    ItIsYourBirthday(player, game);
                    break;
                case USCommunityChestCardType.LifeInsuranceMatures:
                    LifeInsuranceMatures(player, game);
                    break;
                case USCommunityChestCardType.PayHospitalFees:
                    PayHospitalFees(player, game);
                    break;
                case USCommunityChestCardType.PaySchoolFees:
                    PaySchoolFees(player, game);
                    break;
                case USCommunityChestCardType.ReceiveConsultancyFee:
                    ReceiveConsultancyFee(player, game);
                    break;
                case USCommunityChestCardType.AssessedForStreetRepairs:
                    AssessedForStreetRepairs(player, game);
                    break;
                case USCommunityChestCardType.WonSecondPrizeInBeautyContest:
                    WonSecondPrizeInBeautyContest(player, game);
                    break;
                case USCommunityChestCardType.Inherit:
                    Inherit(player, game);
                    break;
                default:
                    // Handle any default case or unrecognized card types
                    break;
            }
        }

        private void AdvanceToGo(Player player, Game game)
        {
            int newPosition = game.Handler.GetPlayerGoPastGoNewPosition(0);
            game.Handler.MovePlayerAndInvokeEvent(player, newPosition);
            var square = game.Board.GetSquareAtPosition(player.Position);
            square.LandOn(player, game);
        }

        private void BankErrorInYourFavour(Player player, Game game)
        {
            game.Transactions.GetMoneyFromBank(player, 200);
        }

        private void DoctorsFee(Player player, Game game)
        {
            game.Handler.TryResolvePayment(player, 50, null, "Could not afford Doctor's fee", true);
        }

        private void FromSaleOfStock(Player player, Game game)
        {
            game.Transactions.GetMoneyFromBank(player, 50);
        }

        private void GetOutOfJailFree(Player player, Game game)
        {
            player.AddJailCards();
            game.LogWriter.CreateLog($"{player.Name} got a new Get Out Of Jail Card, and now has {player.NumberOfGetOutOFJailCards} Get Out Of Jail {(player.NumberOfGetOutOFJailCards == 1 ? "Card" : "Cards")}.");
        }

        private void GoToJail(Player player, Game game)
        {
            game.TheJail.PlayerGoToJail(player);
        }

        private void HolidayFundMatures(Player player, Game game)
        {
            game.Transactions.GetMoneyFromBank(player, 100);
        }

        private void IncomeTaxRefund(Player player, Game game)
        {
            game.Transactions.GetMoneyFromBank(player, 20);
        }

        private void ItIsYourBirthday(Player player, Game game)
        {
            foreach (var gamePlayer in game.Players.ToList())
            {
                if (player != gamePlayer)
                {
                    game.Handler.TryResolvePayment(gamePlayer, 10, player, "Could not pay birthday gift");
                }
            }
        }

        private void LifeInsuranceMatures(Player player, Game game)
        {
            game.Transactions.GetMoneyFromBank(player, 100);
        }

        private void PayHospitalFees(Player player, Game game)
        {
            game.Handler.TryResolvePayment(player, 100, null, "Could not afford hospital fees", true);
        }

        private void PaySchoolFees(Player player, Game game)
        {
            game.Handler.TryResolvePayment(player, 50, null, "Could not afford school fees", true);
        }

        private void ReceiveConsultancyFee(Player player, Game game)
        {
            game.Transactions.GetMoneyFromBank(player, 25);
        }

        private void AssessedForStreetRepairs(Player player, Game game)
        {
            int houses = 0;
            int hotels = 0;
            var propertiesWithHousesList = game.Board.GetAllPropertySquaresPlayerCanSellHousesIn(player);

            foreach (var property in propertiesWithHousesList)
            {
                if (property.Houses == 5) hotels++;
                else if (property.Houses > 0) houses += property.Houses;
            }

            int sumToPay = (houses * 40) + (hotels * 115);

            game.Handler.TryResolvePayment(player, sumToPay, null, $"Could not afford street repairs of {sumToPay}");
        }

        private void WonSecondPrizeInBeautyContest(Player player, Game game)
        {
            game.Transactions.GetMoneyFromBank(player, 10);
        }

        private void Inherit(Player player, Game game)
        {
            game.Transactions.GetMoneyFromBank(player, 100);
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
