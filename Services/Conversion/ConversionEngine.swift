import Foundation

struct ConversionEngine {
    static func run(input: URL, category: FileCategory, format: String, directory: URL) async throws -> [URL] {
        let output = directory.appendingPathComponent(UUID().uuidString).appendingPathExtension(format)
        switch category {
        case .audio, .video:
            try await MediaEngine.convert(input, to: output, format: format, category: category, directory: directory)
        case .image:
            if ["webp", "avif"].contains(format) {
                let source = directory.appendingPathComponent(UUID().uuidString).appendingPathExtension("png")
                try ImageEngine.write(ImageEngine.load(input), to: source, format: "png", quality: 1)
                try await MediaEngine.convert(source, to: output, format: format, category: .image, directory: directory)
            } else {
                try ImageEngine.write(ImageEngine.load(input), to: output, format: format == "jpg" ? "jpeg" : format, quality: 0.9)
            }
        case .pdf, .slides:
            if ["jpg", "png"].contains(format) {
                let pdf: URL
                if category == .slides { pdf = try await DocumentEngine.office(input, format: "pdf", directory: directory) }
                else { pdf = input }
                let document = try PDFEngine.open(pdf)
                var results: [URL] = []
                for pageIndex in 0..<document.pageCount {
                    try Task.checkCancellation()
                    guard let page = document.page(at: pageIndex) else { throw AppError.engine("A page could not be read.") }
                    let file = directory.appendingPathComponent(UUID().uuidString).appendingPathExtension(format)
                    try ImageEngine.write(PDFEngine.render(page), to: file, format: format == "jpg" ? "jpeg" : "png", quality: 0.95, dpi: 300)
                    results.append(file)
                }
                return results
            }
            if category == .pdf { try await PDFExportEngine.convert(input, to: output, format: format, directory: directory) }
            else { try await DocumentEngine.convert(input, to: output, format: format, category: category, directory: directory) }
        case .document, .spreadsheet:
            try await DocumentEngine.convert(input, to: output, format: format, category: category, directory: directory)
        case .subtitle: try SubtitleEngine.convert(input, to: output, format: format)
        case .archive:
            if (try? input.resourceValues(forKeys: [.isDirectoryKey]).isDirectory) == true { try ArchiveEngine.packDirectory(input, to: output, format: format) }
            else { try ArchiveEngine.repack(input, to: output, format: format) }
        case .other: throw AppError.unsupported("This file type has no conversion options.")
        }
        return [output]
    }
}
