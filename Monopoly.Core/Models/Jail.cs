using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models
{
    internal class Jail : Square
    {
        public Jail()
        {
            Position = 10;
            Info = "Visiting in Jail";
        }
    }
}
