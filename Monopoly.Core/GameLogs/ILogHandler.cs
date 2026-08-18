
namespace Monopoly.Core.Logs
{
    public interface ILogHandler
    {
        List<Log> LogList { get; }
        void CreateLog(string s);
    }
}
