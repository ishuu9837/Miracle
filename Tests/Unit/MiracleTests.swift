import XCTest
import CoreGraphics
import ImageIO
import PDFKit
import CoreText
import UniformTypeIdentifiers
@testable import Miracle

final class MiracleTests: XCTestCase {
    var folder: URL!
    override func setUpWithError() throws {
        folder = FileManager.default.temporaryDirectory.appendingPathComponent("miracle-test-\(UUID().uuidString)")
        try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
    }
    override func tearDownWithError() throws { try FileManager.default.removeItem(at: folder) }
    func fixture(name: String = "source.png") throws -> URL {
        let context = try XCTUnwrap(CGContext(data: nil, width: 80, height: 40, bitsPerComponent: 8, bytesPerRow: 0,
                                             space: CGColorSpaceCreateDeviceRGB(), bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue))
        context.setFillColor(CGColor(red: 0.2, green: 0.5, blue: 0.8, alpha: 1))
        context.fill(CGRect(x: 0, y: 0, width: 80, height: 40))
        let url = folder.appendingPathComponent(name)
        try ImageEngine.write(XCTUnwrap(context.makeImage()), to: url, format: "png", quality: 1)
        return url
    }
    func option(_ kind: OperationKind) -> FileOption { .init(id: "test", title: "Test", symbol: "photo", kind: kind) }
    func testWheelSectorHitAreaExcludesHubAndEmptyLayouts() {
        let sector = WheelSector(index: 0, count: 6)
        XCTAssertTrue(sector.path(in: .zero).isEmpty)
        let path = sector.path(in: CGRect(x: 0, y: 0, width: 340, height: 340))
        XCTAssertTrue(path.contains(CGPoint(x: 170, y: 50)))
        XCTAssertFalse(path.contains(CGPoint(x: 170, y: 170)))
        XCTAssertFalse(path.contains(CGPoint(x: 170, y: 300)))
    }
    func testRegistryFiltersMixedAndUnsupportedInputs() throws {
        let image = try fixture()
        let text = folder.appendingPathComponent("text.txt"); try "hello".write(to: text, atomically: true, encoding: .utf8)
        XCTAssertFalse(CapabilityRegistry.options(for: [image], mode: .convert).isEmpty, "Extension: \(image.pathExtension), resolved: \(String(describing: UTType(filenameExtension: image.pathExtension))), png conforms: \(UTType.png.conforms(to: .image))")
        XCTAssertTrue(CapabilityRegistry.options(for: [image, text], mode: .convert).isEmpty)
        XCTAssertTrue(CapabilityRegistry.options(for: [text], mode: .tools).isEmpty)
        XCTAssertEqual(CapabilityRegistry.options(for: [text], mode: .actions).count, 7)
        XCTAssertTrue(CapabilityRegistry.options(for: [], mode: .actions).isEmpty)
    }
    func testConversionKeepsOriginalAndAvoidsCollisions() async throws {
        let source = try fixture()
        let original = try Data(contentsOf: source)
        let queue = JobQueue()
        let first = try await queue.run(option: option(.image("jpeg")), inputs: [source], parameters: .init()) { _ in }
        let second = try await queue.run(option: option(.image("jpeg")), inputs: [source], parameters: .init()) { _ in }
        XCTAssertNotEqual(first, second)
        XCTAssertEqual(try Data(contentsOf: source), original)
        let decoded = try ImageEngine.load(XCTUnwrap(first.first))
        XCTAssertEqual(decoded.width, 80); XCTAssertEqual(decoded.height, 40)
    }
    func testImageTools() async throws {
        let source = try fixture()
        let queue = JobQueue()
        for kind in [OperationKind.resize, .compress, .rotate] {
            let result = try await queue.run(option: option(kind), inputs: [source], parameters: .init(quality: 0.5, maxDimension: 32)) { _ in }
            let image = try ImageEngine.load(XCTUnwrap(result.first))
            if kind == .resize { XCTAssertEqual(image.width, 32); XCTAssertEqual(image.height, 16) }
            if kind == .rotate { XCTAssertEqual(image.width, 40); XCTAssertEqual(image.height, 80) }
        }
    }
    func testPDFRoundTripAndSplit() async throws {
        let a = try fixture(name: "a.png"), b = try fixture(name: "b.png")
        let queue = JobQueue()
        let created = try await queue.run(option: option(.imagePDF), inputs: [a, b], parameters: .init()) { _ in }
        let pdf = try XCTUnwrap(created.first)
        XCTAssertEqual(try PDFEngine.open(pdf).pageCount, 2)
        for kind in [OperationKind.pdfSplit, .pdfImages] {
            let results = try await queue.run(option: option(kind), inputs: [pdf], parameters: .init()) { _ in }
            XCTAssertEqual(results.count, 2)
            if kind == .pdfSplit { XCTAssertEqual(try PDFEngine.open(results[0]).pageCount, 1) }
            else { XCTAssertEqual(try ImageEngine.load(results[0]).width, 333) }
        }
    }
    func testBatchFailureRollsBackOutputs() async throws {
        let good = try fixture()
        let bad = folder.appendingPathComponent("broken.png"); try Data("invalid".utf8).write(to: bad)
        do {
            _ = try await JobQueue().run(option: option(.image("jpeg")), inputs: [good, bad], parameters: .init()) { _ in }
            XCTFail("Invalid image must fail")
        } catch { }
        XCTAssertEqual(try FileManager.default.contentsOfDirectory(atPath: folder.path).sorted(), ["broken.png", "source.png"])
    }
    func testCancellationPublishesNothing() async throws {
        let source = try fixture()
        let queue = JobQueue()
        let operation = option(.image("jpeg"))
        let task = Task {
            withUnsafeCurrentTask { $0?.cancel() }
            return try await queue.run(option: operation, inputs: [source], parameters: .init()) { _ in }
        }
        do { _ = try await task.value; XCTFail("Cancelled task must fail") } catch is CancellationError {} catch { XCTFail("Unexpected error: \(error)") }
        XCTAssertEqual(try FileManager.default.contentsOfDirectory(atPath: folder.path), ["source.png"])
    }
    func testMissingInputFailsBeforeOutput() async throws {
        do {
            _ = try await JobQueue().run(option: option(.image("png")), inputs: [folder.appendingPathComponent("missing.png")], parameters: .init()) { _ in }
            XCTFail("Missing file must fail")
        } catch { XCTAssertTrue(error is AppError) }
        XCTAssertTrue(try FileManager.default.contentsOfDirectory(atPath: folder.path).isEmpty)
    }
    func testMidBatchCancellationRemovesPublishedFiles() async throws {
        let source = try fixture()
        do {
            _ = try await JobQueue().run(option: option(.image("jpeg")), inputs: [source, source], parameters: .init()) { value in
                if value >= 0.5 { withUnsafeCurrentTask { $0?.cancel() } }
            }
            XCTFail("Cancellation during progress must fail")
        } catch is CancellationError {} catch { XCTFail("Unexpected error: \(error)") }
        XCTAssertEqual(try FileManager.default.contentsOfDirectory(atPath: folder.path), ["source.png"])
    }
    func testInvalidParametersDoNotWriteOutputs() async throws {
        let source = try fixture()
        do {
            _ = try await JobQueue().run(option: option(.resize), inputs: [source], parameters: .init(maxDimension: -1)) { _ in }
            XCTFail("Invalid dimensions must fail")
        } catch { XCTAssertTrue(error is AppError) }
        XCTAssertEqual(try FileManager.default.contentsOfDirectory(atPath: folder.path), ["source.png"])
    }
    func testTextPDFExtractionAndScannedPDFError() async throws {
        let pdf = folder.appendingPathComponent("text.pdf")
        guard let context = CGContext(pdf as CFURL, mediaBox: nil, nil) else { return XCTFail("Cannot create fixture") }
        context.beginPDFPage(nil)
        let line = CTLineCreateWithAttributedString(NSAttributedString(string: "Miracle fixture text", attributes: [.font: CTFontCreateWithName("Helvetica" as CFString, 14, nil)]))
        context.textPosition = CGPoint(x: 30, y: 300); CTLineDraw(line, context)
        context.endPDFPage(); context.closePDF()
        let queue = JobQueue()
        let result = try await queue.run(option: option(.pdfText), inputs: [pdf], parameters: .init()) { _ in }
        XCTAssertTrue(try String(contentsOf: XCTUnwrap(result.first), encoding: .utf8).contains("Miracle fixture text"))
        let source = try fixture()
        let scanned = try await queue.run(option: option(.imagePDF), inputs: [source], parameters: .init()) { _ in }
        do {
            _ = try await queue.run(option: option(.pdfText), inputs: scanned, parameters: .init()) { _ in }
            XCTFail("Scanned PDF must report no selectable text")
        } catch { XCTAssertTrue(error is AppError) }
    }
    @MainActor func testHistoryIsCappedClearableAndStoresNamesOnly() throws {
        let store = try HistoryStore(location: folder)
        for i in 0..<55 { try store.add(operation: "PNG", name: "fixture-\(i).png", status: "succeeded") }
        XCTAssertEqual(try store.recent().count, 50)
        XCTAssertFalse(try store.recent().contains { $0.name.contains("/") })
        try store.clear(); XCTAssertTrue(try store.recent().isEmpty)
    }
    @MainActor func testSettingsValidateQualityAndPersist() throws {
        let suite = "miracle-tests-\(UUID().uuidString)"
        let defaults = try XCTUnwrap(UserDefaults(suiteName: suite))
        defer { defaults.removePersistentDomain(forName: suite) }
        defaults.set(5, forKey: "defaults.image.quality")
        let store = SettingsStore(defaults: defaults)
        XCTAssertEqual(store.quality, 0.8)
        store.historyEnabled = false
        XCTAssertFalse(SettingsStore(defaults: defaults).historyEnabled)
    }
}
