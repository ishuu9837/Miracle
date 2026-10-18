using System;
using System.Collections.Generic;
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
            await CheckWheelCloseAsync(input); checks.Add("Wheel close/deactivate regression");
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
                var window = new WheelWindow(new[] { input }, new[] { "jpg", "bmp" }, (_, _) => { });
                window.Loaded += (_, _) => dispatcher.BeginInvoke(() => { window.Close(); completion.TrySetResult(); dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); });
                window.Show(); Dispatcher.Run();
            }
            catch (Exception ex) { completion.TrySetException(ex); }
        });
        thread.IsBackground = true; thread.SetApartmentState(ApartmentState.STA); thread.Start();
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
