using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Miracle.Windows;
internal sealed class OutputJob : IDisposable
{
    public string Stage { get; }
    private readonly string destination, stem;
    public OutputJob(string input, string? directory, string suffix)
    {
        destination = Path.GetFullPath(directory ?? Path.GetDirectoryName(Path.GetFullPath(input))!);
        Directory.CreateDirectory(destination);
        stem = Path.GetFileNameWithoutExtension(input) + "-" + suffix;
        Stage = Path.Combine(destination, ".miracle-job-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(Stage);
    }
    public string File(string ext, string name = "result") => Path.Combine(Stage, name + "." + ext);
    public IReadOnlyList<string> Commit(IEnumerable<string> generated, CancellationToken token)
    {
        var source = generated.ToArray();
        if (source.Length == 0) throw new InvalidDataException("No output was produced.");
        foreach (string path in source) if (!System.IO.File.Exists(path) || new FileInfo(path).Length == 0 || !Path.GetFullPath(path).StartsWith(Stage + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Invalid engine output.");
        var committed = new List<string>();
        try
        {
            for (int i = 0; i < source.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                string name = stem + (source.Length > 1 ? "-" + (i + 1).ToString("D3") : "");
                for (int collision = 0; ; collision++)
                {
                    string target = Path.Combine(destination, name + (collision > 0 ? "-" + collision : "") + (source[i].EndsWith(".tar.gz",StringComparison.OrdinalIgnoreCase)?".tar.gz":Path.GetExtension(source[i])));
                    try { System.IO.File.Move(source[i], target, false); committed.Add(target); break; }
                    catch (IOException) when (System.IO.File.Exists(target)) { }
                }
            }
            return committed;
        }
        catch { foreach (string path in committed) System.IO.File.Delete(path); throw; }
    }
    public string CommitDirectory(string source, CancellationToken token)
    {
        if (!Path.GetFullPath(source).StartsWith(Stage + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(source)) throw new InvalidDataException("Invalid extraction output.");
        token.ThrowIfCancellationRequested();
        for (int collision=0;;collision++)
        {
            string target=Path.Combine(destination,stem+(collision>0?"-"+collision:""));
            try { Directory.Move(source,target); return target; }
            catch(IOException) when(Directory.Exists(target)||System.IO.File.Exists(target)) { }
        }
    }
    public void Dispose() { if (Directory.Exists(Stage)) Directory.Delete(Stage, true); }
}
