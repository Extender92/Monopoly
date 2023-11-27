using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    internal class GameRules
    {
        static int numberOfPlayers { get; set; }
        static int numberOfDice {  get; set; }
        static int dieSides { get; set; }

        internal static Game GameSetup()
        {
            List<Player> players = new List<Player>();
            List<Die> dice = new List<Die>();

            for (int i = 0; i < numberOfPlayers; i++)
            {
                players.Add(new Player("Player " + (i + 1)));
            }

            for (int i = 0; i < numberOfDice; i++)
            {
                dice.Add(new Die(dieSides));
            }

            return new Game(dice, players);
        }
    }
}
