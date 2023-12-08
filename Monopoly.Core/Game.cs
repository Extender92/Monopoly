using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
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

        internal static void PlayerTurn(Player player)
        {
            int diceSum = RollDiceAndReturnSum();

            player.Position += diceSum;
            if (player.Position > 39)
            {
                player.Position -= 39;
            }
            //Events.HandleEvent(player, diceSum);

            Winning(Players.FirstOrDefault());
        }

        private static int RollDiceAndReturnSum()
        {
            int diceSum = 0;
            foreach (Die die in Dice)
            {
                die.Roll();
                diceSum += die.GetDieResult();
            }
            return diceSum;
        }

        private static void Winning(Player player)
        {
            if (player is null) { return; }
            // Win method todo //
        }
    }
}
