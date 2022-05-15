using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stream = Intel.RealSense.Stream;
using Numpy;
using Spectre.Console;

namespace RsCapture
{
    internal static class RealSenseCapture
    {
        
        private static void WriteLogMessage(string message)
        {
            AnsiConsole.MarkupLine($"[grey]LOG:[/] {message}[grey]...[/]");
        }


    }
}
