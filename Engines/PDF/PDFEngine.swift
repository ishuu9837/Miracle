import Foundation
import PDFKit
import CoreGraphics

struct PDFEngine {
    static func open(_ url: URL) throws -> PDFDocument {
        guard let document = PDFDocument(url: url), !document.isLocked, document.pageCount > 0 else {
            throw AppError.input("This PDF is locked, empty, or damaged. Unlock it before trying again.")
        }
        return document
    }
    static func create(images: [URL], to output: URL) throws {
        guard let context = CGContext(output as CFURL, mediaBox: nil, nil) else { throw AppError.engine("The PDF could not be created.") }
        for url in images {
            try Task.checkCancellation()
            let image = try ImageEngine.load(url)
            var bounds = CGRect(x: 0, y: 0, width: image.width, height: image.height)
            let box = Data(bytes: &bounds, count: MemoryLayout<CGRect>.size)
            context.beginPDFPage([kCGPDFContextMediaBox: box] as CFDictionary)
            context.draw(image, in: bounds)
            context.endPDFPage()
        }
        context.closePDF()
    }
    static func render(_ page: PDFPage) throws -> CGImage {
        let bounds = page.bounds(for: .mediaBox)
        let scale = 300.0 / 72.0
        guard bounds.width.isFinite, bounds.height.isFinite, bounds.width > 0, bounds.height > 0,
              bounds.width * scale <= 30000, bounds.height * scale <= 30000,
              bounds.width * bounds.height * scale * scale <= 120_000_000 else {
            throw AppError.input("A PDF page is too large to render safely at 300 DPI. Resize the page before exporting it.")
        }
        guard bounds.width > 0, bounds.height > 0, let context = CGContext(data: nil,
            width: max(1, Int(bounds.width * scale)), height: max(1, Int(bounds.height * scale)), bitsPerComponent: 8,
            bytesPerRow: 0, space: CGColorSpaceCreateDeviceRGB(), bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue) else {
            throw AppError.engine("This PDF page cannot be rendered.")
        }
        context.setFillColor(CGColor(gray: 1, alpha: 1))
        context.fill(CGRect(x: 0, y: 0, width: context.width, height: context.height))
        context.scaleBy(x: scale, y: scale)
        context.translateBy(x: -bounds.minX, y: -bounds.minY)
        page.draw(with: .mediaBox, to: context)
        guard let image = context.makeImage() else { throw AppError.engine("The page could not be rendered.") }
        return image
    }
}
