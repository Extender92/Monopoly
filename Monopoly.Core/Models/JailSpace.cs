using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models
{
    internal class JailSpace : Square
    {
        public JailSpace()
        {
            Position = 30;
            Info = "Go To Jail"; 
        }
    }
}
