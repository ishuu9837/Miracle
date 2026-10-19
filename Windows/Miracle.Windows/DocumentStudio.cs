using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;
using S = DocumentFormat.OpenXml.Spreadsheet;

namespace Miracle.Windows;
internal static class DocumentStudio
{
    private static readonly Regex Tags = new("<[^>]*>", RegexOptions.NonBacktracking, TimeSpan.FromSeconds(2));
    private static string HtmlText(string html) => WebUtility.HtmlDecode(Tags.Replace(Regex.Replace(html, "</?(p|div|h[1-6]|li|br|tr)[^>]*>", "\n", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, TimeSpan.FromSeconds(2)), "")).Trim();
    public static string Text(string input)
    {
        string ext = ConversionService.Extension(input);
        if (new FileInfo(input).Length > 256L * 1024 * 1024) throw new InvalidDataException("Document exceeds 256 MB.");
        if (ext == "pdf") return string.Join("\n\n", PdfStudio.TextPages(input));
        if (ext == "docx") { using(var package=ZipFile.OpenRead(input))CheckZip(package); using var doc = WordprocessingDocument.Open(input, false); return string.Join("\n", doc.MainDocumentPart?.Document?.Descendants<W.Paragraph>().Select(p=>p.InnerText) ?? Array.Empty<string>()); }
        if (ext == "epub")
        {
            using var zip = ZipFile.OpenRead(input); CheckZip(zip);
            var container = SafeXml(ReadEntry(zip, "META-INF/container.xml")); string package = container.Descendants().First(e=>e.Name.LocalName=="rootfile").Attribute("full-path")?.Value ?? throw new InvalidDataException("EPUB has no package.");
            var opf = SafeXml(ReadEntry(zip, package)); string folder = package.Contains('/') ? package[..(package.LastIndexOf('/')+1)] : "";
            var items = opf.Descendants().Where(e=>e.Name.LocalName=="item").ToDictionary(e=>e.Attribute("id")!.Value,e=>e.Attribute("href")!.Value);
            var result = new StringBuilder();
            foreach (var reference in opf.Descendants().Where(e=>e.Name.LocalName=="itemref"))
            {
                string href = items[reference.Attribute("idref")!.Value];
                string full = Uri.UnescapeDataString(folder + href.Split('#')[0]);
                if (full.Contains("..") || full.Contains(':') || full.StartsWith('/')) throw new InvalidDataException("Unsafe EPUB resource path.");
                result.AppendLine(HtmlText(ReadEntry(zip, full))); if (result.Length > 20_000_000) throw new InvalidDataException("EPUB text exceeds budget.");
            }
            return result.ToString();
        }
        if (ext is "txt" or "md" or "markdown" or "html" or "htm")
        {
            using var reader=new StreamReader(input);var builder=new StringBuilder();var buffer=new char[65536];int read;
            while((read=reader.Read(buffer,0,buffer.Length))>0){if(builder.Length+read>20_000_000)throw new InvalidDataException("Document text exceeds 20 million characters.");builder.Append(buffer,0,read);}
            string text = builder.ToString(); return ext is "html" or "htm" ? HtmlText(text) : text;
        }
        throw new InvalidOperationException("Use the Office engine to import this document format.");
    }
    public static void WriteDocx(IEnumerable<string> pages, string output)
    {
        using var doc = WordprocessingDocument.Create(output, WordprocessingDocumentType.Document);
        var part = doc.AddMainDocumentPart(); var body = new W.Body(); part.Document = new W.Document(body);
        bool first = true;
        foreach (string page in pages) { if (!first) body.Append(new W.Paragraph(new W.Run(new W.Break { Type = W.BreakValues.Page }))); foreach (string line in page.Split('\n')) body.Append(new W.Paragraph(new W.Run(new W.Text(line) { Space = SpaceProcessingModeValues.Preserve }))); first = false; }
        part.Document.Save();
    }
    public static void WriteXlsx(IReadOnlyList<string> pages, string output)
    {
        using var doc = SpreadsheetDocument.Create(output, SpreadsheetDocumentType.Workbook);
        var book = doc.AddWorkbookPart(); book.Workbook = new S.Workbook(); var sheet = book.AddNewPart<WorksheetPart>(); var data = new S.SheetData(); sheet.Worksheet = new S.Worksheet(data);
        data.Append(new S.Row(Cell("Page"),Cell("Text")));
        int rowCount=1;
        for (int i=0;i<pages.Count;i++) foreach (string line in pages[i].Split('\n')) { if(++rowCount>1_048_576)throw new InvalidDataException("PDF text exceeds the spreadsheet row limit.");data.Append(new S.Row(Cell((i+1).ToString()),Cell(line.Length>32767?line[..32767]:line))); }
        book.Workbook.Append(new S.Sheets(new S.Sheet { Id=book.GetIdOfPart(sheet),SheetId=1,Name="PDF text" })); book.Workbook.Save();
        static S.Cell Cell(string value) => new() { DataType=S.CellValues.InlineString,InlineString=new S.InlineString(new S.Text(value){Space=SpaceProcessingModeValues.Preserve}) };
    }
    public static void WriteEpub(string text, string output, string title)
    {
        using var zip = ZipFile.Open(output, ZipArchiveMode.Create);
        Add("mimetype","application/epub+zip",CompressionLevel.NoCompression);
        Add("META-INF/container.xml","<?xml version=\"1.0\"?><container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\"><rootfiles><rootfile full-path=\"content.opf\" media-type=\"application/oebps-package+xml\"/></rootfiles></container>");
        string id=Guid.NewGuid().ToString(); string safe=WebUtility.HtmlEncode(title);
        Add("content.opf",$"<?xml version=\"1.0\"?><package xmlns=\"http://www.idpf.org/2007/opf\" version=\"3.0\" unique-identifier=\"bookid\"><metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\"><dc:identifier id=\"bookid\">urn:uuid:{id}</dc:identifier><dc:title>{safe}</dc:title><dc:language>en</dc:language><meta property=\"dcterms:modified\">{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</meta></metadata><manifest><item id=\"chapter\" href=\"chapter.xhtml\" media-type=\"application/xhtml+xml\"/><item id=\"nav\" href=\"nav.xhtml\" properties=\"nav\" media-type=\"application/xhtml+xml\"/></manifest><spine><itemref idref=\"chapter\"/></spine></package>");
        Add("chapter.xhtml",$"<?xml version=\"1.0\"?><html xmlns=\"http://www.w3.org/1999/xhtml\"><head><title>{safe}</title></head><body><h1>{safe}</h1>"+string.Join("",text.Split('\n').Select(l=>"<p>"+WebUtility.HtmlEncode(l)+"</p>"))+"</body></html>");
        Add("nav.xhtml",$"<?xml version=\"1.0\"?><html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\"><head><title>Contents</title></head><body><nav epub:type=\"toc\"><ol><li><a href=\"chapter.xhtml\">{safe}</a></li></ol></nav></body></html>");
        void Add(string name,string contents,CompressionLevel compression=CompressionLevel.Optimal) { var entry=zip.CreateEntry(name,compression); using var writer=new StreamWriter(entry.Open(),new UTF8Encoding(false));writer.Write(contents); }
    }
    public static async Task<string> OfficeAsync(string input,string format,string directory,CancellationToken token)
    {
        string engine=EnginePaths.Office??throw new InvalidOperationException("The local LibreOffice engine is not configured.");
        if(new FileInfo(input).Length>256L*1024*1024)throw new InvalidDataException("Office input exceeds 256 MB.");
        // LibreOffice's profile includes deeply nested registry/cache paths. Keep it
        // in a short private temp path, independent of the user's output folder.
        string work=Path.Combine(Path.GetTempPath(),"mo-"+Guid.NewGuid().ToString("N")[..16]);
        Directory.CreateDirectory(work);
        try
        {
            string source=Path.Combine(work,"input."+ConversionService.Extension(input));File.Copy(input,source);
            string profile=Path.Combine(work,"profile");Directory.CreateDirectory(Path.Combine(profile,"user"));
            File.WriteAllText(Path.Combine(profile,"user","registrymodifications.xcu"),"<?xml version=\"1.0\"?><oor:items xmlns:oor=\"http://openoffice.org/2001/registry\"><item oor:path=\"/org.openoffice.Office.Common/Security/Scripting\"><prop oor:name=\"MacroSecurityLevel\" oor:op=\"fuse\"><value>3</value></prop></item></oor:items>");
            string target=Path.Combine(work,"out");Directory.CreateDirectory(target);
            string filter=format=="tsv"?"csv:Text - txt - csv (StarCalc):9,34,76":format;
            using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromMinutes(5));
            string diagnostics=await ConversionService.RunAsync(engine,new[]{"-env:UserInstallation="+new Uri(profile+Path.DirectorySeparatorChar).AbsoluteUri,"--headless","--nologo","--nodefault","--nolockcheck","--norestore","--convert-to",filter,"--outdir",target,source},timeout.Token);
            string? result=Directory.EnumerateFiles(target).FirstOrDefault();
            if(result==null||new FileInfo(result).Length==0)throw new InvalidDataException("The Office engine could not export this file. "+diagnostics.Trim());
            string staged=Path.Combine(directory,Path.GetFileNameWithoutExtension(input)+"."+format);File.Copy(result,staged,false);return staged;
        }
        finally
        {
            for(int attempt=0;attempt<8;attempt++)
            {
                try{if(Directory.Exists(work))Directory.Delete(work,true);break;}
                catch(IOException)when(attempt<7){await Task.Delay(250);}
            }
        }
    }
    public static async Task<IReadOnlyList<string>> ConvertAsync(string input,string format,string directory,CancellationToken token)
    {
        string ext=ConversionService.Extension(input),output=Path.Combine(directory,"result."+format);
        if(ext is "ppt" or "pptx" or "xlsx" or "xls" or "ods" or "csv" or "tsv")
        {
            string officeFormat=format is "jpg" or "png"?"pdf":format;
            string converted=await OfficeAsync(input,officeFormat,directory,token);
            return format is "jpg" or "png"?await PdfStudio.RenderAsync(converted,directory,format,token):new[]{converted};
        }
        if((ext is "doc" or "rtf" or "odt") || (ext=="docx" && format is "pdf" or "jpg" or "png" && EnginePaths.Office!=null))
        {
            if (format is "md" or "epub" or "html")
            {
                string imported=await OfficeAsync(input,"docx",directory,token);
                return await ConvertAsync(imported,format,directory,token);
            }
            string converted=await OfficeAsync(input,format is "jpg" or "png"?"pdf":format,directory,token);
            return format is "jpg" or "png"?await PdfStudio.RenderAsync(converted,directory,format,token):new[]{converted};
        }
        if(format is "jpg" or "png")
        {
            string pdf=Path.Combine(directory,"document.pdf");PdfStudio.TextToPdf(Text(input),pdf,Path.GetFileNameWithoutExtension(input));return await PdfStudio.RenderAsync(pdf,directory,format,token);
        }
        var pages=ext=="pdf"?PdfStudio.TextPages(input):new[]{Text(input)};string text=string.Join("\n\n",pages);
        switch(format)
        {
            case "docx":WriteDocx(pages,output);break;
            case "xlsx":WriteXlsx(pages,output);break;
            case "pdf":PdfStudio.TextToPdf(text,output,Path.GetFileNameWithoutExtension(input));break;
            case "epub":WriteEpub(text,output,Path.GetFileNameWithoutExtension(input));break;
            case "html":await File.WriteAllTextAsync(output,"<!doctype html><meta charset=\"utf-8\"><title>"+WebUtility.HtmlEncode(Path.GetFileNameWithoutExtension(input))+"</title><main><pre style=\"white-space:pre-wrap\">"+WebUtility.HtmlEncode(text)+"</pre></main>",token);break;
            case "txt":case "md":await File.WriteAllTextAsync(output,text,new UTF8Encoding(false),token);break;
            case "pptx":
                var images=await PdfStudio.RenderAsync(input,directory,"png",token);WritePptx(images,output);break;
            default:throw new InvalidOperationException("Unsupported document output.");
        }
        return new[]{output};
    }
    private static XDocument SafeXml(string xml) { using var reader=XmlReader.Create(new StringReader(xml),new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=20_000_000});return XDocument.Load(reader); }
    private static void CheckZip(ZipArchive zip) { if(zip.Entries.Count>10000||zip.Entries.Sum(e=>e.Length)>512L*1024*1024)throw new InvalidDataException("Document package exceeds resource limits."); }
    private static string ReadEntry(ZipArchive zip,string name) { var entry=zip.GetEntry(name)??throw new InvalidDataException("Missing document resource.");if(entry.Length>20_000_000)throw new InvalidDataException("Document resource too large.");using var reader=new StreamReader(entry.Open());return reader.ReadToEnd(); }
    public static void WritePptx(IReadOnlyList<string> images,string output)
    {
        using var document=PresentationDocument.Create(output,PresentationDocumentType.Presentation);
        var presentation=document.AddPresentationPart();
        var ids=new DocumentFormat.OpenXml.Presentation.SlideIdList();
        presentation.Presentation=new DocumentFormat.OpenXml.Presentation.Presentation(ids,new DocumentFormat.OpenXml.Presentation.SlideSize{Cx=9144000,Cy=11887200});
        for(int i=0;i<images.Count;i++)
        {
            using var dimensions=ImageStudio.Read(images[i]);
            double scale=Math.Min(9144000d/dimensions.Width,11887200d/dimensions.Height);
            long cx=(long)(dimensions.Width*scale),cy=(long)(dimensions.Height*scale),x=(9144000-cx)/2,y=(11887200-cy)/2;
            var slide=presentation.AddNewPart<SlidePart>();var image=slide.AddImagePart(ImagePartType.Png);using(var stream=File.OpenRead(images[i]))image.FeedData(stream);
            string rid=slide.GetIdOfPart(image);
            slide.Slide=new DocumentFormat.OpenXml.Presentation.Slide();
            slide.Slide.InnerXml=$"<p:cSld xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><p:spTree><p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr/><p:pic><p:nvPicPr><p:cNvPr id=\"2\" name=\"PDF page {i+1}\"/><p:cNvPicPr/><p:nvPr/></p:nvPicPr><p:blipFill><a:blip r:embed=\"{rid}\"/><a:stretch><a:fillRect/></a:stretch></p:blipFill><p:spPr><a:xfrm><a:off x=\"{x}\" y=\"{y}\"/><a:ext cx=\"{cx}\" cy=\"{cy}\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr></p:pic></p:spTree></p:cSld>";
            slide.Slide.Save();ids.Append(new DocumentFormat.OpenXml.Presentation.SlideId{Id=(uint)(256+i),RelationshipId=presentation.GetIdOfPart(slide)});
        }
        presentation.Presentation.Save();
    }
}
