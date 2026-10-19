using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Miracle.Windows;
internal static class FeatureSafety
{
    public static async Task RunAsync(string directory,List<string> checks)
    {
        async Task Reject(string name,Func<Task> operation)
        {
            bool rejected=false;try{await operation();}catch(Exception ex)when(ex is IOException or InvalidDataException or InvalidOperationException or ArgumentException or ImageMagick.MagickException){rejected=true;}
            if(!rejected)throw new InvalidDataException(name+" was accepted.");
            if(Directory.GetDirectories(directory,".miracle-job-*").Any())throw new InvalidDataException(name+" leaked staged output.");checks.Add(name);
        }
        string svg=Path.Combine(directory,"external.svg");await File.WriteAllTextAsync(svg,"<svg xmlns=\"http://www.w3.org/2000/svg\"><image href=\"https://example.invalid/private.png\"/></svg>");
        var service=new ConversionService();await Reject("SVG external resource rejected",async()=>{await service.ConvertAsync(svg,"png",directory,CancellationToken.None);});
        string disguised=Path.Combine(directory,"disguised.png");await File.WriteAllTextAsync(disguised,"push graphic-context\nviewbox 0 0 32 32\nfill red\nrectangle 0,0 31,31\npop graphic-context\n");
        await Reject("Vector input disguised as PNG rejected",async()=>{await service.ConvertAsync(disguised,"jpg",directory,CancellationToken.None);});
        foreach(string name in new[]{"../escape.txt","/absolute.txt","C:/absolute.txt","safe/file.txt:stream","CON.txt","folder/trailing. ","folder/../escape.txt"})
        {
            string zip=Path.Combine(directory,"unsafe-"+Guid.NewGuid().ToString("N")+".zip");using(var archive=ZipFile.Open(zip,ZipArchiveMode.Create)){var entry=archive.CreateEntry(name);using var writer=new StreamWriter(entry.Open());writer.Write("malicious path fixture");}
            var tool=FileTools.For(zip).Single();await Reject("Archive path rejected: "+name,async()=>{await FileTools.ApplyAsync(new[]{zip},tool,new ToolValues(),directory,CancellationToken.None);});
        }
        string duplicate=Path.Combine(directory,"duplicate.zip");using(var archive=ZipFile.Open(duplicate,ZipArchiveMode.Create))foreach(string name in new[]{"same.txt","SAME.TXT"}){using var writer=new StreamWriter(archive.CreateEntry(name).Open());writer.Write("collision");}
        await Reject("Archive case collision rejected",async()=>{await FileTools.ApplyAsync(new[]{duplicate},FileTools.For(duplicate).Single(),new ToolValues(),directory,CancellationToken.None);});
        string link=Path.Combine(directory,"link.tar");using(var file=File.Create(link))using(var writer=new TarWriter(file)){writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink,"link"){LinkName="../escape.txt"});}
        await Reject("Archive symbolic link rejected",async()=>{await FileTools.ApplyAsync(new[]{link},FileTools.For(link).Single(),new ToolValues(),directory,CancellationToken.None);});
        await Reject("Invalid PDF page range rejected",()=>{PdfStudio.ParsePages("1,99",2);return Task.CompletedTask;});
        await Reject("Non-finite tool parameter rejected",()=>{new ToolValues(new Dictionary<string,string>{{"speed","NaN"}}).Number("speed",1,.25,4);return Task.CompletedTask;});
        string txt=Path.Combine(directory,"budget.txt");using(var file=File.Create(txt))file.SetLength(20_000_001);
        await Reject("Document text expansion budget enforced",()=>Task.Run(()=>DocumentStudio.Text(txt)));
    }
}
