using System;
using System.IO;
using System.Linq;
namespace Miracle.Windows;
internal static class EnginePaths
{
    public static string? Media { get; set; }
    public static string? HeifEncoder => Existing(Path.Combine(AppContext.BaseDirectory,"Engines","Heif","bin","heif-enc.exe"));
    public static string? Probe => Media == null ? null : Existing(Path.Combine(Path.GetDirectoryName(Media)!, "ffprobe.exe"));
    public static string? Office { get; set; } = new[] {
        Path.Combine(AppContext.BaseDirectory,"Engines","LibreOffice","program","soffice.com"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"LibreOffice","program","soffice.com")
    }.Select(Existing).FirstOrDefault(p=>p!=null);
    private static string? Existing(string path) => File.Exists(path) ? Path.GetFullPath(path) : null;
}
