import Foundation

struct EngineLocator {
    static let architecture = "arm64"
    static var root: URL {
        let bundled = (Bundle.main.resourceURL ?? Bundle.main.bundleURL).appendingPathComponent("Engines")
        if FileManager.default.fileExists(atPath: bundled.path) { return bundled }
        #if DEBUG
        return URL(fileURLWithPath: #filePath).deletingLastPathComponent().deletingLastPathComponent().appendingPathComponent("Resources/Engines")
        #else
        return bundled
        #endif
    }
    static var ffmpeg: URL { root.appendingPathComponent("\(architecture)/ffmpeg") }
    static var office: URL {
        return root.appendingPathComponent("\(architecture)/LibreOffice.app/Contents/MacOS/soffice")
    }
    static var pagesAvailable: Bool {
        ["/Applications/Pages.app", NSHomeDirectory() + "/Applications/Pages.app"].contains { FileManager.default.fileExists(atPath: $0) }
    }
}
