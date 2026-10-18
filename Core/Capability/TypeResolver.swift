import Foundation
import UniformTypeIdentifiers
import ImageIO

struct TypeResolver {
    // Declared types avoid dependence on the LaunchServices extension database, which
    // can be unavailable in command-line tests and constrained desktop sessions.
    static func type(for url: URL) -> UTType {
        switch url.pathExtension.lowercased() {
        case "png": return .png
        case "jpg", "jpeg", "jpe": return .jpeg
        case "heic", "heif": return .heic
        case "tif", "tiff": return .tiff
        case "gif": return .gif
        case "bmp": return .bmp
        case "pdf": return .pdf
        default: return (try? url.resourceValues(forKeys: [.contentTypeKey]))?.contentType ?? UTType(filenameExtension: url.pathExtension) ?? .data
        }
    }
    static func isImage(_ type: UTType) -> Bool {
        type.conforms(to: .image) || (CGImageSourceCopyTypeIdentifiers() as! [String]).contains(type.identifier)
    }
}
