using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Miracle.Windows;

public sealed partial class ConversionService
{
    private static readonly HashSet<string> Images = new(StringComparer.OrdinalIgnoreCase) { "png", "jpg", "jpeg", "bmp", "tif", "tiff", "gif", "webp", "heic", "heif", "avif", "svg" };
    private static readonly HashSet<string> Audio = new(StringComparer.OrdinalIgnoreCase) { "mp3", "m4a", "wav", "flac", "ogg", "opus", "aiff", "wma" };
    private static readonly HashSet<string> Video = new(StringComparer.OrdinalIgnoreCase) { "mp4", "mov", "mkv", "webm", "avi", "wmv" };
    private readonly Dictionary<string, string> codecs = new()
    {
        ["mp3"]="libmp3lame", ["m4a"]="aac", ["wav"]="pcm_s16le", ["flac"]="flac", ["ogg"]="libvorbis", ["opus"]="libopus", ["aiff"]="pcm_s16be", ["wma"]="wmav2",
        ["mp4"]="libx264", ["mov"]="libx264", ["mkv"]="libx264", ["webm"]="libvpx-vp9", ["gif"]="gif", ["avi"]="mpeg4", ["wmv"]="wmv2", ["webp"]="libwebp"
    };
    private HashSet<string> encoders = new(StringComparer.Ordinal);
    public string? FFmpegPath { get; private set; }
    public string EngineStatus { get; private set; } = "Images and subtitles ready. Checking FFmpeg…";
    public static string Extension(string path) => Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
    private static string Canonical(string ext) => ext switch { "jpeg"=>"jpg", "tif"=>"tiff", "markdown"=>"md", "gz"=>"gzip", _=>ext };

