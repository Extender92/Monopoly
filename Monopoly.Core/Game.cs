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
        private List<Plot> Streets { get; set; }
        private GameRules Rules { get; set; }

        public Game(List<Die> dice, List<Player> players, GameRules rules)
        {
            Rules = rules;
            Dice = dice;
            Streets = new List<Plot>();
            Players = players;
        }
        private void StartGame()
        {          
            while (true)
            {
                foreach (var player in Players)
                {

                }
            }
        }

    }
}
