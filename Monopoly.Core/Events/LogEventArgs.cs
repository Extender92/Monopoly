using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Events
{
    internal class LogEventArgs
    {
        public string LogInfo { get; }

        public LogEventArgs(string logInfo)
        {
            LogInfo = logInfo;
        }
    }
}
