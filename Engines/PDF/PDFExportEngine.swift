import Foundation
import PDFKit
import Vision

struct PDFExportEngine {
    static func texts(_ document: PDFDocument) throws -> [String] {
        var pages: [String] = []
        for index in 0..<document.pageCount {
            try Task.checkCancellation()
            guard let page = document.page(at: index) else { throw AppError.input("A PDF page could not be read.") }
            if let text = page.string, !text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty { pages.append(text); continue }
            let image = try PDFEngine.render(page)
            let request = VNRecognizeTextRequest()
            request.recognitionLevel = .accurate; request.usesLanguageCorrection = true
            try VNImageRequestHandler(cgImage: image).perform([request])
            let observations = (request.results ?? []).sorted {
                if abs($0.boundingBox.midY - $1.boundingBox.midY) > 0.015 { return $0.boundingBox.midY > $1.boundingBox.midY }
                return $0.boundingBox.minX < $1.boundingBox.minX
            }
            pages.append(observations.compactMap { $0.topCandidates(1).first?.string }.joined(separator: "\n"))
        }
        return pages
    }
    static func convert(_ input: URL, to output: URL, format: String, directory: URL) async throws {
        let document = try PDFEngine.open(input)
        if format == "pptx" {
            let result = try await DocumentEngine.office(input, format: "pptx", directory: directory,
                filter: "pptx:Impress MS PowerPoint 2007 XML", inputFilter: "impress_pdf_import")
            try FileManager.default.copyItem(at: result, to: output)
            return
        }
        let pages = try texts(document)
        let text = pages.joined(separator: "\n\n--- Page break ---\n\n")
        switch format {
        case "txt", "md": try text.write(to: output, atomically: true, encoding: .utf8)
        case "epub": try EPUBEngine.write(text: text, title: input.deletingPathExtension().lastPathComponent, output: output, directory: directory)
        case "xlsx":
            let csv = directory.appendingPathComponent("pdf-text-\(UUID().uuidString).csv")
            let rows = pages.enumerated().flatMap { index, page in page.components(separatedBy: "\n").map { "\(index + 1),\"\($0.replacingOccurrences(of: "\"", with: "\"\""))\"" } }
            try ("Page,Text\n" + rows.joined(separator: "\n")).write(to: csv, atomically: true, encoding: .utf8)
            let result = try await DocumentEngine.office(csv, format: "xlsx", directory: directory)
            try FileManager.default.copyItem(at: result, to: output)
        case "docx", "pages":
            let html = directory.appendingPathComponent("pdf-pages-\(UUID().uuidString).html")
            let body = pages.enumerated().map { index, text in
                "<section style=\"\(index > 0 ? "page-break-before:always" : "")\"><p>\(MarkupEngine.escape(text).replacingOccurrences(of: "\n", with: "<br/>"))</p></section>"
            }.joined()
            try "<!DOCTYPE html><html><head><meta charset=\"utf-8\"/></head><body>\(body)</body></html>".write(to: html, atomically: true, encoding: .utf8)
            let result = try await DocumentEngine.office(html, format: "docx", directory: directory)
            if format == "pages" { try await PagesService.convert(result, to: output) }
            else { try FileManager.default.copyItem(at: result, to: output) }
        default: throw AppError.unsupported("Choose a supported PDF export format.")
        }
    }
}
