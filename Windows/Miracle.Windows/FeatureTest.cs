using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using ImageMagick.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using DocumentFormat.OpenXml.Packaging;
namespace Miracle.Windows;
internal static class FeatureTest
{
    public static async Task<int> EngineSmokeAsync(string kind,string fixtures)
    {
        string output=Path.Combine(fixtures,"smoke-"+kind+"-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(output);
        var service=new ConversionService();await service.DetectAsync();
        if(kind=="images")foreach(string image in Directory.GetFiles(fixtures,"image.*"))for(int i=0;i<3;i++)await ImageStudio.ConvertAsync(image,Path.Combine(output,Guid.NewGuid()+".png"),"png",CancellationToken.None);
        else if(kind=="pdf")for(int i=0;i<3;i++)await PdfStudio.RenderAsync(Path.Combine(fixtures,"document.pdf"),output,"png",CancellationToken.None);
        else if(kind=="gif-tools")
        {
            string gif=Directory.GetFiles(fixtures,"*.gif").First(p=>Path.GetFileName(p).StartsWith("clip-"));
            var checks=new List<string>();
            foreach(var tool in FileTools.For(gif).Where(t=>t.Id.StartsWith("video.")))
            {
                var fields=tool.Fields.ToDictionary(f=>f.Key,f=>f.Default);fields["start"]="0.25";fields["end"]="1.25";fields["seconds"]="0.75";fields["width"]="32";fields["height"]="32";
                var paths=await FileTools.ApplyAsync(new[]{gif},tool,new ToolValues(fields),output,CancellationToken.None);
                if(paths.Count==0||paths.Any(p=>new FileInfo(p).Length==0))throw new InvalidDataException("GIF tool produced no output.");
                if(tool.Id=="video.snapshot"){using var image=ImageStudio.Read(paths[0]);if(image.Width==0)throw new InvalidDataException("Invalid GIF snapshot.");}
                else{using var probe=await MediaStudio.ProbeAsync(paths[0],CancellationToken.None);if(!probe.RootElement.GetProperty("streams").EnumerateArray().Any(s=>s.GetProperty("codec_type").GetString()=="video"))throw new InvalidDataException("GIF tool lost video stream.");}
                checks.Add(tool.Id);
            }
            await File.WriteAllTextAsync(Path.Combine(fixtures,"gif-tools-report.json"),JsonSerializer.Serialize(new{passed=true,checks},new JsonSerializerOptions{WriteIndented=true}));
        }
        else if(kind=="docx-images")
        {
            string source=Path.Combine(fixtures,"document.docx");
            foreach(string format in new[]{"jpg","png"})
            {
                var paths=await service.ConvertAllAsync(source,format,output,CancellationToken.None);
                if(paths.Count!=2)throw new InvalidDataException("DOCX page breaks were not preserved.");
                foreach(string path in paths){using var image=ImageStudio.Read(path);if(image.Width<2400)throw new InvalidDataException("DOCX image export is not 300 DPI.");}
            }
            await File.WriteAllTextAsync(Path.Combine(fixtures,"docx-images-report.json"),JsonSerializer.Serialize(new{passed=true,checks=new[]{"Two-page DOCX → all JPG pages at 300 DPI","Two-page DOCX → all PNG pages at 300 DPI"}},new JsonSerializerOptions{WriteIndented=true}));
        }
        else if(kind=="office-catalog")
        {
            var checks=new List<string>();
            var inputs=Directory.GetFiles(fixtures,"*",SearchOption.AllDirectories).Where(p=>ConversionService.Category(p) is "slides" or "sheet" || ConversionService.Extension(p)=="rtf" || p==Path.Combine(fixtures,"document.docx")).Distinct().ToArray();
            foreach(string input in inputs)
            foreach(string format in service.Outputs(input).Where(f=>ConversionService.Extension(input)!="docx" || f is "pdf" or "png" or "jpg"))
            {
                var paths=await service.ConvertAllAsync(input,format,output,CancellationToken.None);
                if(paths.Count==0||paths.Any(p=>new FileInfo(p).Length==0))throw new InvalidDataException("Missing Office output.");
                if(format=="pdf")foreach(string path in paths){using var pdf=PdfReader.Open(path,PdfDocumentOpenMode.Import);PdfStudio.Validate(pdf);}
                if(format is "png" or "jpg")foreach(string path in paths){using var image=ImageStudio.Read(path);if(image.Width<2400)throw new InvalidDataException("Office page export is not 300 DPI.");}
                checks.Add(Path.GetFileName(input)+" → "+format);
            }
            await File.WriteAllTextAsync(Path.Combine(fixtures,"office-catalog-report.json"),JsonSerializer.Serialize(new{passed=true,checks},new JsonSerializerOptions{WriteIndented=true}));
        }
        else if(kind=="documents")for(int i=0;i<3;i++)PdfStudio.TextToPdf("Miracle diagnostic",Path.Combine(output,i+".pdf"),"Diagnostic");
        else throw new ArgumentException("Unknown engine test.");
        return 0;
    }
    public static async Task<int> RunAsync(string directory)
    {
        Directory.CreateDirectory(directory);string fixtures=Path.Combine(directory,"fixtures"),results=Path.Combine(directory,"results");Directory.CreateDirectory(fixtures);Directory.CreateDirectory(results);
        var checks=new List<object>();int failures=0;
        async Task Check(string name,Func<Task> action)
        {
            try{await action();checks.Add(new{name,status="PASS"});}
            catch(Exception ex){failures++;checks.Add(new{name,status="FAIL",error=ex.ToString()});}
            await File.WriteAllTextAsync(Path.Combine(directory,"feature-report.json"),JsonSerializer.Serialize(new{complete=false,passed=false,failures,checks},new JsonSerializerOptions{WriteIndented=true}));
        }
        var token=CancellationToken.None;var service=new ConversionService();
        await service.DetectAsync();
        var images=new List<string>();
        await Check("Create image fixtures and all image encoders",async()=>
        {
            ImageStudio.Initialize();
            using var image=new MagickImage(MagickColors.Transparent,96,64);new Drawables().FillColor(MagickColors.Coral).Rectangle(10,10,85,53).Draw(image);
            foreach(string format in new[]{"jpg","png","webp","heic","tiff","avif","bmp","gif"})
            {string path=Path.Combine(fixtures,"image."+format);if(format=="heic")await ImageStudio.ConvertAsync(images.First(p=>p.EndsWith(".png")),path,format,token);else image.Write(path,ImageStudio.Format(format));images.Add(path);}
            string svg=Path.Combine(fixtures,"image.svg");await File.WriteAllTextAsync(svg,"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"96\" height=\"64\"><rect width=\"96\" height=\"64\" fill=\"coral\"/></svg>");images.Add(svg);
        });
        var documents=new List<string>();string pdf=Path.Combine(fixtures,"document.pdf");
        await Check("Create two-page PDF, DOCX, EPUB and text fixtures",async()=>
        {
            string text="Miracle offline conversion.\nOriginal content survives.";
            string txt=Path.Combine(fixtures,"document.txt"),md=Path.Combine(fixtures,"document.md"),html=Path.Combine(fixtures,"document.html"),docx=Path.Combine(fixtures,"document.docx"),epub=Path.Combine(fixtures,"document.epub");
            await File.WriteAllTextAsync(txt,text);await File.WriteAllTextAsync(md,text);await File.WriteAllTextAsync(html,"<html><body><p>"+text+"</p></body></html>");DocumentStudio.WriteDocx(new[]{text,"Second page of Miracle."},docx);DocumentStudio.WriteEpub(text,epub,"Miracle");
            string first=Path.Combine(fixtures,"page.pdf");PdfStudio.TextToPdf(text,first,"Miracle");using(var one=PdfReader.Open(first,PdfDocumentOpenMode.Import))using(var two=new PdfDocument()){two.AddPage(one.Pages[0]);two.AddPage(one.Pages[0]);two.Save(pdf);}
            documents.AddRange(new[]{txt,md,html,docx,epub,pdf});
        });
        var office=new List<string>();
        await Check("Create Office input fixtures through local LibreOffice",async()=>
        {
            if(EnginePaths.Office==null)throw new InvalidOperationException("LibreOffice not bundled.");
            string sheet=Path.Combine(fixtures,"sheet.xlsx");DocumentStudio.WriteXlsx(new[]{"Miracle\n42"},sheet);office.Add(sheet);
            foreach(string format in new[]{"ods","csv","tsv"}){string folder=Path.Combine(fixtures,"make-"+format);Directory.CreateDirectory(folder);office.Add(await DocumentStudio.OfficeAsync(sheet,format,folder,token));}
            string slide=Path.Combine(fixtures,"slides.pptx");var png=images.First(p=>p.EndsWith(".png"));DocumentStudio.WritePptx(new[]{png,png},slide);office.Add(slide);
            foreach(var item in new[]{("docx","rtf"),("xlsx","xls"),("pptx","ppt")})
            {string source=item.Item1=="docx"?documents.First(p=>p.EndsWith(".docx")):item.Item1=="xlsx"?sheet:slide;string folder=Path.Combine(fixtures,"make-"+item.Item2);Directory.CreateDirectory(folder);office.Add(await DocumentStudio.OfficeAsync(source,item.Item2,folder,token));}
        });
        var media=new List<string>();
        await Check("Generate audio and video fixtures with bundled FFmpeg",async()=>
        {
            if(EnginePaths.Media==null)throw new InvalidOperationException("FFmpeg not bundled.");
            string wav=Path.Combine(fixtures,"tone.wav"),mp4=Path.Combine(fixtures,"clip.mp4");
            await ConversionService.RunAsync(EnginePaths.Media,new[]{"-v","error","-nostdin","-y","-f","lavfi","-i","sine=frequency=440:duration=2","-c:a","pcm_s16le",wav},token);
            await ConversionService.RunAsync(EnginePaths.Media,new[]{"-v","error","-nostdin","-y","-f","lavfi","-i","color=c=coral:s=96x64:r=12:d=2","-f","lavfi","-i","sine=frequency=440:duration=2","-c:v","libx264","-c:a","aac","-pix_fmt","yuv420p","-shortest",mp4},token);
            media.AddRange(new[]{wav,mp4});
            foreach(string format in service.Outputs(wav)){string output=await service.ConvertAsync(wav,format,fixtures,token);media.Add(output);}
            foreach(string format in service.Outputs(mp4).Where(x=>x!="mp3")){string output=await service.ConvertAsync(mp4,format,fixtures,token);media.Add(output);}
        });
        string zip=Path.Combine(fixtures,"archive.zip");
        var archives=new List<string>();
        await Check("Create ZIP, TAR and GZIP fixtures",async()=>
        {
            using(var archive=ZipFile.Open(zip,ZipArchiveMode.Create)){var entry=archive.CreateEntry("folder/hello.txt");using var writer=new StreamWriter(entry.Open());writer.Write("Miracle archive content");}
            string rar=Path.Combine(fixtures,"archive.rar");File.Copy(Path.Combine(AppContext.BaseDirectory,"TestFixtures","archive-stored.rar"),rar);archives.Add(rar);
            archives.Add(zip);foreach(string format in new[]{"tar","gzip"})archives.Add(await service.ConvertAsync(zip,format,fixtures,token));
        });
        foreach(string input in images.Concat(documents).Concat(office).Concat(media).Concat(archives).Distinct())
        foreach(string format in service.Outputs(input))
        await Check(Path.GetFileName(input)+" → "+format,async()=>
        {
            var outputs=await service.ConvertAllAsync(input,format,results,token);
            if(outputs.Count==0||outputs.Any(p=>new FileInfo(p).Length==0))throw new InvalidDataException("No output.");
            foreach(string output in outputs)
            {
                if(format is "png" or "jpg" or "webp" or "heic" or "avif" or "bmp" or "tiff") {using var image=ImageStudio.Read(output);if(image.Width==0||image.Height==0)throw new InvalidDataException("Invalid image.");}
                else if(format=="pdf"){using var doc=PdfReader.Open(output,PdfDocumentOpenMode.Import);PdfStudio.Validate(doc);}
                else if(format=="docx"){using var doc=WordprocessingDocument.Open(output,false);if(doc.MainDocumentPart?.Document?.Body==null)throw new InvalidDataException("Invalid DOCX.");}
                else if(format=="pptx"){using var doc=PresentationDocument.Open(output,false);if(doc.PresentationPart?.SlideParts.Count()!=2)throw new InvalidDataException("PDF page count lost.");}
                else if(format=="xlsx"){using var doc=SpreadsheetDocument.Open(output,false);if(doc.WorkbookPart==null)throw new InvalidDataException("Invalid workbook.");}
                else if(format=="epub"){if(!DocumentStudio.Text(output).Contains("Miracle"))throw new InvalidDataException("EPUB lost content.");}
                else if(ConversionService.Category(output) is "audio" or "video"){using var probe=await MediaStudio.ProbeAsync(output,token);if(!probe.RootElement.GetProperty("streams").EnumerateArray().Any())throw new InvalidDataException("No media stream.");}
                else if(ConversionService.Category(output)=="archive"){using var job=new OutputJob(output,results,"verify");await ArchiveStudio.ExtractAsync(output,job.Stage,token);}
            }
            if(input==pdf && format is "jpg" or "png") {if(outputs.Count!=2)throw new InvalidDataException("Not all PDF pages rendered.");using var image=ImageStudio.Read(outputs[0]);if(image.Width<2400)throw new InvalidDataException("Not 300 DPI.");}
        });
        foreach(string input in new[]{images.FirstOrDefault(p=>p.EndsWith(".png")),media.FirstOrDefault(p=>p.EndsWith(".mp4")),media.FirstOrDefault(p=>p.EndsWith(".wav")),pdf,zip}.OfType<string>())
        foreach(var tool in FileTools.For(input))
        await Check(tool.Id,async()=>
        {
            var data=tool.Fields.ToDictionary(f=>f.Key,f=>f.Default);data["width"]="32";data["height"]="32";data["text"]="Miracle";data["x"]="1";data["y"]="32";data["size"]="12";data["start"]="0.25";data["end"]="1.25";data["seconds"]="0.75";data["brightness"]="15";data["title"]="Miracle metadata";data["author"]="Miracle";data["pages"]="2,1,2";data["rotate"]="90";
            if(tool.Id is "image.crop" or "image.redact" or "video.crop" or "video.redact")data["y"]="1";
            var outputs=await FileTools.ApplyAsync(new[]{input},tool,new ToolValues(data),results,token);
            if(outputs.Count==0 || outputs.Any(p=>!File.Exists(p)&&!Directory.Exists(p)))throw new InvalidDataException("Missing tool output.");
            if(tool.Id=="archive.extract"&&!File.ReadAllText(Path.Combine(outputs[0],"folder","hello.txt")).Contains("Miracle"))throw new InvalidDataException("Archive content lost.");
            if(tool.Id=="image.crop"){using var image=ImageStudio.Read(outputs[0]);if(image.Width!=32||image.Height!=32)throw new InvalidDataException("Crop dimensions wrong.");}
            if(tool.Id=="image.redact"){using var image=ImageStudio.Read(outputs[0]);var pixel=image.GetPixels().GetPixel(5,5).ToColor();if(pixel==null||pixel.R!=0||pixel.G!=0||pixel.B!=0)throw new InvalidDataException("Redacted pixel not black.");}
            if(tool.Id=="pdf.split"&&outputs.Count!=2)throw new InvalidDataException("Not all PDF pages split.");
            if(tool.Id=="pdf.organize"){using var doc=PdfReader.Open(outputs[0],PdfDocumentOpenMode.Import);if(doc.PageCount!=3||doc.Pages[0].Rotate!=90)throw new InvalidDataException("Page order or rotation wrong.");}
            if(tool.Id=="video.split"&&outputs.Count<2)throw new InvalidDataException("Video not split.");
            if(tool.Id=="image.edit"){using var image=ImageStudio.Read(outputs[0]);if(image.Width!=64||image.Height!=96)throw new InvalidDataException("Image rotation did not apply.");}
            if(tool.Id=="image.metadata"){using var image=ImageStudio.Read(outputs[0]);if(image.GetExifProfile()?.GetValue(ExifTag.Artist)?.Value!="Miracle")throw new InvalidDataException("Image metadata not saved.");}
            if(tool.Id=="image.background"){using var image=ImageStudio.Read(outputs[0]);if(image.HasAlpha)throw new InvalidDataException("Transparency was not flattened.");}
            if(tool.Id=="pdf.metadata"&&!PdfStudio.Describe(outputs[0]).Contains("Miracle metadata"))throw new InvalidDataException("PDF metadata not saved.");
            if(tool.Id.StartsWith("audio.")||tool.Id.StartsWith("video."))
            {
                if(ConversionService.Category(outputs[0]) is "audio" or "video")
                {
                    using var probe=await MediaStudio.ProbeAsync(outputs[0],token);var streams=probe.RootElement.GetProperty("streams").EnumerateArray().ToArray();
                    if(tool.Id=="video.mute"&&streams.Any(s=>s.GetProperty("codec_type").GetString()=="audio"))throw new InvalidDataException("Mute retained an audio track.");
                    if(tool.Id=="audio.channels"&&!streams.Any(s=>s.GetProperty("codec_type").GetString()=="audio"&&s.GetProperty("channels").GetInt32()==2))throw new InvalidDataException("Stereo conversion failed.");
                    if(tool.Id is "video.trim" or "audio.trim" or "video.speed")
                    {double duration=double.Parse(probe.RootElement.GetProperty("format").GetProperty("duration").GetString()!,System.Globalization.CultureInfo.InvariantCulture);if(Math.Abs(duration-1)>0.3)throw new InvalidDataException("Output duration is incorrect: "+duration);}
                    if(tool.Id=="video.crop"&&!streams.Any(s=>s.GetProperty("codec_type").GetString()=="video"&&s.GetProperty("width").GetInt32()==32&&s.GetProperty("height").GetInt32()==32))throw new InvalidDataException("Video crop dimensions are wrong.");
                }
                else {using var image=ImageStudio.Read(outputs[0]);if(image.Width==0)throw new InvalidDataException("Invalid PNG tool output.");}
            }
        });
        await File.WriteAllTextAsync(Path.Combine(directory,"feature-report.json"),JsonSerializer.Serialize(new{complete=true,passed=failures==0,failures,checks},new JsonSerializerOptions{WriteIndented=true}));
        return failures==0?0:1;
    }
}
