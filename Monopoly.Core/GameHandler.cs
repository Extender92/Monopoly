using Monopoly.Core.Models.Board;
using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Monopoly.Core
{
    internal class GameHandler
    {
        internal Game CurrentGame { get; set; }

        internal GameHandler(Game currentGame)
        {
            CurrentGame = currentGame;
        }

        internal void RoleDiceAndMovePlayer(Player player)
        {
            RollDice(player);
            int diceSum = CalculateDiceSum();
            player.Position += diceSum;
            CheckIfPlayerGoPastGo(player);
        }

        internal void CheckIfPlayerGoPastGo(Player player)
        {
            if (player.Position >= CurrentGame.Board.Squares.Count)
            {
                player.Position -= (CurrentGame.Board.Squares.Count);
                CurrentGame.Transactions.PlayerGetSalary(player);
            }
        }

        internal void RollDice(Player player)
        {
            string diceRoll = $"{player.Name} rolled:";
            foreach (IDie die in CurrentGame.Dice)
            {
                die.Roll();
                diceRoll += $" {die.GetDieResult()}";
            }
            diceRoll += $" Total: {CalculateDiceSum()}";
            CurrentGame.Logs.CreateLog(diceRoll);
        }

        internal bool IsDiceDouble()
        {
            if (CurrentGame.Dice.Count < 2)
            {
                // At least two dice are required for a double
                return false;
            }

            // Get the result of the first die
            int firstDieResult = CurrentGame.Dice[0].GetDieResult();

            // Check if all dice have the same result as the first die
            return CurrentGame.Dice.All(die => die.GetDieResult() == firstDieResult);
        }

        internal int CalculateDiceSum()
        {
            int diceSum = 0;
            foreach (IDie die in CurrentGame.Dice)
            {
                diceSum += die.GetDieResult();
            }
            return diceSum;
        }

        internal void Winning(Player player)
        {
            // Win method todo //
        }

        internal int GetMoneyFromBankruptPlayerAndBankruptPlayer(Player player)
        {
            int remainingPlayerMoney = player.Money;
            player.Money = 0;
            remainingPlayerMoney += CalculatePlayerAssets(player);
            HandlePlayerBankruptcy(player);
            return remainingPlayerMoney;
        }

        internal void HandlePlayerBankruptcy(Player player)
        {
            ClearOwnershipForPlayer(player);
            player.IsBankrupt = true;
        }

        internal void ClearOwnershipForPlayer(Player player)
        {
            foreach (var square in CurrentGame.Board.Squares)
                if (square.Owner == player)
                {
                    square.Owner = null;
                    if (square is PropertySquare property)
                    {
                        property.Houses = 0;
                    }
                }
        }

        internal bool IsPlayerBankrupt(Player player, int sum)
        {
            return !CanAffordWithAssets(player, sum);
        }

        internal bool CanAffordWithAssets(Player player, int sum)
        {
            return CalculatePlayerAssets(player) >= sum;
        }

        internal int CalculatePlayerAssets(Player player)
        {
            int totalAssets = player.Money;

            foreach (var square in CurrentGame.Board.Squares)
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

        internal int CalculateMortgageValue(Square square)
        {
            return square.MortgageValue;
        }

        internal int CalculateHouseAndHotelValue(PropertySquare property)
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
