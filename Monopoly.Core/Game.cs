using Monopoly.Core.Events;
using Monopoly.Core.Logs;
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
        internal static LogHandler Logs { get; set; } = new LogHandler();
        internal static GameBoard Board { get; set; }
        internal static List<Player> Players  { get; set;}
        internal static List<IDie> Dice { get; set; }
        internal static GameRules Rules { get; set; }
        internal static Transaction Transactions { get; set; }
        internal static Jail TheJail { get; set; }
        internal static FortuneCardHandler FortuneCard { get; set; }
        internal static int Fines { get; set; }

        internal static void RoleDiceAndMovePlayer(Player player)
        {
            RollDice(player);
            int diceSum = CalculateDiceSum();
            player.Position += diceSum;
            CheckIfPlayerGoPastGo(player);
        }

        internal static void CheckIfPlayerGoPastGo(Player player)
        {
            if (player.Position >= Board.Squares.Count)
            {
                player.Position -= (Board.Squares.Count);
                Transactions.PlayerGetSalary(player);
            }
        }

        internal static void RollDice(Player player)
        {
            string diceRoll = player.Name + " rolled:";
            foreach (Die die in Game.Dice)
            {
                die.Roll();
                diceRoll += $" {die.GetDieResult()}";
            }
            diceRoll += " Total: " + CalculateDiceSum();
            Game.Logs.CreateLog(diceRoll);
        }

        internal static bool IsDiceDouble()
        {
            if (Dice.Count < 2)
            {
                // At least two dice are required for a double
                return false;
            }

            // Get the result of the first die
            int firstDieResult = Dice[0].GetDieResult();

            // Check if all dice have the same result as the first die
            return Dice.All(die => die.GetDieResult() == firstDieResult);
        }

        internal static int CalculateDiceSum()
        {
            int diceSum = 0;
            foreach (Die die in Dice)
            {
                diceSum += die.GetDieResult();
            }
            return diceSum;
        }

        internal static void Winning(Player player)
        {
            // Win method todo //
        }

        internal static int GetMoneyFromBankruptPlayerAndBankruptPlayer(Player player)
        {
            int remainingPlayerMoney = player.Money;
            player.Money = 0;
            remainingPlayerMoney += CalculatePlayerAssets(player);
            HandlePlayerBankruptcy(player);
            return remainingPlayerMoney;
        }

        internal static void HandlePlayerBankruptcy(Player player)
        {
            ClearOwnershipForPlayer(player);
            player.IsBankrupt = true;
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
            return CalculatePlayerAssets(player) >= sum;
        }

        private static int CalculatePlayerAssets(Player player)
        {
            int totalAssets = player.Money;

            foreach (var square in Board.Squares)
            {
                if (square.Owner == player)
                {
                    if (!square.IsMortgage)
                    {
                        totalAssets += CalculateMortgageValue(square);
                    }

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
            const int hotelIndex = 5;

            int value = 0;

            for (int i = 1; i <= property.Houses; i++)
            {
                if (i == hotelIndex)
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
