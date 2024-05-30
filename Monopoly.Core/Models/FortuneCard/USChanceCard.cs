using Monopoly.Core.Events;
using Monopoly.Core.Models.Board;
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

        public void ExecuteEffect(Player player, Game game)
        {
            // Implement logic specific to the US version
            switch (CardType)
            {
                case USChanceCardType.AdvanceToBoardwalk:
                    AdvanceToBoardwalk(player, game);
                    break;
                case USChanceCardType.AdvanceToGo:
                    AdvanceToGo(player, game);
                    break;
                case USChanceCardType.AdvanceToIllinoisAvenue:
                    AdvanceToIllinoisAvenue(player, game);
                    break;
                case USChanceCardType.AdvanceToStCharlesPlace:
                    AdvanceToStCharlesPlace(player, game);
                    break;
                case USChanceCardType.AdvanceToNearestRailroad:
                    AdvanceToNearestRailroad(player, game);
                    break;
                case USChanceCardType.AdvanceToNearestUtility:
                    AdvanceToNearestUtility(player, game);
                    break;
                case USChanceCardType.BankPaysDividend:
                    BankPaysDividend(player, game);
                    break;
                case USChanceCardType.GetOutOfJailFree:
                    GetOutOfJailFree(player, game);
                    break;
                case USChanceCardType.GoBack3Spaces:
                    GoBack3Spaces(player, game);
                    break;
                case USChanceCardType.GoToJail:
                    GoToJail(player, game);
                    break;
                case USChanceCardType.MakeGeneralRepairs:
                    MakeGeneralRepairs(player, game);
                    break;
                case USChanceCardType.SpeedingFine:
                    SpeedingFine(player, game);
                    break;
                case USChanceCardType.TakeTripToReadingRailroad:
                    TakeTripToReadingRailroad(player, game);
                    break;
                case USChanceCardType.ElectedChairmanOfTheBoard:
                    ElectedChairmanOfTheBoard(player, game);
                    break;
                case USChanceCardType.BuildingLoanMatures:
                    BuildingLoanMatures(player, game);
                    break;
                default:
                    // Handle any default case or unrecognized card types
                    break;
            }
        }

        private void AdvanceToBoardwalk(Player player, Game game)
        {
            player.Position = 39;
            var square = game.Board.GetSquareAtPosition(player.Position);
            square.LandOn(player, game);
        }

        private void AdvanceToGo(Player player, Game game)
        {
            player.Position = 0;
            game.Transactions.PlayerGetSalary(player);
            var square = game.Board.GetSquareAtPosition(player.Position);
            square.LandOn(player, game);
        }

        private void AdvanceToIllinoisAvenue(Player player, Game game)
        {
            if (player.Position > 24) game.Transactions.PlayerGetSalary(player);
            player.Position = 24;
            var square = game.Board.GetSquareAtPosition(player.Position);
            square.LandOn(player, game);
        }

        private void AdvanceToStCharlesPlace(Player player, Game game)
        {
            if (player.Position > 11) game.Transactions.PlayerGetSalary(player);
            player.Position = 11;
            var square = game.Board.GetSquareAtPosition(player.Position);
            square.LandOn(player, game);
        }

        private void AdvanceToNearestRailroad(Player player, Game game)
        {
            if (player.Position > 35)
            {
                player.Position = 5;
                game.Transactions.PlayerGetSalary(player);
            }
            else if (player.Position > 25) player.Position = 35;
            else if (player.Position > 15) player.Position = 25;
            else if (player.Position > 5) player.Position = 15;
            else player.Position = 5;

            var square = game.Board.GetSquareAtPosition(player.Position) as RailroadSquare;
            square.LandOn(player, game, true);
        }

        private void AdvanceToNearestUtility(Player player, Game game)
        {
            if (player.Position > 28)
            {
                player.Position = 12;
                game.Transactions.PlayerGetSalary(player);
            }
            else if (player.Position > 12) player.Position = 28;
            else player.Position = 12;

            game.Handler.RollDice(player);

            var square = game.Board.GetSquareAtPosition(player.Position) as UtilitySquare;
            square.LandOn(player, game, true);
        }

        private void BankPaysDividend(Player player, Game game)
        {
            game.Transactions.GetMoneyFromBank(player, 50);
        }

        private void GetOutOfJailFree(Player player, Game game)
        {
            player.NumberOfGetOutOFJailCards++;
        }

        private void GoBack3Spaces(Player player, Game game)
        {
            player.Position -= 3;
            var square = game.Board.GetSquareAtPosition(player.Position);
            square.LandOn(player, game);
        }

        private void GoToJail(Player player, Game game)
        {
            game.TheJail.PlayerGoToJail(player);
        }

        private void MakeGeneralRepairs(Player player, Game game)
        {
            int houses = 0;
            int hotels = 0;
            var propertiesWithHousesList = game.Board.GetAllPropertySquaresPlayerCanSellHousesIn(player);

            foreach (var property in propertiesWithHousesList)
            {
                if (property.Houses == 5) hotels++;
                else if (property.Houses > 0) houses += property.Houses;
            }

            int sumToPay = (houses * 25) + (hotels * 100);

            if (game.Handler.IfPlayerCantPayInvokeOrBankrupt(player, sumToPay)) return;
            game.Transactions.PayMoneyToBank(player, sumToPay);
        }

        private void SpeedingFine(Player player, Game game)
        {
            if (game.Handler.IfPlayerCantPayInvokeOrBankrupt(player, 15)) return;
            game.Transactions.PayFines(player, 15);
        }

        private void TakeTripToReadingRailroad(Player player, Game game)
        {
            if (player.Position > 5) game.Transactions.PlayerGetSalary(player);
            player.Position = 5;

            var square = game.Board.GetSquareAtPosition(player.Position) as RailroadSquare;
            square.LandOn(player, game);
        }

        private void ElectedChairmanOfTheBoard(Player player, Game game)
        {
            int totalSumToPay = (game.Players.Count - 1) * 50;
            if (game.Handler.IfPlayerCantPayInvokeOrBankrupt(player, totalSumToPay)) return;

            foreach (var gamePlayer in game.Players)
                if (player != gamePlayer) game.Transactions.PayPlayerFromPlayer(player, 50, gamePlayer);
        }

        private void BuildingLoanMatures(Player player, Game game)
        {
            game.Transactions.GetMoneyFromBank(player, 150);
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
