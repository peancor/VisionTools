using Cocona;
using RsCapture;
using RsCapture.Commands;
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
}).WithDescription("list detected realsense devices");


app.AddCommand("info", async ([Option("sn")] string? sn) =>
{
    await InfoCommand.RunAsync(sn);
}).WithDescription("shows device info");

app.AddCommand("run", async (RunParameters p, CoconaAppContext ctx) =>
{
    try
    {   
        //Si no arrancamos
        await RunCommand.RunCapture(p, ctx);
    }
    catch (Exception ex)
    {   
        ConsoleUtils.WriteErrorMessage(ex.Message);
    }
}).WithDescription("runs capture");

app.AddCommand("dm", async (DeepMeasureCommandParameters p, CoconaAppContext ctx) =>
{
    try
    {
        //Si no arrancamos
        await DeepMeasureCommand.RunCapture(p, ctx);
    }
    catch (Exception ex)
    {
        ConsoleUtils.WriteErrorMessage(ex.Message);
    }
}).WithDescription("takes one measure averaging all frames");

AnsiConsole.Write(
    new FigletText("RsCapture")
        .LeftAligned()
        .Color(Color.Lime));

app.Run();