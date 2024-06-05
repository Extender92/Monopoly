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
                case USChanceCardType.GoBackThreeSpaces:
                    GoBackThreeSpaces(player, game);
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
            game.Handler.MovePlayerAndInvokeEvent(player, 39);
            var square = game.Board.GetSquareAtPosition(player.Position);
            square.LandOn(player, game);
        }

        private void AdvanceToGo(Player player, Game game)
        {
            int newPosition = game.Handler.GetPlayerGoPastGoNewPosition(0);
            game.Handler.MovePlayerAndInvokeEvent(player, newPosition);
            var square = game.Board.GetSquareAtPosition(player.Position);
            square.LandOn(player, game);
        }

        private void AdvanceToIllinoisAvenue(Player player, Game game)
        {
            int targetPosition = 24;
            int newPosition = player.Position > targetPosition
                ? game.Handler.GetPlayerGoPastGoNewPosition(targetPosition)
                : targetPosition;

            game.Handler.MovePlayerAndInvokeEvent(player, newPosition);

            var square = game.Board.GetSquareAtPosition(player.Position);
            square.LandOn(player, game);
        }

        private void AdvanceToStCharlesPlace(Player player, Game game)
        {
            int targetPosition = 11;
            int newPosition = player.Position > targetPosition
                ? game.Handler.GetPlayerGoPastGoNewPosition(targetPosition)
                : targetPosition;

            game.Handler.MovePlayerAndInvokeEvent(player, newPosition);

            var square = game.Board.GetSquareAtPosition(player.Position);
            square.LandOn(player, game);
        }

        private void AdvanceToNearestRailroad(Player player, Game game)
        {
            int newPosition = 0;
            if (player.Position > 35) newPosition = game.Handler.GetPlayerGoPastGoNewPosition(5);
            else if (player.Position > 25) newPosition = 35;
            else if (player.Position > 15) newPosition = 25;
            else if (player.Position > 5) newPosition = 15;
            else newPosition = 5;

            game.Handler.MovePlayerAndInvokeEvent(player, newPosition);

            var square = game.Board.GetSquareAtPosition(player.Position) as RailroadSquare;
            square.LandOn(player, game, true);
        }

        private void AdvanceToNearestUtility(Player player, Game game)
        {
            int newPosition = 12;
            if (player.Position > 28) newPosition = game.Handler.GetPlayerGoPastGoNewPosition(newPosition);
            else if (player.Position > 12) newPosition = 28;

            game.Handler.MovePlayerAndInvokeEvent(player, newPosition);

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
            game.Logs.CreateLog($"{player.Name} got a new Get Out Of Jail Card, and now has {player.NumberOfGetOutOFJailCards} Get Out Of Jail {(player.NumberOfGetOutOFJailCards == 1 ? "Card" : "Cards")}.");
        }

        private void GoBackThreeSpaces(Player player, Game game)
        {
            int newPosition = player.Position - 3;
            if (newPosition < 0)
                newPosition = newPosition += game.Board.Squares.Count;

            game.Handler.MovePlayerAndInvokeEvent(player, newPosition);

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
            int newPosition = 5;
            if (player.Position > newPosition) newPosition = game.Handler.GetPlayerGoPastGoNewPosition(newPosition);

            game.Handler.MovePlayerAndInvokeEvent(player, newPosition);

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
            GoBackThreeSpaces,
            GoToJail,
            MakeGeneralRepairs,
            SpeedingFine,
            TakeTripToReadingRailroad,
            ElectedChairmanOfTheBoard,
            BuildingLoanMatures
        }
    }
}
