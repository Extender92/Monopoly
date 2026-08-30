using Monopoly.Core.Interface;
using Monopoly.Core.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace Monopoly.Core.Logs
{
    internal sealed class LogHandler : ILogHandler
    {
        private readonly List<Log> _logs = new();
        private readonly ReadOnlyCollection<Log> _logsView;
        public IReadOnlyList<Log> LogList => _logsView;
        internal Game? OwnerGame { get; set; }

        public LogHandler()
        {
            _logsView = _logs.AsReadOnly();
        }

        public void CreateLog(string text)
        {
            Log log = new Log
            {
                Id = _logs.Count,
                Info = text
            };
            _logs.Add(log);
            if (OwnerGame is Game game)
                game.PublishNotification(new LogAddedNotification(log, game.Profile.PresentationToken));
        }
    }
}
