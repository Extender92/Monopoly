using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models
{
    internal class Railroad : Square
    {
        public Railroad(int position)
        {
            Position = position;
            Info = "Railroad";
        }
    }
}
