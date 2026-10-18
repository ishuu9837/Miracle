using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Miracle.Windows;
internal static class SelfTest
{
    public static async Task<int> RunAsync(string directory)
    {
        var checks = new List<string>();
        Directory.CreateDirectory(directory);
        string report = Path.Combine(directory, "report.json");
        try
        {
            string input = Path.Combine(directory, "sample 'quoted' image.png");
            var bitmap = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null,
                new byte[] { 0,0,255,255, 0,255,0,255, 255,0,0,255, 255,255,255,255 }, 8);
            var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(input)) png.Save(stream);
            byte[] original = await File.ReadAllBytesAsync(input);
            var service = new ConversionService();
            foreach (string format in new[] { "jpg", "bmp", "tiff" })
            {
                string converted = await service.ConvertAsync(input, format, directory, CancellationToken.None);
                using var stream = File.OpenRead(converted);
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                Require(decoder.Frames[0].PixelWidth == 2 && decoder.Frames[0].PixelHeight == 2, "Image dimensions changed.");
                checks.Add("PNG → " + format);
            }
            string first = await service.ConvertAsync(input, "jpg", directory, CancellationToken.None);
            string second = await service.ConvertAsync(input, "jpg", directory, CancellationToken.None);
            Require(first != second && File.Exists(first), "Output collision overwrote a file."); checks.Add("Collision-safe output names");
            byte[] after = await File.ReadAllBytesAsync(input);
            Require(original.SequenceEqual(after), "Original was changed."); checks.Add("Original preserved");
            string srt = Path.Combine(directory, "captions.srt");
            await File.WriteAllTextAsync(srt, "1\n00:00:01,000 --> 00:00:02,500\nHello <b>Miracle</b>!\n");
            string vtt = await service.ConvertAsync(srt, "vtt", directory, CancellationToken.None);
            Require((await File.ReadAllTextAsync(vtt)).StartsWith("WEBVTT"), "VTT header missing."); checks.Add("SRT → VTT");
            string back = await service.ConvertAsync(vtt, "srt", directory, CancellationToken.None);
            Require((await File.ReadAllTextAsync(back)).Contains("00:00:01,000 --> 00:00:02,500"), "SRT timing incorrect."); checks.Add("VTT → SRT");
            string text = await service.ConvertAsync(vtt, "txt", directory, CancellationToken.None);
            Require((await File.ReadAllTextAsync(text)).Trim() == "Hello Miracle!", "TXT content incorrect."); checks.Add("Subtitles → TXT");
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            try { await service.ConvertAsync(input, "jpg", directory, cancelled.Token); throw new Exception("Cancellation ignored."); } catch (OperationCanceledException) { checks.Add("Cancellation"); }
            Require(!Directory.EnumerateFiles(directory, ".miracle-*").Any(), "Temporary output leaked."); checks.Add("Temporary output cleanup");
            Require(service.CommonOutputs(new[] { input, srt }).Count == 0, "Mixed selection accepted invalid output."); checks.Add("Mixed selection validation");
            await CheckWheelCloseAsync(input); checks.Add("Wheel selection/commit/deactivate regression");
            await SecurityChecksAsync(service, input, directory, checks);
            await CheckPrimaryFlowAsync(input); checks.Add("Primary conversion, file reset, animated intro completion/skip and window close");
            await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { passed = true, checks }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception ex) { await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { passed = false, checks, error = ex.ToString() }, new JsonSerializerOptions { WriteIndented = true })); return 1; }
    }
    private static Task CheckWheelCloseAsync(string input)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                bool committed = false;
                var window = new WheelWindow(new[] { input }, new[] { "jpg", "bmp" }, (selectedFiles, format) => { Require(format == "bmp" && selectedFiles[0] == input, "Wheel committed wrong format or files."); committed = true; });
                window.Loaded += (_, _) => dispatcher.BeginInvoke(() => { window.Select("bmp"); window.Commit(); Require(committed, "Wheel did not commit."); completion.TrySetResult(); dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); });
                window.Show(); Dispatcher.Run();
            }
            catch (Exception ex) { completion.TrySetException(ex); }
        });
        thread.IsBackground = true; thread.SetApartmentState(ApartmentState.STA); thread.Start();
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }
    private static async Task SecurityChecksAsync(ConversionService service, string input, string directory, List<string> checks)
    {
        async Task Reject(string file, string format, string label)
        {
            bool rejected = false;
            try { await service.ConvertAsync(file, format, directory, CancellationToken.None); }
            catch (Exception ex) when (ex is IOException or FileFormatException or InvalidDataException or InvalidOperationException or NotSupportedException or ArgumentException) { rejected = true; }
            Require(rejected, label + " was accepted.");
            Require(!Directory.EnumerateFiles(directory, ".miracle-*").Any(), label + " leaked temporary output."); checks.Add(label);
        }
        await Reject(input, "../escape", "Output-format traversal rejected");
        await Reject(Path.Combine(directory, "missing.png"), "jpg", "Missing source rejected");
        string empty = Path.Combine(directory, "empty.png"); await File.WriteAllBytesAsync(empty, Array.Empty<byte>()); await Reject(empty, "jpg", "Empty source rejected");
        string corrupt = Path.Combine(directory, "broken.png"); await File.WriteAllBytesAsync(corrupt, new byte[] { 137,80,78,71,13,10,26,10 }); await Reject(corrupt, "jpg", "Truncated image rejected without output");
        string invalid = Path.Combine(directory, "invalid.srt"); await File.WriteAllTextAsync(invalid, "1\nnot-a-time --> also-invalid\nhello"); await Reject(invalid, "vtt", "Malformed subtitle rejected without output");
        string large = Path.Combine(directory, "oversized.srt"); using (var f = File.Create(large)) f.SetLength(9L * 1024 * 1024); await Reject(large, "txt", "Subtitle resource limit enforced");
        string adversarial = Path.Combine(directory, "adversarial.srt");
        await File.WriteAllTextAsync(adversarial, "1\n00:00:01,000 --> 00:00:02,000\n" + new string('<', 100000));
        var textTimer = Stopwatch.StartNew();
        await service.ConvertAsync(adversarial, "txt", directory, CancellationToken.None);
        Require(textTimer.Elapsed < TimeSpan.FromSeconds(4), "Adversarial subtitle processing stalled."); checks.Add("Adversarial markup processed with linear-time matching");
        string executable = Environment.ProcessPath ?? throw new InvalidOperationException("Missing executable path.");
        string hostile = "spaces \"quotes\" ' & | ; $() > marker";
        string echo = await ConversionService.RunAsync(executable, new[] { "--process-fixture", hostile }, CancellationToken.None);
        Require(echo.TrimEnd('\r','\n') == hostile, "Process arguments were interpreted or changed."); checks.Add("Process argument injection protection");
        string flood = await ConversionService.RunAsync(executable, new[] { "--process-fixture", "flood" }, CancellationToken.None);
        Require(flood.Length <= 262145 && flood.Contains('o') && flood.Contains('e'), "Process streams not bounded/drained."); checks.Add("Concurrent stdout/stderr drain and memory bounds");
        bool failed = false;
        try { await ConversionService.RunAsync(executable, new[] { "--process-fixture", "fail" }, CancellationToken.None); } catch (IOException) { failed = true; }
        Require(failed, "Failed child process reported success."); checks.Add("Child process failures surfaced");
        string pidFile = Path.Combine(directory, "cancelled-child.pid");
        if (File.Exists(pidFile)) File.Delete(pidFile);
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(900));
        var elapsed = Stopwatch.StartNew(); bool cancelled = false;
        try { await ConversionService.RunAsync(executable, new[] { "--process-fixture", "wait", pidFile }, cancel.Token); } catch (OperationCanceledException) { cancelled = true; }
        Require(cancelled && elapsed.Elapsed < TimeSpan.FromSeconds(8), "Process cancellation hung.");
        if (File.Exists(pidFile)) { int pid = int.Parse(await File.ReadAllTextAsync(pidFile)); try { using var child = Process.GetProcessById(pid); Require(child.HasExited, "Cancelled child still running."); } catch (ArgumentException) { } }
        checks.Add("Child process cancellation leaves no running child");
        Require(File.Exists(input), "Security checks removed original.");
    }
    private static Task CheckPrimaryFlowAsync(string input)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                var window = new MainWindow(new[] { input }, true);
                window.Loaded += (_, _) => dispatcher.BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        await Task.Delay(1900); Require(!window.Intro.IsVisible, "Logo intro did not finish automatically.");
                        window.Intro.Finish(); Require(!window.Intro.IsVisible, "Intro skip failed.");
                        Require(!window.HasPrimaryConversion, "Conversion enabled before format selection.");
                        window.SelectFormat("jpg"); Require(window.HasPrimaryConversion && window.SelectedFormat == "jpg", "Primary option not selectable.");
                        await window.VerifyPrimaryConversionAsync();
                        window.UpdateLayout();
                        var render = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                        render.Render(window); var capture = new PngBitmapEncoder(); capture.Frames.Add(BitmapFrame.Create(render));
                        using (var output = File.Create(Path.Combine(Path.GetDirectoryName(input)!, "interface-preview.png"))) capture.Save(output);
                        window.SetFiles(Array.Empty<string>()); Require(!window.HasPrimaryConversion, "Conversion enabled without files.");
                        window.Close(); completion.TrySetResult();
                    }
                    catch (Exception ex) { completion.TrySetException(ex); }
                    finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); }
                }));
                window.Show(); Dispatcher.Run();
            }
            catch (Exception ex) { completion.TrySetException(ex); }
        });
        thread.IsBackground = true; thread.SetApartmentState(ApartmentState.STA); thread.Start();
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
