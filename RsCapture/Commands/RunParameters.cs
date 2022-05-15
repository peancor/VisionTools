using Cocona;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RsCapture.Commands
{
    public class RunParameters : ICommandParameterSet
    {
        [Option("sn", Description = "Serial number of the device")]
        [HasDefaultValue]
        public string? SN { get; set; } = null;

        [Option(Description = "Capture period (sg)")]
        [HasDefaultValue]
        public int Ts { get; set; } = 60;

        [Option("duration", Description = "Duration of capture (h)")]
        [HasDefaultValue]
        public double Duration { get; set; } = 1;

        [Option("width")]
        [HasDefaultValue]
        public int Width { get; set; } = 640;
        [Option("height")]
        [HasDefaultValue]
        public int Height { get; set; } = 480;

        /*
        [Option("roix")]
        [HasDefaultValue]
        public int? RoiX { get; set; } = null;
        [Option("roiy")]
        [HasDefaultValue]
        public int? RoiY { get; set; } = null;
        [Option("roiw")]
        [HasDefaultValue]
        public int? RoiW { get; set; } = null;
        [Option("roih")]
        [HasDefaultValue]
        public int? RoiH { get; set; } = null;
        */

        [Option("data", Description = "folder where data will be saved")]
        [HasDefaultValue]
        public string? DataFolder { get; set; } = null;
    }
}
