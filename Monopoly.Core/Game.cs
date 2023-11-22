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
        private Dice _dice { get; set; }
        private List<Player> _players  { get; set;}
        private List<Street> _streets { get; set; }

        public Game(Dice dice, int numberOfPlayers)
        {
            _dice = dice;
            _streets = new List<Street>();
            _players = Player.GetNewPlayers(numberOfPlayers);
        }
        private void StartGame(int numberOfPlayers)
        {          
            while (true)
            {
                foreach (var player in _players)
                {

                }
            }
        }

    }
}
