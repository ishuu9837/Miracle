import Foundation

enum FileCategory: String, CaseIterable, Sendable {
    case image, audio, video, pdf, document, slides, spreadsheet, subtitle, archive, other
    static func resolve(_ url: URL) -> FileCategory {
        let ext = url.pathExtension.lowercased()
        if ["gif", "mp4", "mov", "mkv", "webm", "avi", "wmv", "m4v"].contains(ext) { return .video }
        if ["jpg", "jpeg", "png", "webp", "heic", "heif", "tif", "tiff", "avif", "svg", "bmp"].contains(ext) { return .image }
        if ["mp3", "m4a", "wav", "flac", "ogg", "opus", "aiff", "aif", "wma"].contains(ext) { return .audio }
        if ext == "pdf" { return .pdf }
        if ["docx", "pages", "md", "markdown", "txt", "epub", "rtf", "rtfd", "textclipping"].contains(ext) { return .document }
        if ["ppt", "pptx"].contains(ext) { return .slides }
        if ["xlsx", "xls", "ods", "csv", "tsv"].contains(ext) { return .spreadsheet }
        if ["srt", "vtt"].contains(ext) { return .subtitle }
        if ["zip", "tar", "gz", "gzip", "tgz", "rar"].contains(ext) { return .archive }
        if (try? url.resourceValues(forKeys: [.isDirectoryKey]).isDirectory) == true { return .archive }
        return .other
    }
}
