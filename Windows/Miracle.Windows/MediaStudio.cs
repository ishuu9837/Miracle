using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
namespace Miracle.Windows;
internal static class MediaStudio
{
    internal static async Task<JsonDocument> ProbeAsync(string input, CancellationToken token)
    {
        if (EnginePaths.Probe == null) throw new InvalidOperationException("ffprobe.exe must be beside ffmpeg.exe.");
        string json = await ConversionService.RunAsync(EnginePaths.Probe, new[] { "-v", "error", "-protocol_whitelist", "file,pipe", "-show_format", "-show_streams", "-of", "json", Path.GetFullPath(input) }, token);
        return JsonDocument.Parse(json);
    }
    internal static async Task<string> DescribeAsync(string input, CancellationToken token)
    { using var probe = await ProbeAsync(input, token); return JsonSerializer.Serialize(probe.RootElement, new JsonSerializerOptions { WriteIndented = true }); }
    internal static async Task<IReadOnlyList<string>> ApplyAsync(string input, string directory, string tool, ToolValues values, CancellationToken token)
    {
        if (EnginePaths.Media == null) throw new InvalidOperationException("Configure the local FFmpeg engine first.");
        bool audio = tool.StartsWith("audio.");
        using var info = await ProbeAsync(input, token);
        double duration = info.RootElement.TryGetProperty("format", out var container) && container.TryGetProperty("duration", out var raw) && double.TryParse(raw.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0;
        if (duration <= 0 || duration > 24*60*60) throw new InvalidDataException("Media must have a known duration under 24 hours.");
        var stream = info.RootElement.GetProperty("streams").EnumerateArray().FirstOrDefault(s => s.TryGetProperty("codec_type", out var t) && t.GetString() == (audio ? "audio" : "video"));
        if (stream.ValueKind == JsonValueKind.Undefined) throw new InvalidDataException("The required media stream is missing.");
        string ext = tool is "video.snapshot" or "audio.visualize" ? "png" : audio ? "wav" : "mp4";
        if (tool == "audio.compress") ext = "mp3";
        var args = new List<string> { "-hide_banner", "-loglevel", "error", "-nostdin", "-n", "-protocol_whitelist", "file,pipe", "-i", Path.GetFullPath(input) };
        string? vf = null, af = null; bool mappedAudio = false;
        double N(string key,double fallback,double min,double max) => values.Number(key,fallback,min,max);
        string F(double number) => ToolValues.Invariant(number);
        string Rectangle(bool redact)
        {
            int w=stream.GetProperty("width").GetInt32(),h=stream.GetProperty("height").GetInt32();
            int x=(int)N("x",0,0,w-2),y=(int)N("y",0,0,h-2);
            int cw=(int)N("width",w-x,2,w-x),ch=(int)N("height",h-y,2,h-y);
            return redact ? $"drawbox=x={x}:y={y}:w={cw}:h={ch}:color=black:t=fill" : $"crop={cw/2*2}:{ch/2*2}:{x/2*2}:{y/2*2}";
        }
        switch (tool)
        {
            case "video.compress": args.AddRange(new[] { "-crf", F(N("quality",28,18,45)), "-preset", "medium" }); break;
            case "audio.compress": args.AddRange(new[] { "-b:a", F(N("bitrate",128,32,320))+"k" }); break;
            case "video.metadata": case "audio.metadata":
                if(values.Flag("clear")) args.AddRange(new[]{"-map_metadata","-1"});
                foreach(string key in new[]{"title","artist","album","comment"}) args.AddRange(new[]{"-metadata",key+"="+values.Text(key)}); break;
            case "video.mute": args.Add("-an"); break;
            case "video.trim": case "audio.trim":
                double start=N("start",0,0,duration),end=N("end",duration,start+.001,duration);
                args.AddRange(new[]{"-ss",F(start),"-t",F(end-start)}); break;
            case "video.crop": vf=Rectangle(false);break;
            case "video.redact": vf=Rectangle(true);args.AddRange(new[]{"-map_metadata","-1"});break;
            case "video.speed":
                double speed=N("speed",2,.25,4);vf="setpts=PTS/"+F(speed);
                if(info.RootElement.GetProperty("streams").EnumerateArray().Any(s=>s.GetProperty("codec_type").GetString()=="audio"))
                { var filters=new List<string>();double remaining=speed;while(remaining>2){filters.Add("atempo=2");remaining/=2;}while(remaining<.5){filters.Add("atempo=0.5");remaining/=.5;}filters.Add("atempo="+F(remaining));af=string.Join(',',filters); } break;
            case "video.snapshot": args.AddRange(new[]{"-ss",F(N("time",0,0,Math.Max(0,duration-.001))),"-frames:v","1"});break;
            case "video.split":
                args.AddRange(new[]{"-f","segment","-segment_time",F(N("seconds",30,.1,duration)),"-reset_timestamps","1","-force_key_frames","expr:gte(t,n_forced*"+F(N("seconds",30,.1,duration))+")"});break;
            case "audio.normalize":af="loudnorm=I="+F(N("loudness",-16,-30,-5))+":TP=-1.5:LRA=11";break;
            case "audio.channels":args.AddRange(new[]{"-ac",F(N("channels",2,1,2))});break;
            case "audio.visualize":args.AddRange(new[]{"-filter_complex","[0:a]showwavespic=s=1600x600:colors=0xDFFF6B[v]","-map","[v]","-frames:v","1"});mappedAudio=true;break;
            case "audio.bleep":
                double from=N("start",0,0,duration),to=N("end",Math.Min(from+1,duration),from+.001,duration),hz=N("frequency",1000,100,5000);
                string filter=$"[0:a]volume=enable='between(t,{F(from)},{F(to)})':volume=0[muted];sine=frequency={F(hz)}:duration={F(duration)}:sample_rate=48000,volume=0.15,volume=enable='not(between(t,{F(from)},{F(to)}))':volume=0[tone];[muted][tone]amix=inputs=2:duration=first:normalize=0[out]";
                args.AddRange(new[]{"-filter_complex",filter,"-map","[out]"});mappedAudio=true;break;
            default:throw new InvalidOperationException("Unknown media tool.");
        }
        if(vf!=null)args.AddRange(new[]{"-vf",vf});if(af!=null)args.AddRange(new[]{"-af",af});
        if(ext=="mp4")args.AddRange(new[]{"-c:v","libx264","-pix_fmt","yuv420p","-c:a","aac"});
        else if(ext is "wav" or "mp3") { if(!mappedAudio)args.AddRange(new[]{"-vn","-map","0:a:0"});args.AddRange(new[]{"-c:a",ext=="wav"?"pcm_s16le":"libmp3lame"}); }
        string output=Path.Combine(directory,tool=="video.split"?"segment-%04d.mp4":"result."+ext);args.Add(output);
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromMinutes(30));
        await ConversionService.RunAsync(EnginePaths.Media,args,timeout.Token);
        return tool=="video.split"?Directory.GetFiles(directory,"segment-*.mp4").OrderBy(p=>p).ToArray():new[]{output};
    }
}
