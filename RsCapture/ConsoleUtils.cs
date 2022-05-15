using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RsCapture
{
    public static class ConsoleUtils
    {
        public static void WriteErrorMessage(string message)
        {
            AnsiConsole.MarkupLine($"[red on navyblue]ERROR:[/] {message}");
        }

        public static void WriteInfoMessage(string message)
        {
            AnsiConsole.MarkupLine($"[yellow on navyblue]INFO:[/] {message}");
        }
    }
}
