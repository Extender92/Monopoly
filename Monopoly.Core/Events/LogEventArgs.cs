using Monopoly.Core.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Events
{
    internal class LogEventArgs
    {
        public List<Log> LogList { get; set; }

        public LogEventArgs(List<Log> logList)
        {
            LogList = logList;
        }
    }
}
