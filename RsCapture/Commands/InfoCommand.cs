using Intel.RealSense;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RsCapture.Commands
{
    internal static class InfoCommand
    {
        public static async Task RunAsync(string? sn)
        {
            using var ctx = new Intel.RealSense.Context();
            var devices = ctx.QueryDevices();

            if (devices.Count == 0)
            {
                ConsoleUtils.WriteErrorMessage("No realsense devices found");
            }

            //Si no hemos especificado sn
            if (sn == null && devices.Count > 0)
            {
                //Si tenemos un dispositivo conectado cogemos su sn
                sn = devices[0].Info[Intel.RealSense.CameraInfo.SerialNumber];
            }
            //Ahora comprobamos que ese sn existe porque sin no error
            var device = devices.Where(d => d.Info[CameraInfo.SerialNumber] == sn).FirstOrDefault();
            if (device is null)
            {
                //Error
                ConsoleUtils.WriteErrorMessage($"Error device with serial number [yellow]{sn}[/] not found");
                return;
            }

            ConsoleUtils.WriteInfoMessage($"using device [yellow]{device.Info[CameraInfo.Name]}[/] with sn:[yellow]{device.Info[CameraInfo.SerialNumber]}[/]");

            var infolist = device.Info.ToArray();
            foreach (var (k, v) in infolist)
            {
                AnsiConsole.MarkupLine($"{k}: [yellow]{v}[/]");
            }

            //Sensores
            // Create the tree
            var root = new Tree("Sensors");
            foreach (var s in device.Sensors)
            {
                var node = root.AddNode($"{s.Info[CameraInfo.Name]}");
                foreach (var (k, v) in s.Info)
                {
                    node.AddNode($"{k}: [yellow]{v}[/]");
                    //AnsiConsole.MarkupLine($"{k}: [yellow]{v}[/]");
                }
                var snode = node.AddNode("Processing blocks");
                foreach (var pb in s.ProcessingBlocks)
                {
                    foreach (var (k, v) in pb.Info)
                    {
                        snode.AddNode($"{k}: [yellow]{v}[/]");
                        //AnsiConsole.MarkupLine($"{k}: [yellow]{v}[/]");
                    }
                }
            }
            AnsiConsole.Write(root);
        }
    }
}
