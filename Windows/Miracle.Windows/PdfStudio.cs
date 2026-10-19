using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Miracle.Windows;
internal static class PdfStudio
{
    public static void Validate(PdfDocument document) { if (document.PageCount is < 1 or > 500) throw new InvalidDataException("PDFs must contain 1–500 pages."); }
    public static IReadOnlyList<string> TextPages(string input)
    {
        using var document = UglyToad.PdfPig.PdfDocument.Open(input);
        if (document.NumberOfPages is < 1 or > 500) throw new InvalidDataException("PDF must contain 1–500 pages.");
        var result = new List<string>(); int total = 0;
        foreach (var page in document.GetPages()) { string text = UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor.ContentOrderTextExtractor.GetText(page); total += text.Length; if (total > 20_000_000) throw new InvalidDataException("PDF text exceeds the processing budget."); result.Add(text); }
        return result;
    }
    public static Task<IReadOnlyList<string>> RenderAsync(string input,string directory,string format,CancellationToken token)=>NativePdfRenderer.RenderAsync(input,directory,format,token);
    public static void ImagesToPdf(IEnumerable<string> inputs, string output)
    {
        using var document = new PdfDocument();
        foreach (string input in inputs)
        {
            using var raster = ImageStudio.Read(input); using var bytes = new MemoryStream(raster.ToByteArray(MagickFormat.Png)); using var image = XImage.FromStream(bytes);
            var page = document.AddPage(); page.Width = XUnit.FromPoint(raster.Width * 72.0 / 96); page.Height = XUnit.FromPoint(raster.Height * 72.0 / 96);
            using var graphics = XGraphics.FromPdfPage(page); graphics.DrawImage(image, 0, 0, page.Width.Point, page.Height.Point);
        }
        document.Save(output);
    }
    public static void TextToPdf(string text, string output, string title)
    {
        if(text.Length>20_000_000)throw new InvalidDataException("Text exceeds PDF processing budget.");
        using var document = new PdfDocument(); document.Info.Title = title;
        var font = new XFont("Segoe UI", 11, XFontStyleEx.Regular);
        PdfPage page = document.AddPage(); XGraphics graphics = XGraphics.FromPdfPage(page); double y = 48;
        try
        {
            foreach (string paragraph in text.Replace("\r", "").Split('\n'))
            {
                var words = paragraph.Split(' '); var line = new StringBuilder();
                foreach (string word in words)
                {
                    if (line.Length > 0 && graphics.MeasureString(line + " " + word, font).Width > page.Width.Point - 96) { DrawLine(line.ToString()); line.Clear(); }
                    if (word.Length > 200) { if (line.Length > 0) { DrawLine(line.ToString()); line.Clear(); } for (int i = 0; i < word.Length; i += 75) DrawLine(word.Substring(i, Math.Min(75, word.Length - i))); }
                    else { if (line.Length > 0) line.Append(' '); line.Append(word); }
                }
                DrawLine(line.ToString());
            }
            graphics.Dispose(); document.Save(output);
        }
        finally { graphics.Dispose(); }
        void DrawLine(string value)
        {
            if (y > page.Height.Point - 48) { graphics.Dispose(); page = document.AddPage(); graphics = XGraphics.FromPdfPage(page); y = 48; if (document.PageCount > 500) throw new InvalidDataException("Text export exceeds 500 pages."); }
            graphics.DrawString(value, font, XBrushes.Black, new XPoint(48, y)); y += 16;
        }
    }
    public static IReadOnlyList<string> Apply(string[] inputs, string directory, string tool, ToolValues values, CancellationToken token)
    {
        var results = new List<string>();
        if (tool == "pdf.organize" && inputs.Length > 1)
        {
            using var combined = new PdfDocument();
            foreach (string input in inputs) { using var source = PdfReader.Open(input, PdfDocumentOpenMode.Import); Validate(source); foreach (var page in source.Pages) combined.AddPage(page); if (combined.PageCount > 500) throw new InvalidDataException("Combined PDF exceeds 500 pages."); }
            string path = Path.Combine(directory, "combined.pdf"); combined.Save(path); return new[] { path };
        }
        foreach (string input in inputs)
        {
            token.ThrowIfCancellationRequested(); using var source = PdfReader.Open(input, PdfDocumentOpenMode.Import); Validate(source);
            if (tool == "pdf.split")
            {
                for (int i = 0; i < source.PageCount; i++) { token.ThrowIfCancellationRequested(); using var one = new PdfDocument(); one.AddPage(source.Pages[i]); string path = Path.Combine(directory, "page-" + (i + 1).ToString("D3") + ".pdf"); one.Save(path); results.Add(path); }
            }
            else
            {
                using var output = new PdfDocument();
                var order = tool == "pdf.organize" ? ParsePages(values.Text("pages"), source.PageCount) : Enumerable.Range(0, source.PageCount).ToArray();
                int rotation = (int)values.Number("rotate", 0, 0, 270); if (rotation % 90 != 0) throw new ArgumentException("Rotation must be a multiple of 90.");
                foreach (int index in order) { var page = output.AddPage(source.Pages[index]); page.Rotate = (page.Rotate + rotation) % 360; }
                if (tool == "pdf.metadata") { output.Info.Title = values.Text("title"); output.Info.Author = values.Text("author"); output.Info.Subject = values.Text("description"); output.Info.Keywords = values.Text("keywords"); }
                output.Options.CompressContentStreams = true;
                string path = Path.Combine(directory, "result.pdf"); output.Save(path); results.Add(path);
            }
        }
        return results;
    }
    public static string Describe(string input)
    {
        using var doc=PdfReader.Open(input,PdfDocumentOpenMode.Import);Validate(doc);
        return $"Pages: {doc.PageCount}\nTitle: {doc.Info.Title}\nAuthor: {doc.Info.Author}\nSubject: {doc.Info.Subject}\nKeywords: {doc.Info.Keywords}";
    }
    public static int[] ParsePages(string text, int count)
    {
        if (string.IsNullOrWhiteSpace(text)) return Enumerable.Range(0, count).ToArray();
        var indices = new List<int>();
        foreach (string part in text.Split(','))
        {
            string[] limits = part.Trim().Split('-');
            if (limits.Length > 2 || !int.TryParse(limits[0], out int first)) throw new ArgumentException("Use page numbers or ranges: 1,3,5-7.");
            int last = first; if (limits.Length == 2 && !int.TryParse(limits[1], out last)) throw new ArgumentException("Invalid page range.");
            if (first < 1 || last < first || last > count) throw new ArgumentException("Page number outside the document.");
            for (int page = first; page <= last; page++) { indices.Add(page - 1); if (indices.Count > 500) throw new ArgumentException("At most 500 output pages."); }
        }
        if (indices.Count == 0) throw new ArgumentException("Choose at least one page."); return indices.ToArray();
    }
}
