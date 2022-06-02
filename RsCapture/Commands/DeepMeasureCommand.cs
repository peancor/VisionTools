using Cocona;
using Intel.RealSense;
using Numpy;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RsCapture.Commands
{
    public class DeepMeasureCommand
    {
        public static async Task RunCapture(DeepMeasureCommandParameters p, CoconaAppContext appcontext)
        {
            using var ctx = new Intel.RealSense.Context();
            var devices = ctx.QueryDevices();

            if (devices.Count == 0)
            {
                AnsiConsole.MarkupLine("No realsense devices found");
            }

            string sn = p.SN;
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

            DateTime lastCaptureTime = DateTime.MinValue;
            int framesReceived = 0;
            int width = p.Width;
            int height = p.Height;

            double duration = p.Duration;
            //comprobamos el data path
            var rootPath = "";
            if (string.IsNullOrEmpty(p.DataFolder) == false && Directory.Exists(p.DataFolder))
            {
                rootPath = p.DataFolder;
            }
            //Aquí creamos el nuevo data folder
            var dataPath = Path.Combine(rootPath, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            if (Directory.Exists(dataPath) == false)
            {
                Directory.CreateDirectory(dataPath);
            }

            //imprimimos parametros
            AnsiConsole.MarkupLine($"Device: [yellow]{device.Info[CameraInfo.Name]}[/]");
            AnsiConsole.MarkupLine($"Serial number: [yellow]{sn}[/]");
            AnsiConsole.MarkupLine($"width: [yellow]{p.Width}[/]");
            AnsiConsole.MarkupLine($"height: [yellow]{p.Height}[/]");
            AnsiConsole.MarkupLine($"Duration (h): [yellow]{p.Duration}[/]");

            await AnsiConsole.Status().StartAsync("Comenzando medición...", async ctx =>
            {
                DateTime startTime = DateTime.Now;
                bool timeElapsed = false;

                var depthBuffer = new ushort[width * height];
                var depthDistanceBuffer = new decimal[width * height];
                var caBuffer = new decimal[width * height];


                using (var pipeline = new Pipeline())
                {
                    var config = new Config();
                    config.EnableDevice(sn);
                    config.EnableStream(Intel.RealSense.Stream.Depth, width, height, Format.Z16);
                    //config.EnableStream(Stream.Color, ColorWidth, ColorHeight, Format.Rgb8);
                    //config.EnableStream(Stream.Accel);

                    //config.EnableStream(Stream.Confidence);

                    var profile = pipeline.Start(config);
                    var depthProfile = profile.GetStream(Intel.RealSense.Stream.Depth).Cast<VideoStreamProfile>();
                    //var colorProfile = profile.GetStream(Stream.Color).Cast<VideoStreamProfile>();
                    //Intrinsics = AlignToColor ? colorProfile.GetIntrinsics() : depthProfile.GetIntrinsics();

                    var depthSensor = pipeline.ActiveProfile.Device.Sensors.Where(s => s.Is(Extension.DepthSensor)).First();
                    if (depthSensor.Options.Supports(Option.Accuracy))
                    {
                        depthSensor.Options[Option.Accuracy].Value = 3;
                    }
                    //
                    //DepthScale = depthSensor.DepthScale;
                    //depthSensor.Options[Option.VisualPreset].Value = (float)Rs400VisualPreset.HighAccuracy;
                    depthSensor.Options[Option.VisualPreset].Value = 5;
                    var blocks = depthSensor.ProcessingBlocks.ToList();

                    DepthFrame? depthFrame = null;
                    while (!timeElapsed)
                    {
                        //cogemos frames
                        using var frames = pipeline.WaitForFrames();
                        depthFrame = frames.DepthFrame.DisposeWith(frames);
                        //debemos capturar
                        RsUtils.FillDepth(depthFrame, depthBuffer, width, height);
                        for (int i = 0; i < depthBuffer.Length; i++)
                        {
                            depthDistanceBuffer[i] = (decimal)depthBuffer[i] * 1000.0M * (decimal)depthSensor.DepthScale;
                        }
                        //filtramos
                        //var filteredFrame = temporal.Process(depthFrame).DisposeWith(frames);
                        //promediamos
                        for (int i = 0; i < caBuffer.Length; i++)
                        {
                            caBuffer[i] = caBuffer[i] + ((depthDistanceBuffer[i] - caBuffer[i]) / (framesReceived + 1));
                        }


                        var elapsed = DateTime.Now - startTime;
                        if (elapsed > TimeSpan.FromSeconds(duration))
                        {
                            timeElapsed = true;
                        }
                        ctx.Status($"received: [yellow]{framesReceived}[/]");

                        if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Q)
                        {
                            ConsoleUtils.WriteInfoMessage("Cancelling...");
                            break;
                        }
                        framesReceived++;
                    }

                    var ddBuffer = Array.ConvertAll(caBuffer, x => (double)x);
                    var arr = np.array(ddBuffer);
                    var fn = $"dm from {startTime.ToString("yyMMddHHmmss")} to {DateTime.Now.ToString("yyMMddHHmmss")} ({framesReceived}).npy";
                    np.save(Path.Combine(dataPath, fn), arr);
                }
            });
        }
    }
}
