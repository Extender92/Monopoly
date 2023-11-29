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
        private int[] Board { get; } = new int[40];
        private List<Die> Dice { get; set; }
        private List<Player> Players  { get; set;}
        private EventHandler Events { get; set; }
        private GameRules Rules { get; set; }

        public Game(List<Die> dice, List<Player> players, GameRules rules)
        {
            Rules = rules;
            Dice = dice;
            Events = new EventHandler();
            Players = players;
        }
        private void StartGame()
        {          
            while (true)
            {
                if (Players.Count <= 1) break;
                foreach (var player in Players)
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
                }
            }
            Winning(Players.FirstOrDefault());
        }

        private void Winning(Player player)
        {
            if (player is null) { return; }
            // Win method todo //
        }
    }
}
