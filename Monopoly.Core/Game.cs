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

        internal static void NextPlayerTakeTurn(Player player)
        {
            int diceSum = RollDiceAndReturnSum();

            player.Position += diceSum;
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
    }
}
