using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Events
{
    internal class PlayerEventArgs : EventArgs
    {
        public Player Player { get; }
        public int TargetSum { get; }

        public PlayerEventArgs(Player player, int targetSum = 0)
        {
            Player = player;
            TargetSum = targetSum;
        }
    }
}
