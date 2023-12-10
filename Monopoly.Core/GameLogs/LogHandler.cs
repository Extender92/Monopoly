using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Logs
{
    internal class LogHandler
    {
        public List<Log> Logs { get; set; }

        public LogHandler()
        {
            List<Log> logs = new List<Log>();
        }

        internal void CreateLog(string text)
        {
            Log log = new Log();
            log.Id = Logs.Count;
            log.Info = text;
            Logs.Add(log);
        }
    }
}
