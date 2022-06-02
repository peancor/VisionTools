using Cocona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RsCapture.Commands
{
    public class DeepMeasureCommandParameters: ICommandParameterSet
    {
        [Option("sn", Description = "Serial number of the device")]
        [HasDefaultValue]
        public string? SN { get; set; } = null;


        [Option("duration", Description = "Duration of measure (sg)")]
        [HasDefaultValue]
        public double Duration { get; set; } = 3600;

        [Option("width")]
        [HasDefaultValue]
        public int Width { get; set; } = 640;
        [Option("height")]
        [HasDefaultValue]
        public int Height { get; set; } = 480;


        [Option("data", Description = "folder where data will be saved")]
        [HasDefaultValue]
        public string? DataFolder { get; set; } = null;
    }
}
