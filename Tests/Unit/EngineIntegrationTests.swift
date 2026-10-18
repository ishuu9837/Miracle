import XCTest
import ImageIO
import CoreGraphics
@testable import Miracle

final class EngineIntegrationTests: XCTestCase {
    var folder: URL!
    override func setUpWithError() throws {
        folder = FileManager.default.temporaryDirectory.appendingPathComponent("miracle-engine-test-\(UUID().uuidString)")
        try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
    }
    override func tearDownWithError() throws { try FileManager.default.removeItem(at: folder) }
    func file(_ name: String, _ text: String) throws -> URL {
        let url = folder.appendingPathComponent(name)
        try text.write(to: url, atomically: true, encoding: .utf8)
        return url
    }
    func testRequestedFormatMatrixAndInputOnlyTypes() throws {
        let expected: [(String, Int)] = [("source.bmp", 7), ("source.wma", 7), ("source.avi", 6), ("source.pdf", 9), ("source.rtf", 6), ("source.ppt", 3), ("source.xls", 5), ("source.srt", 2), ("source.rar", 3)]
        for (name, count) in expected {
            let source = try file(name, "fixture")
            let options = CapabilityRegistry.options(for: [source], mode: .convert)
            XCTAssertEqual(options.count, count, name)
            for option in options { XCTAssertFalse(["BMP", "SVG", "WMA", "AVI", "WMV", "PPT", "XLS", "RAR"].contains(option.title)) }
        }
    }
    func testCatalogHas230CanonicalOrderedPairs() throws {
        let inputs = ["jpg png webp heic tiff avif svg bmp", "mp3 m4a wav flac ogg opus aiff wma", "mp4 mov mkv webm gif avi wmv", "pdf", "docx pages md txt epub rtf rtfd textclipping", "ppt pptx", "xlsx ods csv tsv xls", "srt vtt", "zip tar gz rar"]
        let expected = [50, 49, 37, 9, 45, 6, 21, 4, 9]
        var total = 0
        for (extensions, count) in zip(inputs, expected) {
            var actual = 0
            for ext in extensions.split(separator: " ") {
                let source = try file("catalog.\(ext)", "fixture")
                actual += CapabilityRegistry.options(for: [source], mode: .convert).count
            }
            XCTAssertEqual(actual, count, extensions)
            total += actual
        }
        XCTAssertEqual(total, 230)
        let gifOptions = CapabilityRegistry.options(for: [try file("silent.gif", "fixture")], mode: .convert)
        XCTAssertNotNil(gifOptions.first(where: { $0.title == "MP3" })?.unavailableReason)
    }
    func testSubtitleRoundTripAndInvalidTimestamps() throws {
        let srt = try file("captions.srt", "1\n00:00:01,000 --> 00:00:03,500\nHello, world.\n\n2\n00:00:04,000 --> 00:00:05,000\nSecond cue.\n")
        let vtt = folder.appendingPathComponent("captions.vtt"), roundtrip = folder.appendingPathComponent("roundtrip.srt")
        try SubtitleEngine.convert(srt, to: vtt, format: "vtt")
        try SubtitleEngine.convert(vtt, to: roundtrip, format: "srt")
        XCTAssertEqual(try SubtitleEngine.parse(String(contentsOf: srt, encoding: .utf8)), try SubtitleEngine.parse(String(contentsOf: roundtrip, encoding: .utf8)))
        let txt = folder.appendingPathComponent("captions.txt")
        try SubtitleEngine.convert(srt, to: txt, format: "txt")
        XCTAssertTrue(try String(contentsOf: txt, encoding: .utf8).contains("Second cue."))
        XCTAssertThrowsError(try SubtitleEngine.parse("1\n00:00:05,000 --> 00:00:01,000\nInvalid"))
    }
    func testArchiveRepackingAndUnsafePaths() throws {
        let directory = folder.appendingPathComponent("files")
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        try "Archive fixture".write(to: directory.appendingPathComponent("note.txt"), atomically: true, encoding: .utf8)
        var previous = folder.appendingPathComponent("fixture.zip")
        try ArchiveEngine.zipDirectory(directory, to: previous)
        for ext in ["tar", "gz", "zip"] {
            let result = folder.appendingPathComponent("converted.\(ext)")
            try ArchiveEngine.repack(previous, to: result, format: ext)
            previous = result
        }
        let entries = try ArchiveEngine.readEntries(previous)
        XCTAssertEqual(entries["note.txt"], Data("Archive fixture".utf8))
        for path in ["../escape", "/tmp/escape", "dir/../../escape", "C:\\escape"] { XCTAssertFalse(ArchiveEngine.safePath(path)) }
        let invalid = try file("invalid.rar", "not an archive")
        XCTAssertThrowsError(try ArchiveEngine.repack(invalid, to: folder.appendingPathComponent("invalid.zip"), format: "zip"))
        try FileManager.default.createSymbolicLink(at: directory.appendingPathComponent("link"), withDestinationURL: URL(fileURLWithPath: "/etc/passwd"))
        XCTAssertThrowsError(try ArchiveEngine.zipDirectory(directory, to: folder.appendingPathComponent("unsafe.zip")))
    }
    func testRARInputAndWorkerWriteBoundary() async throws {
        let fixture = URL(fileURLWithPath: #filePath).deletingLastPathComponent().deletingLastPathComponent().appendingPathComponent("Fixtures/regular-files.rar")
        let output = folder.appendingPathComponent("rar-export.zip")
        try ArchiveEngine.repack(fixture, to: output, format: "zip")
        XCTAssertNotNil(try ArchiveEngine.readEntries(output)["test.txt"])
        let outside = folder.deletingLastPathComponent().appendingPathComponent("miracle-write-boundary-\(UUID().uuidString)")
        defer { try? FileManager.default.removeItem(at: outside) }
        do {
            try await ProcessRunner.run(URL(fileURLWithPath: "/usr/bin/touch"), arguments: [outside.path], directory: folder)
            XCTFail("Worker must not write outside its private job directory")
        } catch { XCTAssertTrue(error is AppError) }
        XCTAssertFalse(FileManager.default.fileExists(atPath: outside.path))
    }
    func testEPUBSpineOrderAndTextRoundTrip() async throws {
        let output = folder.appendingPathComponent("fixture.epub")
        try EPUBEngine.write(text: "First line\nSecond line", title: "Test & title", output: output, directory: folder)
        let entries = try ArchiveEngine.readEntries(output)
        XCTAssertEqual(String(data: try XCTUnwrap(entries["mimetype"]), encoding: .utf8), "application/epub+zip")
        XCTAssertEqual(EPUBEngine.readingOrder(entries), ["OEBPS/chapter.xhtml"])
        let text = try await DocumentEngine.text(output, directory: folder)
        XCTAssertTrue(text.contains("First line")); XCTAssertTrue(text.contains("Second line"))
    }
    func testMediaAudioFormatsDecodeBackToWAV() async throws {
        let source = folder.appendingPathComponent("source.wav")
        try await ProcessRunner.run(EngineLocator.ffmpeg, arguments: ["-y", "-f", "lavfi", "-i", "sine=frequency=440:duration=0.15", "-c:a", "pcm_s16le", source.path], directory: folder)
        for format in ["mp3", "m4a", "flac", "ogg", "opus", "aiff"] {
            let output = folder.appendingPathComponent("sound.\(format)")
            try await MediaEngine.convert(source, to: output, format: format, category: .audio, directory: folder)
            let decoded = folder.appendingPathComponent("decoded-\(format).wav")
            try await MediaEngine.convert(output, to: decoded, format: "wav", category: .audio, directory: folder)
            XCTAssertEqual(try Data(contentsOf: decoded).prefix(4), Data("RIFF".utf8), format)
        }
    }
    func testMediaVideoContainersAndAudioExtraction() async throws {
        let source = folder.appendingPathComponent("source.mp4")
        try await ProcessRunner.run(EngineLocator.ffmpeg, arguments: ["-y", "-f", "lavfi", "-i", "color=c=blue:s=64x48:r=12:d=0.25", "-f", "lavfi", "-i", "sine=frequency=400:duration=0.25", "-c:v", "libx264", "-c:a", "aac", "-shortest", source.path], directory: folder)
        for format in ["mov", "mkv", "webm", "gif", "mp3"] {
            let output = folder.appendingPathComponent("video.\(format)")
            try await MediaEngine.convert(source, to: output, format: format, category: .video, directory: folder)
            let decoded = folder.appendingPathComponent("verify-\(format).\(format == "mp3" ? "wav" : "mp4")")
            try await MediaEngine.convert(output, to: decoded, format: format == "mp3" ? "wav" : "mp4", category: format == "mp3" ? .audio : .video, directory: folder)
            XCTAssertGreaterThan(try Data(contentsOf: decoded).count, 100, format)
        }
    }
    func testWebPAndAVIFActualDecoding() async throws {
        let svg = try file("shape.svg", "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"80\" height=\"40\"><rect width=\"80\" height=\"40\" fill=\"red\"/></svg>")
        let rendered = try ImageEngine.load(svg)
        XCTAssertEqual(rendered.width, 80)
        let png = folder.appendingPathComponent("source.png")
        try ImageEngine.write(rendered, to: png, format: "png", quality: 1)
        for format in ["webp", "avif"] {
            let output = try await ConversionEngine.run(input: png, category: .image, format: format, directory: folder)
            XCTAssertEqual(try ImageEngine.load(XCTUnwrap(output.first)).width, 80)
            let decoded = folder.appendingPathComponent("verify-\(format).png")
            try await ProcessRunner.run(EngineLocator.ffmpeg, arguments: ["-y", "-i", try XCTUnwrap(output.first).path, "-frames:v", "1", decoded.path], directory: folder)
            XCTAssertEqual(try ImageEngine.load(decoded).width, 80)
        }
    }
    func testLegacyAudioAndVideoInputOnlyFormats() async throws {
        let wma = folder.appendingPathComponent("legacy.wma")
        try await ProcessRunner.run(EngineLocator.ffmpeg, arguments: ["-y", "-f", "lavfi", "-i", "sine=duration=0.15", "-c:a", "wmav2", "-f", "asf", wma.path], directory: folder)
        let m4a = folder.appendingPathComponent("legacy.m4a")
        try await MediaEngine.convert(wma, to: m4a, format: "m4a", category: .audio, directory: folder)
        XCTAssertGreaterThan(try Data(contentsOf: m4a).count, 100)
        for (ext, codec) in [("avi", "mpeg4"), ("wmv", "wmv2")] {
            let source = folder.appendingPathComponent("legacy.\(ext)")
            try await ProcessRunner.run(EngineLocator.ffmpeg, arguments: ["-y", "-f", "lavfi", "-i", "color=c=red:s=64x48:r=12:d=0.25", "-c:v", codec, source.path], directory: folder)
            let mp4 = folder.appendingPathComponent("from-\(ext).mp4")
            try await MediaEngine.convert(source, to: mp4, format: "mp4", category: .video, directory: folder)
            XCTAssertGreaterThan(try Data(contentsOf: mp4).count, 100)
        }
    }
    func testExternalProcessCancellation() async throws {
        let work = try XCTUnwrap(folder)
        let start = Date()
        let task = Task {
            try await ProcessRunner.run(URL(fileURLWithPath: "/bin/sleep"), arguments: ["30"], directory: work)
        }
        try await Task.sleep(for: .milliseconds(150)); task.cancel()
        do { try await task.value; XCTFail("Cancelled worker must stop") } catch is CancellationError {} catch { XCTFail("Unexpected error: \(error)") }
        XCTAssertLessThan(Date().timeIntervalSince(start), 3)
        XCTAssertTrue(try FileManager.default.contentsOfDirectory(atPath: work.path).isEmpty)
    }
    func testRTFAndTextClippingInput() throws {
        let rtf = try file("rich.rtf", "{\\rtf1\\ansi Miracle rich text fixture.}")
        XCTAssertTrue(try RichTextReader.read(rtf).contains("Miracle rich text fixture."))
        let clipping = folder.appendingPathComponent("text.textClipping")
        let data = try PropertyListSerialization.data(fromPropertyList: ["UTI-Data": ["public.utf8-plain-text": Data("Clipped text".utf8)]], format: .binary, options: 0)
        try data.write(to: clipping)
        XCTAssertEqual(try RichTextReader.read(clipping), "Clipped text")
    }
    func testPDFPresentationImportWithTrimmedEngine() async throws {
        let svg = try file("presentation.svg", "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"80\" height=\"40\"><rect width=\"80\" height=\"40\" fill=\"red\"/></svg>")
        let png = folder.appendingPathComponent("presentation.png")
        try ImageEngine.write(ImageEngine.load(svg), to: png, format: "png", quality: 1)
        let pdf = folder.appendingPathComponent("presentation.pdf")
        try PDFEngine.create(images: [png], to: pdf)
        let converted = try await ConversionEngine.run(input: pdf, category: .pdf, format: "pptx", directory: folder)
        XCTAssertNotNil(try ArchiveEngine.readEntries(XCTUnwrap(converted.first))["ppt/slides/slide1.xml"])
    }
    func testOfficeDocumentsSpreadsheetsAndPDFExport() async throws {
        let text = try file("document.md", "# Miracle fixture\nA paragraph for document conversion.")
        let docx = try await ConversionEngine.run(input: text, category: .document, format: "docx", directory: folder)
        let contents = try await DocumentEngine.text(XCTUnwrap(docx.first), directory: folder)
        XCTAssertTrue(contents.contains("Miracle fixture"))
        let csv = try file("table.csv", "Name,Value\nAlpha,1\nBeta,2\n")
        let spreadsheet = try await ConversionEngine.run(input: csv, category: .spreadsheet, format: "xlsx", directory: folder)
        let legacySheet = try await DocumentEngine.office(XCTUnwrap(spreadsheet.first), format: "xls", directory: folder, filter: "xls:MS Excel 97")
        let reimportedSheet = try await ConversionEngine.run(input: legacySheet, category: .spreadsheet, format: "xlsx", directory: folder)
        XCTAssertFalse(reimportedSheet.isEmpty)
        for format in ["ods", "tsv", "pdf"] {
            let converted = try await ConversionEngine.run(input: XCTUnwrap(spreadsheet.first), category: .spreadsheet, format: format, directory: folder)
            if format == "pdf" { XCTAssertGreaterThan(try PDFEngine.open(XCTUnwrap(converted.first)).pageCount, 0) }
            else { XCTAssertGreaterThan(try Data(contentsOf: XCTUnwrap(converted.first)).count, 0) }
        }
        let pdfExports = try await ConversionEngine.run(input: XCTUnwrap(docx.first), category: .document, format: "pdf", directory: folder)
        let pdf = try XCTUnwrap(pdfExports.first)
        for format in ["docx", "pptx", "xlsx", "epub", "txt", "md", "jpg", "png"] {
            let converted = try await ConversionEngine.run(input: pdf, category: .pdf, format: format, directory: folder)
            XCTAssertFalse(converted.isEmpty, format)
            if ["jpg", "png"].contains(format) {
                let source = try XCTUnwrap(CGImageSourceCreateWithURL(XCTUnwrap(converted.first) as CFURL, nil))
                let properties = try XCTUnwrap(CGImageSourceCopyPropertiesAtIndex(source, 0, nil) as? [CFString: Any])
                XCTAssertEqual((properties[kCGImagePropertyDPIWidth] as? NSNumber)?.intValue, 300)
            }
            if format == "pptx" {
                let slides = try ArchiveEngine.readEntries(XCTUnwrap(converted.first))
                XCTAssertNotNil(slides["ppt/slides/slide1.xml"])
                let legacySlides = try await DocumentEngine.office(XCTUnwrap(converted.first), format: "ppt", directory: folder, filter: "ppt:MS PowerPoint 97")
                let reimportedSlides = try await ConversionEngine.run(input: legacySlides, category: .slides, format: "pdf", directory: folder)
                XCTAssertFalse(reimportedSlides.isEmpty)
                for slideExport in ["pdf", "png"] {
                    let result = try await ConversionEngine.run(input: XCTUnwrap(converted.first), category: .slides, format: slideExport, directory: folder)
                    XCTAssertFalse(result.isEmpty)
                }
            }
        }
    }
}
