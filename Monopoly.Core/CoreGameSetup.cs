using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monopoly.Core.Logs;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Randomness;

namespace Monopoly.Core
{
    public static class CoreGameSetup
    {
        public static Game Setup(
            GameRules gameRules,
            IPlayerDecisionProvider? decisions = null,
            IMatchRandomSource? randomSource = null)
        {
            List<Player> players = new List<Player>();

            for (int i = 0; i < gameRules.NumberOfPlayers; i++)
            {
                players.Add(new Player("Player " + (i + 1), i));
            }

            Game game = new Game(players, players.First(), gameRules, decisions, randomSource: randomSource);

            return game;
        }
    }
}
