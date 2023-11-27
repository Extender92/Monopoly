using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models
{
    internal class Plot : Square
    {
        internal int Price { get; set; }
        internal int House { get; set; }
    }
}
