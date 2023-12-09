using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    internal class Jail
    {
        internal void PlayerGoToJail(Player player)
        {
            player.Position = 10;
            player.InJail = true;
        }
    }
}