    public async Task DetectAsync(string? selected = null)
    {
        FFmpegPath = null; EnginePaths.Media = null;
        encoders.Clear();
        var candidates = new List<string>();
        if (selected != null) candidates.Add(selected);
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "Engines", "Media", "ffmpeg.exe"));
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "Engines", "ffmpeg.exe"));
        foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            try { if (!string.IsNullOrWhiteSpace(dir)) candidates.Add(Path.Combine(dir.Trim('"'), "ffmpeg.exe")); } catch (ArgumentException) { }
        }
        foreach (string candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Path.IsPathFullyQualified(candidate) || !File.Exists(candidate)) continue;
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                string output = await RunAsync(candidate, new[] { "-hide_banner", "-encoders" }, timeout.Token);
                encoders = Regex.Matches(output, @"(?m)^\s*[VAS][A-Z\.]{5}\s+(\S+)").Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
                if (encoders.Count == 0) continue;
                FFmpegPath = candidate; EnginePaths.Media = candidate;
                EngineStatus = "Local image, PDF, document, archive & subtitle engines ready · FFmpeg ready" + (EnginePaths.Office!=null?" · LibreOffice ready":" · locate LibreOffice for slides/sheets");
                return;
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or OperationCanceledException or System.ComponentModel.Win32Exception) { }
        }
        EngineStatus = "Native images & subtitles ready · choose FFmpeg for media";
    }

    public IReadOnlyList<string> Outputs(string input)
    {
        string ext = Extension(input), canonical = Canonical(ext);
        IEnumerable<string> options;
        if (Images.Contains(ext)) options = new[] { "jpg", "png", "webp", "heic", "tiff", "avif", "bmp", "pdf" }.Where(x => x == "pdf" || ImageStudio.CanWrite(x));
        else if (AdvancedOutputs(ext) is { } advanced) options = advanced;
        else if (ext is "srt" or "vtt") options = new[] { "srt", "vtt", "txt" };
        else if (FFmpegPath != null && Audio.Contains(ext)) options = new[] { "mp3", "m4a", "wav", "flac", "ogg", "opus", "aiff", "wma" }.Where(CanEncode);
        else if (FFmpegPath != null && Video.Contains(ext)) options = new[] { "mp4", "mov", "mkv", "webm", "avi", "wmv", "gif", "mp3" }.Where(CanEncode);
        else options = Array.Empty<string>();
        if (ext=="gif" && FFmpegPath!=null) options=options.Concat(new[]{"mp4","mov","mkv","webm","avi","wmv"}.Where(CanEncode));
        return options.Where(x => x != canonical).Distinct().ToArray();
    }
    private bool CanEncode(string ext) => codecs.TryGetValue(ext, out string? codec) && encoders.Contains(codec) && (ext is not ("mp4" or "mov" or "mkv" or "webm") || encoders.Contains(ext == "webm" ? "libopus" : "aac"));
    public IReadOnlyList<string> CommonOutputs(IEnumerable<string> files)
    {
        var list = files.ToArray();
        return list.Length == 0 ? Array.Empty<string>() : Outputs(list[0]).Where(format => list.All(f => Outputs(f).Contains(format))).ToArray();
    }

    public async Task<string> ConvertAsync(string input, string format, string? directory, CancellationToken token)
    {
        var outputs = await ConvertAllAsync(input, format, directory, token);
        return outputs[0];
    }
    private async Task ConvertMediaAsync(string input, string output, string format, bool image, CancellationToken token)
    {
        if (FFmpegPath == null) throw new InvalidOperationException("Choose ffmpeg.exe first.");
        var args = new List<string> { "-hide_banner", "-loglevel", "error", "-nostdin", "-n", "-protocol_whitelist", "file,pipe", "-i", input };
        if (image) { args.AddRange(new[] { "-frames:v", "1" }); if (format == "webp") args.AddRange(new[] { "-c:v", "libwebp" }); }
        else if (format is "mp3" or "m4a" or "wav" or "flac" or "ogg" or "opus" or "aiff" or "wma") args.AddRange(new[] { "-vn", "-c:a", codecs[format] });
        else if (format == "gif") args.AddRange(new[] { "-an", "-vf", "fps=15,scale=960:-1:force_original_aspect_ratio=decrease" });
        else args.AddRange(new[] { "-c:v", codecs[format], "-c:a", format == "webm" ? "libopus" : format == "wmv" ? "wmav2" : format == "avi" ? "libmp3lame" : "aac", "-pix_fmt", "yuv420p" });
        args.Add(output);
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(token);
        limit.CancelAfter(TimeSpan.FromMinutes(30));
        await RunAsync(FFmpegPath, args, limit.Token);
    }
    internal static async Task<string> RunAsync(string executable, IEnumerable<string> args, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!Path.IsPathFullyQualified(executable)) throw new InvalidOperationException("Engine path must be absolute.");
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new IOException("Could not start the conversion engine.");
        // Drain both streams concurrently to avoid child-process pipe deadlocks.
        var stdout = ReadBoundedAsync(process.StandardOutput);
        var stderr = ReadBoundedAsync(process.StandardError);
        try { await process.WaitForExitAsync(token); }
        catch (OperationCanceledException) { try { process.Kill(true); } catch (InvalidOperationException) { } await process.WaitForExitAsync(); await Task.WhenAll(stdout, stderr); throw; }
        string[] result = await Task.WhenAll(stdout, stderr);
        if (process.ExitCode != 0) throw new IOException(string.IsNullOrWhiteSpace(result[1]) ? "The conversion engine failed." : result[1].Trim());
        return result[0] + "\n" + result[1];
    }
    private static async Task<string> ReadBoundedAsync(StreamReader reader)
    {
        var text = new StringBuilder(); var buffer = new char[4096]; int count;
        while ((count = await reader.ReadAsync(buffer)) > 0) { int keep = Math.Min(count, 131072 - text.Length); if (keep > 0) text.Append(buffer, 0, keep); }
        return text.ToString();
    }
    private static async Task ConvertSubtitleAsync(string input, string output, string format, CancellationToken token)
    {
        if (new FileInfo(input).Length > 8L * 1024 * 1024) throw new InvalidDataException("Subtitle input exceeds the 8 MB limit.");
        string text = (await File.ReadAllTextAsync(input, token)).Replace("\r\n", "\n").TrimStart('\uFEFF');
        var blocks = Regex.Split(text.Trim(), @"\n\s*\n");
        var cues = new List<(string Time, string Body)>();
        foreach (string block in blocks)
        {
            string[] lines = block.Split('\n'); int timing = Array.FindIndex(lines, x => x.Contains("-->"));
            if (timing < 0) continue;
            string time = Regex.Replace(lines[timing], @"(?<=\d),(?=\d{3})", ".");
            time = Regex.Replace(time, @"(?<![\d:])(\d{2}:\d{2}\.\d{3})", "00:$1");
            if (!Regex.IsMatch(time, @"^\d{2}:\d{2}:\d{2}\.\d{3}\s+-->\s+\d{2}:\d{2}:\d{2}\.\d{3}")) throw new InvalidDataException("Invalid subtitle timestamp.");
            cues.Add((time, string.Join("\n", lines.Skip(timing + 1))));
        }
        if (cues.Count == 0) throw new InvalidDataException("No valid subtitle cues were found.");
        string result = format switch
        {
            "txt" => string.Join("\n\n", cues.Select(c => Regex.Replace(c.Body, "<[^>]+>", "", RegexOptions.NonBacktracking, TimeSpan.FromSeconds(2)))),
            "vtt" => "WEBVTT\n\n" + string.Join("\n\n", cues.Select(c => c.Time + "\n" + c.Body)),
            _ => string.Join("\n\n", cues.Select((c, i) => (i + 1) + "\n" + Regex.Replace(Regex.Match(c.Time, @"\d{2}:\d{2}:\d{2}\.\d{3}\s+-->\s+\d{2}:\d{2}:\d{2}\.\d{3}").Value, @"\.(?=\d{3})", ",") + "\n" + c.Body))
        };
        await File.WriteAllTextAsync(output, result + "\n", new UTF8Encoding(false), token);
    }
}
