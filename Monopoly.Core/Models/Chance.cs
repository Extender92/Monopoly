using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models
{
    internal class Chance(string info)
    {
        public string Info { get; set; } = info;
    }
}
