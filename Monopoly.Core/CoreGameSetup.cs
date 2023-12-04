using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monopoly.Core.Models;

namespace Monopoly.Core
{
    internal class CoreGameSetup
    {
        internal static Game Setup(GameRules rules)
        {
            List<Player> players = new List<Player>();
            List<IDie> dice = new List<IDie>();

            for (int i = 0; i < rules.NumberOfPlayers; i++)
            {
                players.Add(new Player("Player " + (i + 1), i));
            }

            for (int i = 0; i < rules.NumberOfDice; i++)
            {
                dice.Add(new Die(rules.DieSides));
            }



            return new Game(dice, players, rules);
        }
    }
}
