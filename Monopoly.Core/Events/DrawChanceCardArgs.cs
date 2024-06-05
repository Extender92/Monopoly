using Monopoly.Core.Logs;
using Monopoly.Core.Models.FortuneCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Events
{
    internal class DrawChanceCardArgs : EventArgs
    {
        public IChanceCard ChanceCard { get; }

        public DrawChanceCardArgs(IChanceCard chanceCard)
        {
            ChanceCard = chanceCard;
        }
    }
}
