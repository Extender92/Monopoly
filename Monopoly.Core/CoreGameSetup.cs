using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;

namespace Monopoly.Core
{
    internal class CoreGameSetup
    {
        internal static Game Setup(GameRules gameRules)
        {
            List<Player> players = new List<Player>();
            List<IDie> dice = new List<IDie>();

            for (int i = 0; i < gameRules.NumberOfPlayers; i++)
            {
                players.Add(new Player("Player " + (i + 1), i));
            }

            for (int i = 0; i < gameRules.NumberOfDice; i++)
            {
                dice.Add(new Die(gameRules.DieSides));
            }

            Game game = new Game(players, dice, gameRules);

            return game;
        }
    }
}
