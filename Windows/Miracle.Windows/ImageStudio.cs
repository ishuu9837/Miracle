using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using ImageMagick.Configuration;
using ImageMagick.Drawing;

namespace Miracle.Windows;
internal static class ImageStudio
{
    private static readonly Lazy<bool> setup = new(() =>
    {
        var configuration = ConfigurationFiles.Default;
        configuration.Policy.Data = """
        <policymap>
          <policy domain="delegate" rights="none" pattern="*" />
          <policy domain="filter" rights="none" pattern="*" />
          <policy domain="path" rights="none" pattern="@*" />
          <policy domain="coder" rights="none" pattern="*" />
          <policy domain="coder" rights="read|write" pattern="{PNG,JPEG,JPG,WEBP,HEIC,HEIF,AVIF,TIFF,BMP,GIF,XC,TEXT,LABEL}" />
          <policy domain="coder" rights="read" pattern="{SVG,MSVG,MVG}" />
          <policy domain="resource" name="memory" value="512MiB" />
          <policy domain="resource" name="map" value="512MiB" />
          <policy domain="resource" name="disk" value="1GiB" />
          <policy domain="resource" name="width" value="20KP" />
          <policy domain="resource" name="height" value="20KP" />
          <policy domain="resource" name="list-length" value="100" />
          <policy domain="resource" name="time" value="120" />
        </policymap>
        """;
        MagickNET.Initialize(configuration);
        ResourceLimits.Thread = 2;
        return true;
    });
    internal static void Initialize() { _ = setup.Value; }
    public static MagickFormat Format(string value) => value switch { "jpg" or "jpeg" => MagickFormat.Jpeg, "png" => MagickFormat.Png, "webp" => MagickFormat.WebP, "heic" or "heif" => MagickFormat.Heic, "avif" => MagickFormat.Avif, "tiff" or "tif" => MagickFormat.Tiff, "bmp" => MagickFormat.Bmp, "gif" => MagickFormat.Gif, _ => throw new InvalidOperationException("Unknown image format.") };
    public static bool CanWrite(string format) { if(format=="heic")return EnginePaths.HeifEncoder!=null; Initialize(); var kind = Format(format); return MagickNET.SupportedFormats.Any(f => f.Format == kind && f.SupportsWriting); }
    public static MagickImage Read(string input)
    {
        Initialize();
        if (new FileInfo(input).Length > 256L * 1024 * 1024) throw new InvalidDataException("Image exceeds 256 MB.");
        if (Path.GetExtension(input).Equals(".svg", StringComparison.OrdinalIgnoreCase))
        {
            string svg = System.IO.File.ReadAllText(input);
            if (svg.Contains("<!ENTITY", StringComparison.OrdinalIgnoreCase) || svg.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) || svg.Contains("href", StringComparison.OrdinalIgnoreCase) || svg.Contains("url(", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("SVG external references are not allowed.");
        }
        // Force the declared image coder: MVG is permitted only as an internal SVG renderer,
        // never by format sniffing arbitrary input with a misleading extension.
        var settings = new MagickReadSettings { FrameCount = 1, FrameIndex = 0, Format = ConversionService.Extension(input)=="svg"?MagickFormat.Msvg:Format(ConversionService.Extension(input)) };
        var info = new MagickImageInfo(input,settings);
        if ((ulong)info.Width * info.Height > 80_000_000) throw new InvalidDataException("Image exceeds 80 megapixels.");
        return new MagickImage(input, settings);
    }
    public static async Task ConvertAsync(string input, string output, string format, CancellationToken token)
    {
        if(format=="heic")
        {
            string encoder=EnginePaths.HeifEncoder??throw new InvalidOperationException("The local HEIC encoder is missing.");
            string png=output+".png";
            try
            {
                await Task.Run(()=>{using var image=Read(input);image.Write(png,MagickFormat.Png);},token);
                using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromMinutes(2));
                await ConversionService.RunAsync(encoder,new[]{"-q","90","-o",output,png},timeout.Token);
            }
            finally{if(System.IO.File.Exists(png))System.IO.File.Delete(png);}
        }
        else await Task.Run(() => { token.ThrowIfCancellationRequested(); using var image = Read(input); image.Quality = 92; image.Write(output, Format(format)); token.ThrowIfCancellationRequested(); }, token);
    }
    public static async Task<string> ApplyAsync(string input, string output, string tool, ToolValues values, CancellationToken token)
    {
        await Task.Run(() =>
        {
            token.ThrowIfCancellationRequested(); using var image = Read(input);
            switch (tool)
            {
                case "image.compress": image.Quality = (uint)values.Number("quality", 75, 1, 100); break;
                case "image.metadata":
                    if (values.Flag("clear")) image.Strip();
                    image.SetAttribute("comment", values.Text("description"));
                    var exif = image.GetExifProfile() ?? new ExifProfile(); exif.SetValue(ExifTag.Artist, values.Text("author")); exif.SetValue(ExifTag.ImageDescription, values.Text("description")); exif.SetValue(ExifTag.Copyright, values.Text("copyright")); image.SetProfile(exif); break;
                case "image.edit": image.BrightnessContrast(new Percentage(values.Number("brightness", 0, -100, 100)), new Percentage(values.Number("contrast", 0, -100, 100))); image.Modulate(new Percentage(100), new Percentage(values.Number("saturation", 100, 0, 300))); image.Rotate(values.Number("rotate", 0, -360, 360)); break;
                case "image.annotate": new Drawables().Font("Arial").FontPointSize(values.Number("size", 28, 6, 300)).FillColor(new MagickColor(values.Color("color", "#000000"))).Text(values.Number("x", 20, 0, image.Width), values.Number("y", 40, 0, image.Height), values.Required("text")).Draw(image); break;
                case "image.background": image.BackgroundColor = new MagickColor(values.Color("color", "#FFFFFF")); image.Alpha(AlphaAction.Remove); break;
                case "image.crop":
                case "image.redact":
                    int x = (int)values.Number("x", 0, 0, image.Width - 1), y = (int)values.Number("y", 0, 0, image.Height - 1);
                    uint width = (uint)values.Number("width", image.Width - x, 1, image.Width - x), height = (uint)values.Number("height", image.Height - y, 1, image.Height - y);
                    if (tool == "image.crop") { image.Crop(new MagickGeometry(x, y, width, height)); image.ResetPage(); }
                    else { new Drawables().FillColor(MagickColors.Black).StrokeColor(MagickColors.Black).Rectangle(x, y, x + width - 1, y + height - 1).Draw(image); image.Strip(); }
                    break;
                default: throw new InvalidOperationException("Unknown image tool.");
            }
            image.Write(output, tool == "image.compress" ? MagickFormat.Jpeg : MagickFormat.Png); token.ThrowIfCancellationRequested();
        }, token);
        return output;
    }
    public static string Describe(string input)
    {
        using var image=Read(input);var exif=image.GetExifProfile();
        return $"{image.Width} × {image.Height} pixels\nAuthor: {exif?.GetValue(ExifTag.Artist)?.Value}\nDescription: {exif?.GetValue(ExifTag.ImageDescription)?.Value}\nCopyright: {exif?.GetValue(ExifTag.Copyright)?.Value}\n"+string.Join("\n",image.AttributeNames.Take(40).Select(n=>n+": "+image.GetAttribute(n)));
    }
}
