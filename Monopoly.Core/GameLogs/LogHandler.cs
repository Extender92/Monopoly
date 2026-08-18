using Monopoly.Core.Events;
using Monopoly.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Logs
{
    public class LogHandler : ILogHandler
    {
        public List<Log> LogList { get; } = new List<Log>();
        internal IGame? OwnerGame { get; set; }

        public void CreateLog(string text)
        {
            Log log = new Log
            {
                Id = LogList.Count,
                Info = text
            };
            LogList.Add(log);
            GameEvents.InvokeLogAdded((object?)OwnerGame ?? this);
        }
    }
}
