using Monopoly.Core.Logs;
using Monopoly.Core.Models.FortuneCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Events
{
    internal class DrawChanceCardArgs
    {
        public IChanceCard ChanceCard { get; set; }

        public DrawChanceCardArgs(IChanceCard chanceCard)
        {
            ChanceCard = chanceCard;
        }
    }
}
