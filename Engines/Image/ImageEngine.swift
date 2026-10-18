import Foundation
import AppKit
import ImageIO
import CoreGraphics
import UniformTypeIdentifiers

struct ImageEngine {
    static func load(_ url: URL, maxDimension: Int = 24000) throws -> CGImage {
        if url.pathExtension.lowercased() == "svg" {
            let data = try Data(contentsOf: url)
            guard data.count <= 8 * 1024 * 1024, let xml = String(data: data, encoding: .utf8) else { throw AppError.input("This SVG is too large or not UTF-8.") }
            let lower = xml.lowercased().replacingOccurrences(of: "http://www.w3.org/2000/svg", with: "").replacingOccurrences(of: "http://www.w3.org/1999/xlink", with: "").replacingOccurrences(of: "url(#", with: "local-reference(")
            guard !["<!doctype", "<!entity", "<script", "<foreignobject", "@import", "http:", "https:", "file:", "url("].contains(where: lower.contains) else {
                throw AppError.input("This SVG contains scripts or external references. Export a self-contained SVG and try again.")
            }
            let references = try NSRegularExpression(pattern: #"(?:href|src)\s*=\s*["'](?!#|data:image/)[^"']"#)
            guard references.firstMatch(in: lower, range: NSRange(lower.startIndex..., in: lower)) == nil else { throw AppError.input("This SVG references another file. Embed its images before converting it.") }
            guard let image = NSImage(data: data), image.size.width > 0, image.size.height > 0 else { throw AppError.input("This SVG could not be decoded.") }
            let scale = min(1, CGFloat(maxDimension) / max(image.size.width, image.size.height))
            var bounds = CGRect(x: 0, y: 0, width: image.size.width * scale, height: image.size.height * scale)
            guard let cg = image.cgImage(forProposedRect: &bounds, context: nil, hints: nil) else { throw AppError.input("This SVG cannot be rendered on this Mac.") }
            guard let context = CGContext(data: nil, width: max(1, Int(bounds.width)), height: max(1, Int(bounds.height)), bitsPerComponent: 8, bytesPerRow: 0, space: CGColorSpaceCreateDeviceRGB(), bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue) else { throw AppError.engine("There is not enough memory to render this SVG.") }
            context.draw(cg, in: CGRect(origin: .zero, size: bounds.size))
            guard let rendered = context.makeImage() else { throw AppError.engine("The SVG could not be rendered.") }
            return rendered
        }
        guard let source = CGImageSourceCreateWithURL(url as CFURL, nil), CGImageSourceGetCount(source) > 0 else {
            throw AppError.input("This image could not be opened. Choose another file.")
        }
        let options: [CFString: Any] = [kCGImageSourceCreateThumbnailFromImageAlways: true,
            kCGImageSourceThumbnailMaxPixelSize: maxDimension, kCGImageSourceCreateThumbnailWithTransform: true]
        guard let image = CGImageSourceCreateThumbnailAtIndex(source, 0, options as CFDictionary) else {
            throw AppError.engine("This image is too large or damaged to decode.")
        }
        return image
    }

    static func write(_ image: CGImage, to url: URL, format: String, quality: Double, dpi: Double? = nil) throws {
        let type: UTType
        switch format {
        case "jpeg": type = .jpeg
        case "heic": type = .heic
        case "tiff": type = .tiff
        case "bmp": type = .bmp
        case "gif": type = .gif
        case "png": type = .png
        default: throw AppError.unsupported("Choose a supported image format.")
        }
        guard let destination = CGImageDestinationCreateWithURL(url as CFURL, type.identifier as CFString, 1, nil) else {
            throw AppError.unsupported("This format cannot be written on this Mac.")
        }
        var properties: [CFString: Any] = [kCGImageDestinationLossyCompressionQuality: quality]
        if let dpi { properties[kCGImagePropertyDPIWidth] = dpi; properties[kCGImagePropertyDPIHeight] = dpi }
        CGImageDestinationAddImage(destination, image, properties as CFDictionary)
        guard CGImageDestinationFinalize(destination) else { throw AppError.engine("The image could not be saved. Check available disk space.") }
    }

    static func rotate(_ image: CGImage) throws -> CGImage {
        guard let context = CGContext(data: nil, width: image.height, height: image.width, bitsPerComponent: 8,
                                      bytesPerRow: 0, space: CGColorSpaceCreateDeviceRGB(), bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue) else {
            throw AppError.engine("There is not enough memory to rotate this image.")
        }
        context.translateBy(x: CGFloat(image.height), y: 0)
        context.rotate(by: .pi / 2)
        context.draw(image, in: CGRect(x: 0, y: 0, width: image.width, height: image.height))
        guard let output = context.makeImage() else { throw AppError.engine("The image could not be rotated.") }
        return output
    }
}
