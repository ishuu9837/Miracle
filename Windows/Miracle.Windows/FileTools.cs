using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Miracle.Windows;
internal record ToolField(string Key,string Label,string Default="");
internal record FileTool(string Id,string Name,string Description,params ToolField[] Fields);
internal static class FileTools
{
    private static ToolField F(string key,string label,string value="")=>new(key,label,value);
    private static readonly ToolField[] Rectangle={F("x","Left (pixels)","0"),F("y","Top (pixels)","0"),F("width","Width (blank = remaining)"),F("height","Height (blank = remaining)")};
    private static readonly ToolField[] Trim={F("start","Start (seconds)","0"),F("end","End (blank = duration)")};
    private static readonly ToolField[] MediaTags={F("title","Title"),F("artist","Artist"),F("album","Album"),F("comment","Comment"),F("clear","Remove existing metadata (true / false)","false")};
    internal static IReadOnlyList<FileTool> For(string input)=>ConversionService.Extension(input)=="gif"?For("image.png").Concat(For("video.mp4")).ToArray():ConversionService.Category(input) switch
    {
        "image"=>new[]{
            new FileTool("image.compress","Compress","JPEG export; transparency becomes a solid background.",F("quality","JPEG quality (1–100)","75")),
            new FileTool("image.metadata","Metadata","Read metadata below. Save a new PNG with updated fields; blank fields clear those tags.",F("author","Author"),F("description","Description"),F("copyright","Copyright"),F("clear","Remove existing metadata (true / false)","false")),
            new FileTool("image.edit","Edit","Adjust and rotate; saves PNG.",F("brightness","Brightness (-100 to 100)","0"),F("contrast","Contrast (-100 to 100)","0"),F("saturation","Saturation (0–300)","100"),F("rotate","Rotation (degrees)","0")),
            new FileTool("image.annotate","Annotate","Add text to pixels; saves PNG.",F("text","Text"),F("x","Left","20"),F("y","Baseline","40"),F("size","Font size","28"),F("color","Color (#RRGGBB)","#000000")),
            new FileTool("image.background","Add BG","Flatten transparency onto a color.",F("color","Background (#RRGGBB)","#FFFFFF")),
            new FileTool("image.crop","Crop","Crop to a pixel rectangle.",Rectangle),new FileTool("image.redact","Redact","Permanently replace a rectangle with black pixels and remove metadata.",Rectangle)},
        "video"=>new[]{
            new FileTool("video.compress","Compress","Re-encode as MP4. Higher CRF = smaller, lower quality.",F("quality","CRF (18–45)","28")),
            new FileTool("video.metadata","Metadata","Read below; save MP4 with updated tags. Blank fields clear those tags.",MediaTags),new FileTool("video.mute","Mute","Save an MP4 without audio."),
            new FileTool("video.trim","Trim","Save the selected time interval as MP4.",Trim),new FileTool("video.crop","Crop","Crop to even pixel dimensions for MP4.",Rectangle),
            new FileTool("video.speed","Speed","Change video and audio speed.",F("speed","Speed multiplier (0.25–4)","2")),
            new FileTool("video.snapshot","Snapshot","Export one frame as PNG.",F("time","Time (seconds)","0")),new FileTool("video.split","Split","Split into MP4 parts at this interval.",F("seconds","Seconds per part","30")),new FileTool("video.redact","Redact","Permanently mask this rectangle across every frame; audio stays.",Rectangle)},
        "audio"=>new[]{
            new FileTool("audio.compress","Compress","Export compressed MP3.",F("bitrate","Bitrate (32–320 kbps)","128")),new FileTool("audio.metadata","Metadata","Read below; save WAV with updated tags. Blank fields clear those tags.",MediaTags),
            new FileTool("audio.normalize","Normalize","Normalize loudness; export WAV.",F("loudness","Target LUFS (-30 to -5)","-16")),new FileTool("audio.visualize","Visualize","Export a waveform PNG."),
            new FileTool("audio.trim","Trim","Save the time interval as WAV.",Trim),new FileTool("audio.channels","Channels","Export mono or stereo WAV.",F("channels","Channels (1 or 2)","2")),new FileTool("audio.bleep","Bleep","Replace this interval with a tone; export WAV.",F("start","Start (seconds)","0"),F("end","End (seconds)","1"),F("frequency","Tone frequency (Hz)","1000"))},
        "pdf"=>new[]{new FileTool("pdf.compress","Compress","Lossless PDF stream compression. Size reduction depends on the source."),new FileTool("pdf.metadata","Metadata","Read below; update PDF information.",F("title","Title"),F("author","Author"),F("description","Subject"),F("keywords","Keywords")),new FileTool("pdf.split","Split","Export every page as a separate PDF."),new FileTool("pdf.organize","Organize","Reorder / duplicate / remove pages using a list; rotate selected pages.",F("pages","Pages (blank = all; e.g. 3,1,2-4)"),F("rotate","Rotation (0, 90, 180, 270)","0"),F("merge","Merge selected PDFs in selection order (true / false)","false"))},
        "archive"=>new[]{new FileTool("archive.extract","Extract Archive","Extract safely into a new folder. Existing files stay untouched.")},_=>Array.Empty<FileTool>()
    };
    internal static async Task<string> DescribeAsync(string input,CancellationToken token)
    {
        string kind=ConversionService.Category(input);
        if(kind is "audio" or "video")return await MediaStudio.DescribeAsync(input,token);
        return await Task.Run(()=>kind=="image"?ImageStudio.Describe(input):kind=="pdf"?PdfStudio.Describe(input):"",token);
    }
    internal static async Task<IReadOnlyList<string>> ApplyAsync(string[] inputs,FileTool tool,ToolValues values,string? directory,CancellationToken token)
    {
        if(inputs.Length==0||inputs.Any(i=>!For(i).Any(t=>t.Id==tool.Id)))throw new InvalidOperationException("Tool unavailable for this selection.");
        var results=new List<string>();
        if(tool.Id=="pdf.organize"&&values.Flag("merge")&&inputs.Length>1)
        {
            foreach(string input in inputs)ConversionService.ValidateInput(input);
            using var job=new OutputJob(inputs[0],directory,"merged");
            var generated=await Task.Run(()=>PdfStudio.Apply(inputs,job.Stage,tool.Id,values,token),token);
            return job.Commit(generated,token);
        }
        foreach(string input in inputs)
        {
            ConversionService.ValidateInput(input);token.ThrowIfCancellationRequested();
            using var job=new OutputJob(input,directory,tool.Id.Split('.')[1]);IReadOnlyList<string> generated;
            if(tool.Id.StartsWith("image."))generated=new[]{await ImageStudio.ApplyAsync(input,job.File(tool.Id=="image.compress"?"jpg":"png"),tool.Id,values,token)};
            else if(tool.Id.StartsWith("pdf."))generated=await Task.Run(()=>PdfStudio.Apply(new[]{input},job.Stage,tool.Id,values,token),token);
            else if(tool.Id=="archive.extract") { string root=await ArchiveStudio.ExtractAsync(input,job.Stage,token);results.Add(job.CommitDirectory(root,token));continue; }
            else generated=await MediaStudio.ApplyAsync(input,job.Stage,tool.Id,values,token);
            results.AddRange(job.Commit(generated,token));
        }
        return results;
    }
}
