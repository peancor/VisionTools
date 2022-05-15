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
    public static class RunCommand
    {
        public static async Task RunCapture(RunParameters p, CoconaAppContext appcontext)
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
            int framesCaptured = 0;
            int width = p.Width;
            int height = p.Height;
            int ts = p.Ts;
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
            AnsiConsole.MarkupLine($"Ts (sg): [yellow]{p.Ts}[/]");

            await AnsiConsole.Status().StartAsync("Comenzando proceso de captura...", async ctx =>
            {
                DateTime startTime = DateTime.Now;
                bool timeElapsed = false;

                var depthBuffer = new ushort[width * height];
                var depthDistanceBuffer = new float[width * height];

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
                    depthSensor.Options[Option.VisualPreset].Value = (float)Rs400VisualPreset.HighAccuracy;
                    var blocks = depthSensor.ProcessingBlocks.ToList();


                    //Filtros
                    //Colorizer colorizer = new Colorizer();
                    //colorizer.Options[Option.ColorScheme].Value = (int)ColorScheme.Bio;
                    //colorizer.Options[Option.HistogramEqualizationEnabled].Value = 1f;
                    //colorizer.Options[Option.MinDistance].Value = .15f;
                    //colorizer.Options[Option.MaxDistance].Value = .2f;

                    TemporalFilter tf = new TemporalFilter();


                    HoleFillingFilter hf = new HoleFillingFilter();
                    //hf.Options[Option.HolesFill].Value = 5;


                    //Align alignFilter = new Align(AlignToColor ? Stream.Color : Stream.Depth);

                    DecimationFilter decimation = new DecimationFilter();
                    decimation.Options[Option.FilterMagnitude].Value = 2;

                    DisparityTransform depthToDisparity = new DisparityTransform();
                    DisparityTransform disparityToDepth = new DisparityTransform(false);

                    SpatialFilter spatial = new();
                    spatial.Options[Option.HolesFill].Value = 5;
                    TemporalFilter temporal = new TemporalFilter();
                    temporal.Options[Option.FilterSmoothAlpha].Value = 0f;


                    do
                    {
                        //cogemos frames
                        using var frames = pipeline.WaitForFrames();
                        framesReceived++;
                        var depthFrame = frames.DepthFrame.DisposeWith(frames);
                        //filtramos
                        var filteredFrame = temporal.Process(depthFrame).DisposeWith(frames);
                        //Comprobamos si debemos capturar
                        var elapsedCapture = DateTime.Now - lastCaptureTime;
                        if (elapsedCapture.TotalSeconds > ts)
                        {
                            lastCaptureTime = DateTime.Now;
                            //debemos capturar
                            RsUtils.FillDepth(depthFrame, depthBuffer, width, height);
                            for (int i = 0; i < depthBuffer.Length; i++)
                            {
                                depthDistanceBuffer[i] = depthBuffer[i] * 1000.0f * depthSensor.DepthScale;
                            }
                            var arr = np.array(depthDistanceBuffer);
                            var fn = $"{framesCaptured.ToString("000000")}.npy";
                            np.save(Path.Combine(dataPath, fn), arr);
                            framesCaptured++;
                        }

                        var elapsed = DateTime.Now - startTime;
                        if (elapsed > TimeSpan.FromHours(duration))
                        {
                            timeElapsed = true;
                        }
                        ctx.Status($"received: [yellow]{framesReceived}[/], captured: [yellow]{framesCaptured}[/]");

                        if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Q)
                        {
                            ConsoleUtils.WriteInfoMessage("Cancelling...");
                            return;
                        }
                    }
                    while (appcontext.CancellationToken.IsCancellationRequested == false && !timeElapsed);
                }
            });
        }
    }
}
