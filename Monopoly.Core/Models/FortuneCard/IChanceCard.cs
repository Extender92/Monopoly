using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.FortuneCard
{
    internal interface IChanceCard
    {
        string Info { get; }
        void ExecuteEffect(Player player);
    }
}
