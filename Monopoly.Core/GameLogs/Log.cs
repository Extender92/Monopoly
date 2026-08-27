using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Logs
{
    public class Log
    {
        public int Id { get; internal init; }
        public string Info { get; internal init; } = string.Empty;
    }
}
