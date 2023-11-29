using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    internal class Game
    {
        internal int[] Board { get; } = new int[40];
        internal List<Die> Dice { get; set; }
        internal List<Player> Players  { get; set;}
        internal EventHandler Events { get; set; }
        internal GameRules Rules { get; set; }

        public Game(List<Die> dice, List<Player> players, GameRules rules)
        {
            Rules = rules;
            Dice = dice;
            Events = new EventHandler();
            Players = players;
        }

        internal void PlayerTurn(Player player)
        {          
            int diceSum = 0;
            foreach (Die die in Dice)
            {
                die.Roll();
                diceSum += die.GetDieResult();
            }
            player.Position += diceSum;
            if (player.Position > 39)
            {
                player.Position -= 39;
            }
            Events.HandleEvent(player, diceSum);
            // Trade
            // Buy houses
        }

        private void Winning(Player player)
        {
            if (player is null) { return; }
            // Win method todo //
        }
    }
}
