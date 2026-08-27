
namespace Monopoly.Core.Logs
{
    public interface IGameLog
    {
        IReadOnlyList<Log> LogList { get; }
    }

    internal interface ILogHandler : IGameLog
    {
        void CreateLog(string s);
    }
}
