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
        public static async Task PrintDeviceInfo(string sn)
        {
            var config = new Config();
            config.EnableDevice(sn);
            using var pipeline = new Pipeline();
            pipeline.Start(config);

            await Task.Delay(1);
        }

        private static void WriteLogMessage(string message)
        {
            AnsiConsole.MarkupLine($"[grey]LOG:[/] {message}[grey]...[/]");
        }

        public static async Task RunCapture(string sn, int ts, double duration, int width, int height, CancellationToken ct)
        {
            DateTime lastCaptureTime = DateTime.MinValue;
            int framesReceived = 0;
            int framesCaptured = 0;

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
                        config.EnableStream(Stream.Depth, width, height, Format.Z16);
                        //config.EnableStream(Stream.Color, ColorWidth, ColorHeight, Format.Rgb8);
                        //config.EnableStream(Stream.Accel);

                        //config.EnableStream(Stream.Confidence);

                        var profile = pipeline.Start(config);
                        var depthProfile = profile.GetStream(Stream.Depth).Cast<VideoStreamProfile>();
                        //var colorProfile = profile.GetStream(Stream.Color).Cast<VideoStreamProfile>();
                        //Intrinsics = AlignToColor ? colorProfile.GetIntrinsics() : depthProfile.GetIntrinsics();

                        var depthSensor = pipeline.ActiveProfile.Device.Sensors.Where(s => s.Is(Extension.DepthSensor)).First();
                        depthSensor.Options[Option.Accuracy].Value = 3;
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
                                var fn = $"{DateTime.Now.ToString("yyyyMMdd-HHmmss")}.npy";
                                np.save(fn, arr);
                                framesCaptured++;
                            }

                            var elapsed = DateTime.Now - startTime;
                            if (elapsed > TimeSpan.FromHours(duration))
                            {
                                timeElapsed = true;
                            }
                            ctx.Status($"frames recibidos: [red]{framesReceived}[/], capturados: [yellow]{framesCaptured}[/]");
                        }
                        while (!ct.IsCancellationRequested && !timeElapsed);
                    }
                });
        }
    }
}
