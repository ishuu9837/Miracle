import XCTest
import AppKit
@testable import Miracle

/// Opt-in slow audit of each requested canonical pair, using generated fixtures.
/// Run with MIRACLE_FULL_MATRIX_REPORT=/absolute/path/results.csv Scripts/test.sh.
final class ConversionMatrixTests: XCTestCase {
    func testFullRequestedMatrix() async throws {
        guard let report = ProcessInfo.processInfo.environment["MIRACLE_FULL_MATRIX_REPORT"] else {
            throw XCTSkip("Set MIRACLE_FULL_MATRIX_REPORT to run the complete fixture audit.")
        }
        let root = FileManager.default.temporaryDirectory.appendingPathComponent("miracle-matrix-\(UUID().uuidString)")
        try FileManager.default.createDirectory(at: root, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: root) }
        var fixtures: [String: URL] = [:]
        func write(_ ext: String, _ text: String) throws -> URL {
            let url = root.appendingPathComponent("input.\(ext)")
            try text.write(to: url, atomically: true, encoding: .utf8)
            return url
        }
        let svg = try write("svg", "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"80\" height=\"40\"><rect width=\"80\" height=\"40\" fill=\"red\"/></svg>")
        fixtures["svg"] = svg
        let png = root.appendingPathComponent("input.png")
        try ImageEngine.write(ImageEngine.load(svg), to: png, format: "png", quality: 1)
        fixtures["png"] = png
        for ext in ["jpg", "heic", "tiff", "webp", "avif"] {
            fixtures[ext] = try await ConversionEngine.run(input: png, category: .image, format: ext, directory: root).first
        }
        let bmp = root.appendingPathComponent("input.bmp")
        try ImageEngine.write(ImageEngine.load(png), to: bmp, format: "bmp", quality: 1)
        fixtures["bmp"] = bmp
        let wav = root.appendingPathComponent("input.wav")
        try await ProcessRunner.run(EngineLocator.ffmpeg, arguments: ["-y", "-f", "lavfi", "-i", "sine=duration=0.12", wav.path], directory: root)
        fixtures["wav"] = wav
        for ext in ["mp3", "m4a", "flac", "ogg", "opus", "aiff"] {
            fixtures[ext] = try await ConversionEngine.run(input: wav, category: .audio, format: ext, directory: root).first
        }
        let wma = root.appendingPathComponent("input.wma")
        try await ProcessRunner.run(EngineLocator.ffmpeg, arguments: ["-y", "-i", wav.path, "-c:a", "wmav2", wma.path], directory: root)
        fixtures["wma"] = wma
        let mp4 = root.appendingPathComponent("input.mp4")
        try await ProcessRunner.run(EngineLocator.ffmpeg, arguments: ["-y", "-f", "lavfi", "-i", "color=c=blue:s=64x48:r=12:d=0.2", "-f", "lavfi", "-i", "sine=duration=0.2", "-c:v", "libx264", "-c:a", "aac", "-shortest", mp4.path], directory: root)
        fixtures["mp4"] = mp4
        for ext in ["mov", "mkv", "webm", "gif"] {
            fixtures[ext] = try await ConversionEngine.run(input: mp4, category: .video, format: ext, directory: root).first
        }
        for (ext, video, audio) in [("avi", "mpeg4", "libmp3lame"), ("wmv", "wmv2", "wmav2")] {
            let url = root.appendingPathComponent("input.\(ext)")
            try await ProcessRunner.run(EngineLocator.ffmpeg, arguments: ["-y", "-i", mp4.path, "-c:v", video, "-c:a", audio, url.path], directory: root)
            fixtures[ext] = url
        }
        let md = try write("md", "# Miracle matrix\nA generated paragraph for conversion verification.")
        fixtures["md"] = md
        fixtures["txt"] = try write("txt", "Miracle matrix\nA generated paragraph.")
        for ext in ["docx", "epub"] {
            fixtures[ext] = try await ConversionEngine.run(input: md, category: .document, format: ext, directory: root).first
        }
        let docx = try XCTUnwrap(fixtures["docx"])
        fixtures["pdf"] = try await DocumentEngine.office(docx, format: "pdf", directory: root)
        fixtures["rtf"] = try write("rtf", "{\\rtf1\\ansi Miracle matrix paragraph.}")
        let rtfd = root.appendingPathComponent("input.rtfd")
        try FileManager.default.createDirectory(at: rtfd, withIntermediateDirectories: true)
        try FileManager.default.copyItem(at: XCTUnwrap(fixtures["rtf"]), to: rtfd.appendingPathComponent("TXT.rtf"))
        fixtures["rtfd"] = rtfd
        let clipping = root.appendingPathComponent("input.textclipping")
        let clippingData = try PropertyListSerialization.data(fromPropertyList: ["UTI-Data": ["public.utf8-plain-text": Data("Miracle matrix clipping".utf8)]], format: .binary, options: 0)
        try clippingData.write(to: clipping); fixtures["textclipping"] = clipping
        fixtures["csv"] = try write("csv", "Name,Value\nAlpha,1\nBeta,2\n")
        fixtures["tsv"] = try write("tsv", "Name\tValue\nAlpha\t1\nBeta\t2\n")
        for ext in ["xlsx", "ods"] {
            fixtures[ext] = try await DocumentEngine.office(XCTUnwrap(fixtures["csv"]), format: ext, directory: root)
        }
        fixtures["xls"] = try await DocumentEngine.office(XCTUnwrap(fixtures["xlsx"]), format: "xls", directory: root, filter: "xls:MS Excel 97")
        fixtures["pptx"] = try await ConversionEngine.run(input: XCTUnwrap(fixtures["pdf"]), category: .pdf, format: "pptx", directory: root).first
        fixtures["ppt"] = try await DocumentEngine.office(XCTUnwrap(fixtures["pptx"]), format: "ppt", directory: root, filter: "ppt:MS PowerPoint 97")
        fixtures["srt"] = try write("srt", "1\n00:00:00,000 --> 00:00:00,100\nMiracle matrix\n")
        fixtures["vtt"] = try write("vtt", "WEBVTT\n\n00:00:00.000 --> 00:00:00.100\nMiracle matrix\n")
        let archiveFiles = root.appendingPathComponent("archive-input")
        try FileManager.default.createDirectory(at: archiveFiles, withIntermediateDirectories: true)
        try "Miracle archive fixture".write(to: archiveFiles.appendingPathComponent("note.txt"), atomically: true, encoding: .utf8)
        for ext in ["zip", "tar", "gz"] {
            let output = root.appendingPathComponent("input.\(ext)")
            try ArchiveEngine.packDirectory(archiveFiles, to: output, format: ext); fixtures[ext] = output
        }
        fixtures["rar"] = URL(fileURLWithPath: #filePath).deletingLastPathComponent().deletingLastPathComponent().appendingPathComponent("Fixtures/regular-files.rar")
        let families = ["image": "jpg png webp heic tiff avif svg bmp", "audio": "mp3 m4a wav flac ogg opus aiff wma", "video": "mp4 mov mkv webm gif avi wmv", "pdf": "pdf", "document": "docx pages md txt epub rtf rtfd textclipping", "slides": "ppt pptx", "spreadsheet": "xlsx ods csv tsv xls", "subtitle": "srt vtt", "archive": "zip tar gz rar"]
        var rows = ["category,input,output,status,detail"]
        var checked = 0
        var failures: [String] = []
        for categoryName in families.keys.sorted() {
            let category = try XCTUnwrap(FileCategory(rawValue: categoryName))
            for source in try XCTUnwrap(families[categoryName]).split(separator: " ").map(String.init) {
                let input = fixtures[source] ?? root.appendingPathComponent("input.\(source)")
                for option in CapabilityRegistry.options(for: [input], mode: .convert) {
                    let target = option.id.components(separatedBy: ".").last ?? ""
                    checked += 1
                    if source == "pages" || target == "pages" {
                        rows.append("\(categoryName),\(source),\(target),UNAVAILABLE,Apple Pages not installed; requires a separate runtime check")
                        continue
                    }
                    let job = root.appendingPathComponent("job-\(UUID().uuidString)")
                    try FileManager.default.createDirectory(at: job, withIntermediateDirectories: true)
                    do {
                        let results: [URL]
                        if category == .image && target == "pdf" {
                            let pdf = job.appendingPathComponent("image.pdf")
                            try PDFEngine.create(images: [input], to: pdf); results = [pdf]
                        } else { results = try await ConversionEngine.run(input: input, category: category, format: target, directory: job) }
                        XCTAssertFalse(results.isEmpty)
                        for output in results { try await validate(output, format: target, category: category, directory: job) }
                        rows.append("\(categoryName),\(source),\(target),PASS,Generated fixture exported and output reopened")
                    } catch {
                        failures.append("\(categoryName).\(source).\(target)")
                        rows.append("\(categoryName),\(source),\(target),FAIL,\(String(describing: error).replacingOccurrences(of: ",", with: ";").replacingOccurrences(of: "\n", with: " ").replacingOccurrences(of: "\"", with: "'"))")
                    }
                    try FileManager.default.removeItem(at: job)
                }
            }
        }
        try rows.joined(separator: "\n").appending("\n").write(toFile: report, atomically: true, encoding: .utf8)
        XCTAssertEqual(checked, 230)
        // The requested GIF → MP3 pair has no possible audio source. Any other
        // failure is unexpected and must fail the audit after its report is saved.
        XCTAssertEqual(failures, ["video.gif.mp3"])
    }
    private func validate(_ output: URL, format: String, category: FileCategory, directory: URL) async throws {
        XCTAssertGreaterThan(try Data(contentsOf: output).count, 0)
        if format == "pdf" { XCTAssertGreaterThan(try PDFEngine.open(output).pageCount, 0) }
        else if ["jpg", "png", "heic", "tiff", "webp", "avif"].contains(format) { XCTAssertGreaterThan(try ImageEngine.load(output).width, 0) }
        else if ["mp3", "m4a", "wav", "flac", "ogg", "opus", "aiff", "mp4", "mov", "mkv", "webm", "gif"].contains(format) {
            try await ProcessRunner.run(EngineLocator.ffmpeg, arguments: ["-v", "error", "-i", output.path, "-f", "null", "-"], directory: directory)
        } else if format == "docx" { XCTAssertNotNil(try ArchiveEngine.readEntries(output)["word/document.xml"]) }
        else if format == "pptx" { XCTAssertNotNil(try ArchiveEngine.readEntries(output)["ppt/slides/slide1.xml"]) }
        else if format == "xlsx" { XCTAssertNotNil(try ArchiveEngine.readEntries(output)["xl/workbook.xml"]) }
        else if format == "ods" { XCTAssertNotNil(try ArchiveEngine.readEntries(output)["content.xml"]) }
        else if format == "epub" { let text = try await DocumentEngine.text(output, directory: directory); XCTAssertFalse(text.isEmpty) }
        else if ["zip", "tar", "gz"].contains(format) {
            if format == "zip" { XCTAssertFalse(try ArchiveEngine.readEntries(output).isEmpty) }
            else { try ArchiveEngine.repack(output, to: directory.appendingPathComponent("validate.zip"), format: "zip") }
        } else if category == .subtitle && format != "txt" {
            XCTAssertFalse(try SubtitleEngine.parse(String(contentsOf: output, encoding: .utf8)).isEmpty)
        }
    }
}
