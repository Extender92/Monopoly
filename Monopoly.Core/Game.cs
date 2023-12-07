using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    internal class Game
    {
        internal Board Board { get; set; }
        internal List<Player> Players  { get; set;}
        internal List<IDie> Dice { get; set; }
        private EventHandler Events { get; set; }
        internal GameRules Rules { get; set; }

        public Game(List<IDie> dice, List<Player> players, GameRules rules)
        {
            Board = new Board(rules);
            Players = players;
            Dice = dice;
            //Events = new EventHandler();
            Rules = rules;
        }
        internal void PlayerTurn(Player player)
        {
            int diceSum = RollDiceAndReturnSum();

            player.Position += diceSum;
            if (player.Position > 39)
            {
                player.Position -= 39;
            }
            //Events.HandleEvent(player, diceSum);

            Winning(Players.FirstOrDefault());
        }

        private int RollDiceAndReturnSum()
        {
            int diceSum = 0;
            foreach (Die die in Dice)
            {
                die.Roll();
                diceSum += die.GetDieResult();
            }
            return diceSum;
        }

        private void Winning(Player player)
        {
            if (player is null) { return; }
            // Win method todo //
        }
    }
}
