import Foundation
import PDFKit

struct JobParameters: Sendable {
    var quality: Double = 0.8
    var maxDimension: Int = 1920
    var outputFolder: URL?
}

/// Serial engine execution bounds memory use; cancellation is checked at every output boundary.
actor JobQueue {
    func run(option: FileOption, inputs: [URL], parameters: JobParameters,
             progress: @Sendable (Double) async -> Void) async throws -> [URL] {
        guard (0.1...1).contains(parameters.quality), (16...16000).contains(parameters.maxDimension) else {
            throw AppError.input("Choose a valid image quality and a size from 16 to 16,000 pixels.")
        }
        guard !inputs.isEmpty else { throw AppError.input("Choose at least one file.") }
        for url in inputs {
            guard url.isFileURL, FileManager.default.isReadableFile(atPath: url.path),
                  ((try? url.resourceValues(forKeys: [.isRegularFileKey]).isRegularFile) == true ||
                   ["pages", "rtfd"].contains(url.pathExtension.lowercased()) || FileCategory.resolve(url) == .archive) else {
                throw AppError.input("A selected file is no longer readable. Choose it again.")
            }
        }
        let temp = FileManager.default.temporaryDirectory.appendingPathComponent("miracle-\(UUID().uuidString)")
        try FileManager.default.createDirectory(at: temp, withIntermediateDirectories: true, attributes: [.posixPermissions: 0o700])
        defer { try? FileManager.default.removeItem(at: temp) }
        var outputs: [URL] = []
        // If the batch fails or is cancelled, remove only outputs this job created.
        var completed = false
        defer { if !completed { for url in outputs { try? FileManager.default.removeItem(at: url) } } }
        func publish(_ file: URL, input: URL, stem: String, ext: String) throws {
            outputs.append(try OutputResolver.publish(file, beside: input, folder: parameters.outputFolder, stem: stem, extension: ext))
        }
        if option.kind == .imagePDF {
            let target = temp.appendingPathComponent("images.pdf")
            try PDFEngine.create(images: inputs, to: target)
            try publish(target, input: inputs[0], stem: inputs[0].deletingPathExtension().lastPathComponent + "-converted", ext: "pdf")
        } else {
            for (index, input) in inputs.enumerated() {
                try Task.checkCancellation()
                let stem = input.deletingPathExtension().lastPathComponent
                let target = temp.appendingPathComponent(UUID().uuidString)
                switch option.kind {
                case .convert(let category, let format):
                    await progress(.nan)
                    let exported = try await ConversionEngine.run(input: input, category: category, format: format, directory: temp)
                    for (offset, file) in exported.enumerated() {
                        let suffix = exported.count > 1 ? "-page-\(offset + 1)" : "-converted"
                        try publish(file, input: input, stem: stem + suffix, ext: category == .archive && format == "gz" ? "tar.gz" : format)
                    }
                case .image(let format):
                    let image = try ImageEngine.load(input)
                    try ImageEngine.write(image, to: target, format: format, quality: parameters.quality)
                    try publish(target, input: input, stem: stem + "-converted", ext: format == "jpeg" ? "jpg" : format)
                case .resize, .compress, .rotate:
                    var image = try ImageEngine.load(input, maxDimension: option.kind == .resize ? parameters.maxDimension : 24000)
                    if option.kind == .rotate { image = try ImageEngine.rotate(image) }
                    let format = option.kind == .compress ? "jpeg" : "png"
                    try ImageEngine.write(image, to: target, format: format, quality: parameters.quality)
                    try publish(target, input: input, stem: stem + "-" + option.title.lowercased().replacingOccurrences(of: " ", with: "-"), ext: format == "jpeg" ? "jpg" : format)
                case .pdfText:
                    let document = try PDFEngine.open(input)
                    guard let string = document.string, !string.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty else {
                        throw AppError.unsupported("This PDF has no selectable text. Text recognition is not included yet.")
                    }
                    try string.write(to: target, atomically: true, encoding: .utf8)
                    try publish(target, input: input, stem: stem + "-converted", ext: "txt")
                case .pdfImages, .pdfSplit:
                    let document = try PDFEngine.open(input)
                    for pageIndex in 0..<document.pageCount {
                        try Task.checkCancellation()
                        guard let page = document.page(at: pageIndex) else { throw AppError.engine("A PDF page could not be read.") }
                        if option.kind == .pdfImages {
                            try ImageEngine.write(PDFEngine.render(page), to: target, format: "png", quality: 1)
                        } else {
                            let single = PDFDocument(); single.insert(page, at: 0)
                            guard single.write(to: target) else { throw AppError.engine("The PDF page could not be saved.") }
                        }
                        try publish(target, input: input, stem: stem + "-page-\(pageIndex + 1)", ext: option.kind == .pdfImages ? "png" : "pdf")
                        await progress((Double(index) + Double(pageIndex + 1) / Double(document.pageCount)) / Double(inputs.count))
                    }
                default: throw AppError.unsupported("Choose a conversion or editing tool.")
                }
                await progress(Double(index + 1) / Double(inputs.count))
            }
        }
        try Task.checkCancellation()
        completed = true
        await progress(1)
        return outputs
    }
}
