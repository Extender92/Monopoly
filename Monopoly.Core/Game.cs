using Monopoly.Core.Events;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    internal static class Game
    {
        internal static GameBoard Board { get; set; }
        internal static List<Player> Players  { get; set;}
        internal static List<IDie> Dice { get; set; }
        internal static GameRules Rules { get; set; }
        internal static Transaction Transaction { get; set; }
        internal static Jail Jail { get; set; }
        internal static FortuneCardHandler FortuneCard { get; set; }
        internal static int Fines { get; set; }

        internal static void NextPlayerTakeTurn(Player player)
        {
            int diceSum = RollDiceAndReturnSum();

            player.Position += diceSum;
            CheckIfPlayerGoPastGo(player);
        }

        private static void CheckIfPlayerGoPastGo(Player player)
        {
            if (player.Position >= Board.Squares.Count)
            {
                player.Position -= (Board.Squares.Count);
                Transaction.PlayerGetSalary(player);
            }
        }

        internal static int RollDiceAndReturnSum()
        {
            int diceSum = 0;
            foreach (Die die in Dice)
            {
                die.Roll();
                diceSum += die.GetDieResult();
            }
            return diceSum;
        }

        internal static void Winning(Player player)
        {
            if (player is null) { return; }
            // Win method todo //
        }

        internal static int HandlePlayerBankruptcyAndGetMoney(Player player)
        {
            int remainingPlayerMoney = player.Money;
            player.Money = 0;
            remainingPlayerMoney += CalculatePlayerAssets(player);
            ClearOwnershipForPlayer(player);
            Players.Remove(player);
            return remainingPlayerMoney;
        }

        public static void ClearOwnershipForPlayer(Player player)
        {
            foreach (var square in Board.Squares)
                if (square.Owner == player)
                {
                    square.Owner = null;
                    if (square is PropertySquare property){
                        property.Houses = 0;
                    }
                }
        }

        internal static bool IsPlayerBankrupt(Player player, int sum)
        {
            return !CanAffordWithAssets(player, sum);
        }

        internal static bool CanAffordWithAssets(Player player, int sum)
        {
            return CalculatePlayerAssets(player) < sum;
        }

        private static int CalculatePlayerAssets(Player player)
        {
            int totalAssets = 0;

            foreach (var square in Board.Squares)
            {
                if (square.Owner == player)
                {
                    totalAssets += CalculateMortgageValue(square);

                    if (square is PropertySquare property)
                    {
                        totalAssets += CalculateHouseAndHotelValue(property);
                    }
                }
            }

            return totalAssets;
        }

        private static int CalculateMortgageValue(Square square)
        {
            return square.MortgageValue;
        }

        private static int CalculateHouseAndHotelValue(PropertySquare property)
        {
            const int hotelValue = 5;

            int value = 0;

            for (int i = 1; i <= property.Houses; i++)
            {
                if (i == hotelValue)
                {
                    value += property.BuildHotelCost / 2;
                }
                else
                {
                    value += property.BuildHouseCost / 2;
                }
            }

            return value;
        }
    }
}
