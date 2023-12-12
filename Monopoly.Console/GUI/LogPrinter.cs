using Monopoly.Core.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class LogPrinter
    {
        internal IConsoleWrapper Console { get; set; }
        public int LogPosX { get; set; }
        public int LogPosY { get; set; }

        public LogPrinter(IConsoleWrapper console)
        {
            Console = console;
            SetLogPosition();
        }

        internal void SetLogPosition()
        {
            LogPosX = ConsolePositions.LogPosX;
            LogPosY = ConsolePositions.LogPosY;
        }

        internal void PrintLogs(string header, List<string> logs, ConsoleColor borderColor = ConsoleColor.White, ConsoleColor textColor = ConsoleColor.White)
        {
            int width = logs.Max(x => x.Length) + 2;
            if (width < header.Length + 3) width = header.Length + 4;

            // Border and Header Text
            Console.SetTextColor(borderColor);
            Console.SetPosition(LogPosX, LogPosY);
            Console.Write("┌─ " + header + " " + new String('─', width - header.Length - 3) + '┐');

            // Body
            for (int i = 0; i < logs.Count; i++)
            {
                Console.SetPosition(LogPosX, LogPosY + 1 + i);
                Console.Write("│");

                Console.SetTextColor(textColor);
                Console.Write(" " + logs[i] + " ");

                Console.SetTextColor(borderColor);
                Console.SetPosition((LogPosX + width + 1), LogPosY + 1 + i);
                Console.Write("│");
            }

            // Footer
            Console.SetPosition(LogPosX, LogPosY + (logs.Count + 1));
            Console.Write('└' + new String('─', width) + '┘');
            Console.ResetColor();
        }

        internal void PrintNewestLogs(int maxAmountOfLogs, List<Log> LogList)
        {
            // Ensure that maxAmountOfLogs is within the range of logList.Count
            int startIndex = Math.Max(0, LogList.Count - maxAmountOfLogs);

            List<string> logStrings = LogList.Count > 0
                ? Helpers.StringHelper.CreateStringList(LogList
                    .Skip(startIndex)
                    .Select(l => l.Info)
                    .ToArray())
                : new List<string> { "Logs is empty" };

            logStrings.Reverse();  // Reverse the order of logStrings

            PrintLogs("Logs", logStrings, ConsoleColor.Green, ConsoleColor.White);
        }
    }
}
