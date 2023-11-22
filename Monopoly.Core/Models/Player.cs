using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models
{
    public class Player
    {
        public string Name { get; set; }
        public int Money { get; set; }
        public int Position { get; set; } = 0;

        public Player(string name)
        {
            Name = name;
            Money = 3000;
        }

        public static List<Player> GetNewPlayers(int numberOfPlayers)
        {
            List<Player> players = new List<Player>();
            for (int i = 0; i < numberOfPlayers; i++)
            {
                players.Add(new Player("Player " + (i + 1)));
            }
            return players;
        }
    }
}
