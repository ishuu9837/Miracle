using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpCompress.Archives;

namespace Miracle.Windows;
internal static class ArchiveStudio
{
    private const long Budget = 2L * 1024 * 1024 * 1024;
    internal static string SafeName(string key)
    {
        key = key.Replace('\\', '/').TrimEnd('/');
        var parts = key.Split('/');
        if (string.IsNullOrWhiteSpace(key) || Path.IsPathRooted(key) || parts.Any(p => p.Length == 0 || p is "." or ".." || p.EndsWith(' ') || p.EndsWith('.') || p.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || p.Contains(':') || IsReserved(p))) throw new InvalidDataException("Unsafe archive path: " + key);
        return Path.Combine(parts);
    }
    private static bool IsReserved(string part)
    {
        string stem = part.Split('.')[0].ToUpperInvariant();
        return stem is "CON" or "PRN" or "AUX" or "NUL" || (stem.Length == 4 && (stem.StartsWith("COM") || stem.StartsWith("LPT")) && stem[3] is >= '1' and <= '9');
    }
    public static async Task<string> ExtractAsync(string input, string stage, CancellationToken token)
    {
        string root = Path.Combine(stage, "extracted"); Directory.CreateDirectory(root);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); long total = 0; int count = 0;
        async Task Copy(string key,bool folder,bool link,Stream? stream)
        {
            token.ThrowIfCancellationRequested();
            if (++count > 10000) throw new InvalidDataException("Archive exceeds 10,000 entries.");
            if(link)throw new InvalidDataException("Archive links are not supported.");
            string name=SafeName(key);if(!seen.Add(name))throw new InvalidDataException("Duplicate archive path.");
            string path=Path.GetFullPath(Path.Combine(root,name));
            if(!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Archive path escapes destination.");
            if(folder){Directory.CreateDirectory(path);return;}
            if(stream==null)throw new InvalidDataException("Archive entry has no content.");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);using var target=new FileStream(path,FileMode.CreateNew);
            byte[] buffer=new byte[65536];int read;
            while((read=await stream.ReadAsync(buffer,token))>0){total+=read;if(total>Budget)throw new InvalidDataException("Archive exceeds 2 GB extraction budget.");await target.WriteAsync(buffer.AsMemory(0,read),token);}
        }
        string ext=ConversionService.Extension(input);
        if(ext is "tar" or "gz" or "gzip")
        {
            string tarPath=input;
            if(ext!="tar")
            {
                tarPath=Path.Combine(stage,"uncompressed");
                using var compressed=File.OpenRead(input);using var gzip=new GZipStream(compressed,CompressionMode.Decompress);using var destination=File.Create(tarPath);
                byte[] buffer=new byte[65536];long bytes=0;int read;
                while((read=await gzip.ReadAsync(buffer,token))>0){bytes+=read;if(bytes>Budget)throw new InvalidDataException("GZIP exceeds expansion budget.");await destination.WriteAsync(buffer.AsMemory(0,read),token);}
            }
            using var source=File.OpenRead(tarPath);
            bool tar=ext=="tar";
            if(!tar){byte[] header=new byte[512];int length=await source.ReadAsync(header,token);tar=length==512&&System.Text.Encoding.ASCII.GetString(header,257,5)=="ustar";source.Position=0;}
            if(tar)
            {
                using var reader=new TarReader(source,true);TarEntry? entry;
                while((entry=await reader.GetNextEntryAsync(false,token))!=null)
                {bool folder=entry.EntryType==TarEntryType.Directory;bool regular=entry.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile;await Copy(entry.Name,folder,!folder&&!regular,entry.DataStream);}
            }
            else await Copy(Path.GetFileNameWithoutExtension(input),false,false,source);
        }
        else
        {
            using var archive=ArchiveFactory.Open(input);
            foreach(var entry in archive.Entries)
            {
                if(entry.IsEncrypted)throw new InvalidDataException("Encrypted archives are not supported.");
                if(entry.Size>Budget)throw new InvalidDataException("Archive entry exceeds expansion budget.");
                if(entry.IsDirectory)await Copy(entry.Key??"",true,!string.IsNullOrEmpty(entry.LinkTarget),null);
                else{using var source=entry.OpenEntryStream();await Copy(entry.Key??"",false,!string.IsNullOrEmpty(entry.LinkTarget),source);}
            }
        }
        if (count == 0) throw new InvalidDataException("Archive is empty."); return root;
    }
    public static async Task<string> ConvertAsync(string input, string format, string stage, CancellationToken token)
    {
        string root = await ExtractAsync(input, stage, token);
        var extracted=Directory.GetFiles(root);
        if(extracted.Length==1 && Directory.GetDirectories(root).Length==0 && ConversionService.Extension(extracted[0])=="tar" && ConversionService.Extension(input) is "gz" or "gzip")
        { string nested=Path.Combine(stage,"nested");Directory.CreateDirectory(nested);root=await ExtractAsync(extracted[0],nested,token); }
        string output = Path.Combine(stage, "result." + (format == "gzip" ? "tar.gz" : format));
        if (format == "zip")
        {
            using var zip = ZipFile.Open(output, ZipArchiveMode.Create);
            foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            { token.ThrowIfCancellationRequested(); var entry = zip.CreateEntry(Path.GetRelativePath(root, file).Replace('\\','/'), CompressionLevel.Optimal); using var source = File.OpenRead(file); using var target = entry.Open(); await source.CopyToAsync(target, token); }
        }
        else
        {
            using var file = File.Create(output);
            using Stream compressed = format == "gzip" ? new GZipStream(file, CompressionLevel.Optimal, true) : file;
            using var writer = new TarWriter(compressed, TarEntryFormat.Pax, true);
            foreach (string path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) { token.ThrowIfCancellationRequested(); await writer.WriteEntryAsync(path, Path.GetRelativePath(root,path).Replace('\\','/'), token); }
        }
        return output;
    }
}
