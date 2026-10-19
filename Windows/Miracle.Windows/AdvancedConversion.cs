using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Miracle.Windows;
public sealed partial class ConversionService
{
    internal static string Category(string path) => Extension(path) switch
    {
        "png" or "jpg" or "jpeg" or "webp" or "heic" or "heif" or "tif" or "tiff" or "avif" or "bmp" or "svg" or "gif" => "image",
        "mp4" or "mov" or "mkv" or "webm" or "avi" or "wmv" => "video",
        "mp3" or "m4a" or "wav" or "flac" or "ogg" or "opus" or "aiff" or "wma" => "audio",
        "pdf" => "pdf", "zip" or "tar" or "gz" or "gzip" or "rar" => "archive",
        "ppt" or "pptx" => "slides", "xlsx" or "xls" or "ods" or "csv" or "tsv" => "sheet",
        "txt" or "md" or "markdown" or "docx" or "doc" or "rtf" or "odt" or "epub" or "html" or "htm" => "document", _ => "other"
    };
    private static IEnumerable<string>? AdvancedOutputs(string ext)
    {
        string kind = Category("file."+ext); bool office = EnginePaths.Office != null;
        return kind switch
        {
            "pdf" => new[] { "docx", "pptx", "xlsx", "jpg", "png", "epub", "txt", "md" },
            "document" when ext is "rtf" or "doc" or "odt" => office ? new[] { "docx", "pdf", "txt", "md", "html", "epub", "png", "jpg" } : Array.Empty<string>(),
            "document" => new[] { "docx", "pdf", "txt", "md", "html", "epub", "png", "jpg" },
            "slides" => office ? new[] { "pdf", "jpg", "png" } : Array.Empty<string>(),
            "sheet" => office ? new[] { "xlsx", "ods", "csv", "tsv", "pdf" } : Array.Empty<string>(),
            "archive" => new[] { "zip", "tar", "gzip" }, _ => null
        };
    }
    public async Task<IReadOnlyList<string>> ConvertAllAsync(string input, string format, string? directory, CancellationToken token)
    {
        if (!Outputs(input).Contains(format)) throw new InvalidOperationException("This conversion is unavailable with the installed engines.");
        ValidateInput(input); token.ThrowIfCancellationRequested();
        using var job = new OutputJob(input, directory, "converted");
        string ext = Extension(input), kind = Category(input), output = job.File(format);
        IReadOnlyList<string> generated;
        if (ext is "srt" or "vtt") { await ConvertSubtitleAsync(input, output, format, token); generated = new[] { output }; }
        else if (ext=="gif" && format is "mp4" or "mov" or "mkv" or "webm" or "avi" or "wmv") { await ConvertMediaAsync(input,output,format,false,token);generated=new[]{output}; }
        else if (kind == "image")
        {
            if (format == "pdf") await Task.Run(() => PdfStudio.ImagesToPdf(new[] { input }, output), token);
            else await ImageStudio.ConvertAsync(input, output, format, token);
            generated = new[] { output };
        }
        else if (kind == "pdf" && format is "jpg" or "png") generated = await PdfStudio.RenderAsync(input, job.Stage, format, token);
        else if (kind is "pdf" or "document" or "slides" or "sheet") generated = await DocumentStudio.ConvertAsync(input, format, job.Stage, token);
        else if (kind == "archive") generated = new[] { await ArchiveStudio.ConvertAsync(input, format, job.Stage, token) };
        else { await ConvertMediaAsync(input, output, format, false, token); generated = new[] { output }; }
        return job.Commit(generated, token);
    }
    internal static void ValidateInput(string input)
    {
        if (!File.Exists(input)) throw new FileNotFoundException("Source file no longer exists.", input);
        if (new FileInfo(input).Length == 0) throw new InvalidDataException("Source file is empty.");
    }
}
