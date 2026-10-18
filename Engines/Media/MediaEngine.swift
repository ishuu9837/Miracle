import Foundation

struct MediaEngine {
    static func convert(_ input: URL, to output: URL, format: String, category: FileCategory, directory: URL) async throws {
        var args = ["-nostdin", "-hide_banner", "-loglevel", "error", "-y", "-threads", "2", "-protocol_whitelist", "file,pipe,crypto", "-i", input.path, "-map_metadata", "-1"]
        if category == .image {
            args += ["-frames:v", "1", "-an"]
            switch format {
            case "webp": args += ["-c:v", "libwebp", "-quality", "90", "-f", "webp"]
            case "avif": args += ["-c:v", "libaom-av1", "-still-picture", "1", "-cpu-used", "6", "-crf", "28", "-f", "avif"]
            default: args += ["-f", "image2", "-update", "1"]
            }
        } else if category == .audio || format == "mp3" {
            args += ["-vn", "-map", "0:a:0"]
            switch format {
            case "mp3": args += ["-c:a", "libmp3lame", "-q:a", "2", "-f", "mp3"]
            case "m4a": args += ["-c:a", "aac", "-b:a", "192k", "-f", "ipod"]
            case "wav": args += ["-c:a", "pcm_s16le", "-f", "wav"]
            case "aiff": args += ["-c:a", "pcm_s16be", "-f", "aiff"]
            case "flac": args += ["-c:a", "flac", "-f", "flac"]
            case "ogg": args += ["-c:a", "libvorbis", "-q:a", "5", "-f", "ogg"]
            case "opus": args += ["-c:a", "libopus", "-b:a", "160k", "-f", "opus"]
            default: throw AppError.unsupported("Choose a supported audio format.")
            }
        } else {
            switch format {
            case "mp4", "mov", "mkv":
                args += ["-map", "0:v:0", "-map", "0:a?", "-c:v", "libx264", "-preset", "fast", "-crf", "21", "-pix_fmt", "yuv420p", "-vf", "pad=ceil(iw/2)*2:ceil(ih/2)*2", "-c:a", "aac", "-b:a", "192k"]
                args += ["-f", format == "mkv" ? "matroska" : format]
                if format != "mkv" { args += ["-movflags", "+faststart"] }
            case "webm": args += ["-c:v", "libvpx-vp9", "-crf", "32", "-b:v", "0", "-cpu-used", "4", "-c:a", "libopus", "-f", "webm"]
            case "gif": args += ["-an", "-vf", "fps=12,scale=640:-1:flags=lanczos,split[a][b];[a]palettegen[p];[b][p]paletteuse", "-loop", "0", "-f", "gif"]
            default: throw AppError.unsupported("Choose a supported video format.")
            }
        }
        args.append(output.path)
        try await ProcessRunner.run(EngineLocator.ffmpeg, arguments: args, directory: directory, timeout: 3600)
    }
}
