using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monopoly.Core.Models;

namespace Monopoly.Core
{
    internal class GameSetup
    {
        internal static Game Setup(int numberOfPlayers, int numberOfDice, int dieSides)
        {
            List<Player> players = new List<Player>();
            List<IDie> dice = new List<IDie>();
            GameRules rules = new GameRules();

            for (int i = 0; i < numberOfPlayers; i++)
            {
                players.Add(new Player("Player " + (i + 1), i));
            }

            for (int i = 0; i < numberOfDice; i++)
            {
                dice.Add(new Die(dieSides));
            }

            return new Game(dice, players, rules);
        }
    }
}
