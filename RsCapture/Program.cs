using Cocona;
using RsCapture;
using Spectre.Console;

var app = CoconaApp.Create();

app.AddCommand("list", () =>
{
    using var ctx = new Intel.RealSense.Context();
    var devices = ctx.QueryDevices();
    if (devices.Count == 0)
    {
        AnsiConsole.Markup("No devices found");
    }
    int index = 0;
    foreach (var d in devices)
    {
        AnsiConsole.MarkupLine($"Found device {index++}: [yellow]{d.Info[Intel.RealSense.CameraInfo.Name]}[/] [red]({d.Info[Intel.RealSense.CameraInfo.SerialNumber]})[/]");
    }
});

app.AddCommand("info", async ([Option("sn")] string? sn) =>
{
    using var ctx = new Intel.RealSense.Context();
    var devices = ctx.QueryDevices();

    if (devices.Count == 0)
    {
        AnsiConsole.MarkupLine("No realsense devices found");
    }

    //Si no hemos especificado sn
    if (sn == null && devices.Count > 0)
    {
        //Si tenemos un dispositivo conectado cogemos su sn
        sn = devices[0].Info[Intel.RealSense.CameraInfo.SerialNumber];
    }
    //Ahora comprobamos que ese sn existe porque sin no error
    var sns = devices.Select(d => d.Info[Intel.RealSense.CameraInfo.SerialNumber]).ToArray();
    if (sns.Contains(sn) == false)
    {
        //Error
        AnsiConsole.MarkupLine($"Error device with serial number [red]{sn}[/] not found");
        return;
    }
    await RealSenseCapture.PrintDeviceInfo(sn);
});

app.AddCommand("run", async (RunParameters p) =>
{
    PrintRunParamsStatus(p, out var roi);
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
    var sns = devices.Select(d => d.Info[Intel.RealSense.CameraInfo.SerialNumber]).ToArray();
    if (sns.Contains(sn) == false)
    {
        //Error
        AnsiConsole.MarkupLine($"Error device with serial number [red]{sn}[/] not found");
        return;
    }
    //Si no arrancamos
    CancellationTokenSource cts = new CancellationTokenSource();
    await RealSenseCapture.RunCapture(sn, p, cts.Token);
});

AnsiConsole.Write(
    new FigletText("RsCapture")
        .LeftAligned()
        .Color(Color.Lime));

app.Run();

void PrintRunParamsStatus(RunParameters p, out (int, int, int, int)? roiout)
{
    if ((p.RoiX, p.RoiY, p.RoiW, p.RoiH) is (int, int, int, int) roi)
    {
        roiout = ((int, int, int, int)?)roi;
        AnsiConsole.MarkupLine($"roi: {roi}");
    }
    else
    {
        roiout = null;
        AnsiConsole.MarkupLine("roi not defined");
    }
}

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

    [Option("data")]
    [HasDefaultValue]
    public string? DataFolder { get; set; } = null;
}