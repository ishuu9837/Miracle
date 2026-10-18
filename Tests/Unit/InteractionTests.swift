import XCTest
import SwiftUI
@testable import Miracle

final class InteractionTests: XCTestCase {
    func testDropTargetsFollowPointerForEveryWheelLayout() {
        for count in 1...6 {
            for index in 0..<count {
                let angle = Double(index) / Double(count) * 2 * .pi - .pi / 2
                let point = CGPoint(x: 170 + cos(angle) * 112, y: 170 + sin(angle) * 112)
                XCTAssertEqual(WheelHitTesting.index(at: point, count: count), index)
            }
            XCTAssertNil(WheelHitTesting.index(at: CGPoint(x: 170, y: 170), count: count), "The hub must never trigger an operation")
            XCTAssertNil(WheelHitTesting.index(at: .zero, count: count))
            XCTAssertNil(WheelHitTesting.index(at: CGPoint(x: 500, y: 500), count: count))
        }
    }
    func testResizeDropIsNotRotateAndGapsDoNotFallBack() {
        XCTAssertEqual(WheelHitTesting.index(at: CGPoint(x: 170, y: 58), count: 3), 0)
        XCTAssertEqual(WheelHitTesting.index(at: CGPoint(x: 267, y: 226), count: 3), 1)
        XCTAssertEqual(WheelHitTesting.index(at: CGPoint(x: 73, y: 226), count: 3), 2)
        XCTAssertNil(WheelHitTesting.index(at: CGPoint(x: 170, y: 330), count: 3))
    }
    @MainActor func testCompletionWindowDismissesAndNewJobCancelsOldDismissal() async throws {
        _ = NSApplication.shared
        let app = AppDelegate()
        app.resultDismissDelay = .milliseconds(40)
        app.model.running = true
        app.openHUD()
        XCTAssertTrue(app.resultVisible)
        XCTAssertEqual(app.resultWindowLevel, .floating)
        app.model.running = false
        app.completeHUD()
        XCTAssertEqual(app.resultWindowLevel, .normal)
        try await Task.sleep(for: .milliseconds(100))
        XCTAssertFalse(app.resultVisible, "A completed result must not stick above other apps")
        app.openHUD(); app.completeHUD()
        app.model.running = true
        app.openHUD()
        try await Task.sleep(for: .milliseconds(100))
        XCTAssertTrue(app.resultVisible, "An older completion must not dismiss a new job")
        app.dismissHUD()
        XCTAssertFalse(app.resultVisible)
    }
    @MainActor func testJobShowsRunningBeforeHUDAndFinishesOnce() async throws {
        let folder = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: folder) }
        let input = folder.appendingPathComponent("input.txt")
        try "Fixture paragraph".write(to: input, atomically: true, encoding: .utf8)
        let suite = "miracle-interaction-\(UUID().uuidString)"
        let defaults = try XCTUnwrap(UserDefaults(suiteName: suite))
        defer { defaults.removePersistentDomain(forName: suite) }
        let model = AppModel(historyLocation: folder.appendingPathComponent("history"), settings: SettingsStore(defaults: defaults))
        model.settings.outputFolder = folder
        model.selected = [input]
        var shown = 0, finished = 0
        model.showHUD = { shown += 1; XCTAssertTrue(model.running) }
        model.finishHUD = { finished += 1; XCTAssertFalse(model.running) }
        model.run(.init(id: "document.convert.md", title: "MD", symbol: "doc", kind: .convert(.document, "md")))
        await model.task?.value
        XCTAssertEqual(shown, 1); XCTAssertEqual(finished, 1)
        XCTAssertFalse(model.failed); XCTAssertEqual(model.outputs.count, 1)
        XCTAssertEqual(try String(contentsOf: input, encoding: .utf8), "Fixture paragraph")
        model.selected = [folder.appendingPathComponent("missing.txt")]
        model.run(.init(id: "document.convert.md", title: "MD", symbol: "doc", kind: .convert(.document, "md")))
        await model.task?.value
        XCTAssertEqual(finished, 2); XCTAssertTrue(model.failed)
        XCTAssertTrue(model.outputs.isEmpty, "A failed job must not keep the previous result's Open button")
        model.settings.outputFolder = nil
    }
}
