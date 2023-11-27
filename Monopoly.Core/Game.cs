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
        private Die _die { get; set; }
        private List<Player> _players  { get; set;}
        private List<Plot> _streets { get; set; }

        public Game(Die dice, int numberOfPlayers)
        {
            _die = dice;
            _streets = new List<Plot>();
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
