
namespace Monopoly.Core.Logs
{
    internal interface ILogHandler
    {
        List<Log> LogList { get; }
        void CreateLog(string s);
    }
}