import Foundation
import UniformTypeIdentifiers
import ImageIO

enum WheelMode: String, CaseIterable, Identifiable, Sendable {
    case convert = "Convert", tools = "Tools", actions = "Actions"
    var id: String { rawValue }
    var key: String { switch self { case .convert: return "⇧"; case .tools: return "⌘"; case .actions: return "⌃" } }
}

enum OperationKind: Equatable, Sendable {
    case convert(FileCategory, String)
    case image(String), imagePDF, resize, compress, rotate, pdfImages, pdfSplit, pdfText
    case copy, move, copyPath, reveal, airDrop, float, terminal
}

struct FileOption: Identifiable, Sendable {
    let id: String
    let title: String
    let symbol: String
    let kind: OperationKind
    var unavailableReason: String? = nil
    var note: String? = nil
    var needsPanel: Bool { kind == .resize || kind == .compress }
}

/// Source-specific catalog; conditional capabilities carry explicit unavailable reasons.
struct CapabilityRegistry {
    static func options(for urls: [URL], mode: WheelMode) -> [FileOption] {
        guard !urls.isEmpty else { return [] }
        if mode == .actions {
            return [
                .init(id: "action.copy", title: "Copy files", symbol: "doc.on.doc", kind: .copy),
                .init(id: "action.path", title: "Copy path", symbol: "link", kind: .copyPath),
                .init(id: "action.float", title: "Float", symbol: "square.stack.3d.up", kind: .float),
                .init(id: "action.airdrop", title: "AirDrop", symbol: "airplayaudio", kind: .airDrop),
                .init(id: "action.reveal", title: "Reveal", symbol: "folder", kind: .reveal),
                .init(id: "action.terminal", title: "Terminal", symbol: "terminal", kind: .terminal),
                .init(id: "action.move", title: "Move…", symbol: "folder.badge.arrow.forward", kind: .move)
            ]
        }
        let categories = urls.map(FileCategory.resolve)
        guard let category = categories.first, categories.allSatisfy({ $0 == category }) else { return [] }
        if mode == .tools {
            if category == .image {
                return [
                    .init(id: "image.resize", title: "Resize", symbol: "arrow.up.left.and.arrow.down.right", kind: .resize),
                    .init(id: "image.compress", title: "Compress", symbol: "arrow.down.right.and.arrow.up.left", kind: .compress),
                    .init(id: "image.rotate", title: "Rotate right", symbol: "rotate.right", kind: .rotate)
                ]
            }
            if category == .pdf { return [.init(id: "pdf.split", title: "Split pages", symbol: "rectangle.split.2x1", kind: .pdfSplit)] }
            return []
        }
        let formats: [String]
        switch category {
        case .image: formats = ["jpg", "png", "webp", "heic", "tiff", "avif", "pdf"]
        case .audio: formats = ["mp3", "m4a", "wav", "flac", "ogg", "opus", "aiff"]
        case .video: formats = ["mp4", "mov", "mkv", "webm", "gif", "mp3"]
        case .pdf: formats = ["docx", "pages", "pptx", "xlsx", "jpg", "png", "epub", "txt", "md"]
        case .document:
            let officeInputs = urls.allSatisfy { ["docx", "pages"].contains($0.pathExtension.lowercased()) }
            formats = ["docx", "pages", "md", "txt", "epub", "html"] + (officeInputs ? ["pdf"] : [])
        case .slides: formats = ["pdf", "jpg", "png"]
        case .spreadsheet: formats = ["xlsx", "ods", "csv", "tsv", "pdf"]
        case .subtitle: formats = ["srt", "vtt", "txt"]
        case .archive: formats = ["zip", "tar", "gz"]
        case .other: formats = []
        }
        return formats.filter { format in
            !urls.allSatisfy { canonical($0.pathExtension) == format }
        }.map { format in
            let imagePDF = category == .image && format == "pdf"
            var result = FileOption(id: "\(category.rawValue).convert.\(format)", title: format == "gz" ? "GZIP" : format.uppercased(),
                symbol: symbol(for: category), kind: imagePDF ? .imagePDF : .convert(category, format))
            if format == "pages", !EngineLocator.pagesAvailable { result.unavailableReason = "Install Apple Pages to export .pages files." }
            if category == .video && format == "mp3" && urls.contains(where: { $0.pathExtension.lowercased() == "gif" }) {
                result.unavailableReason = "GIF files contain no audio track to extract. Select a video with audio."
            }
            if category == .pdf {
                if ["docx", "pages", "md", "epub", "txt"].contains(format) { result.note = "All pages · text reflow, with OCR for scanned pages" }
                if format == "xlsx" { result.note = "All pages · text lines in Page and Text columns; tables are not reconstructed" }
                if format == "pptx" { result.note = "All pages · one slide per PDF page" }
                if ["jpg", "png"].contains(format) { result.note = "All pages · 300 DPI" }
            }
            if category == .document { result.note = "Text-first export · complex layouts and embedded media may change" }
            if category == .archive, format == "gz" { result.note = "Creates a gzip-compressed TAR (.tar.gz)" }
            return result
        }
    }
    private static func canonical(_ ext: String) -> String {
        switch ext.lowercased() { case "jpeg": return "jpg"; case "tif": return "tiff"; case "aif": return "aiff"; case "markdown": return "md"; case "gzip", "tgz": return "gz"; default: return ext.lowercased() }
    }
    private static func symbol(for category: FileCategory) -> String {
        switch category { case .audio: return "waveform"; case .video: return "film"; case .image: return "photo"; case .archive: return "archivebox"; default: return "doc" }
    }
}
