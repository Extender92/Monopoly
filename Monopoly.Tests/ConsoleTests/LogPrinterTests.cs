using Microsoft.VisualStudio.TestPlatform.Utilities;
using Monopoly.Console.GUI;
using Monopoly.Core.Logs;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Tests.ConsoleTests
{
    public class LogPrinterTests
    {
        [Fact]
        public void PrintLogs_PrintsLogsCorrectly()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();
            var logPrinter = new ConsoleLogPrinter(consoleMock.Object);

            var output = new StringWriter();
            System.Console.SetOut(output);
            consoleMock.Setup(c => c.Write(It.IsAny<string>())).Callback<string>(s => output.Write(s));

            var header = "Test Header";
            var logs = new List<string> { "Log Entry 1", "Log Entry 2", "Log Entry 3" };

            // Act
            logPrinter.PrintLogs(header, logs, ConsoleColor.Yellow, ConsoleColor.Magenta);


            // Assert
            consoleMock.Verify(c => c.SetPosition(It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
            consoleMock.Verify(c => c.SetTextColor(ConsoleColor.Yellow), Times.AtLeastOnce);
            consoleMock.Verify(c => c.SetTextColor(ConsoleColor.Magenta), Times.Exactly(logs.Count));
            consoleMock.Verify(c => c.ResetColor(), Times.Once);
            consoleMock.Verify(c => c.Write(It.IsAny<string>()), Times.AtLeastOnce);

            string expectedOutput =
                "┌─ Test Header ─┐" +
                "│ Log Entry 1 │" +
                "│ Log Entry 2 │" +
                "│ Log Entry 3 │" +
                "└───────────────┘";

            Assert.Equal(expectedOutput, output.ToString());

            // Clean up: Restore console output
            System.Console.SetOut(System.Console.Out);
        }

        [Fact]
        public void PrintNewestLogs_PrintsNewestLogsCorrectly()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();
            var logPrinter = new ConsoleLogPrinter(consoleMock.Object);

            var output = new StringWriter();
            System.Console.SetOut(output);
            consoleMock.Setup(c => c.Write(It.IsAny<string>())).Callback<string>(s => output.Write(s));

            var maxAmountOfLogs = 10;
            var logList = new List<Log>();
            logList.Add(new Log { Info = "Log Entry 1" });
            logList.Add(new Log { Info = "Log Entry 2" });
            logList.Add(new Log { Info = "Log Entry 3" });
            logList.Add(new Log { Info = "Log Entry 4" });
            logList.Add(new Log { Info = "Log Entry 5" });
            logList.Add(new Log { Info = "Log Entry 6" });
            logList.Add(new Log { Info = "Log Entry 7" });
            logList.Add(new Log { Info = "Log Entry 8" });
            logList.Add(new Log { Info = "Log Entry 9" });
            logList.Add(new Log { Info = "Log Entry 10" });
            logList.Add(new Log { Info = "Log Entry 11" });
            logList.Add(new Log { Info = "Log Entry 12" });

            // Act
            logPrinter.PrintNewestLogs(maxAmountOfLogs, logList);

            // Assert
            consoleMock.Verify(c => c.SetPosition(It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
            consoleMock.Verify(c => c.SetTextColor(It.IsAny<ConsoleColor>()), Times.AtLeastOnce);
            consoleMock.Verify(c => c.ResetColor(), Times.AtLeastOnce);
            consoleMock.Verify(c => c.Write(It.IsAny<string>()), Times.AtLeastOnce);

            string expectedOutput =
                "┌─ Logs ───────┐" +
                "│ Log Entry 12 │" +
                "│ Log Entry 11 │" +
                "│ Log Entry 10 │" +
                "│ Log Entry 9 │" +
                "│ Log Entry 8 │" +
                "│ Log Entry 7 │" +
                "│ Log Entry 6 │" +
                "│ Log Entry 5 │" +
                "│ Log Entry 4 │" +
                "│ Log Entry 3 │" +
                "└──────────────┘";

            Assert.Equal(expectedOutput, output.ToString());

            // Clean up: Restore console output
            System.Console.SetOut(System.Console.Out);
        }
    }
}
