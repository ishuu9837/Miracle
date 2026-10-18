import AppKit

@MainActor enum PagesService {
    /// Native .pages files are created by Apple's installed Pages app, never by changing an extension.
    static func convert(_ input: URL, to output: URL) throws {
        guard EngineLocator.pagesAvailable else { throw AppError.unsupported("Install Apple Pages to export native Pages documents.") }
        func literal(_ path: String) -> String {
            "\"" + path.replacingOccurrences(of: "\\", with: "\\\\").replacingOccurrences(of: "\"", with: "\\\"") + "\""
        }
        let source = """
        tell application id "com.apple.iWork.Pages"
            set convertedDocument to open POSIX file \(literal(input.path))
            try
                save convertedDocument in POSIX file \(literal(output.path))
                close convertedDocument saving no
            on error errorText number errorNumber
                close convertedDocument saving no
                error errorText number errorNumber
            end try
        end tell
        """
        var error: NSDictionary?
        guard let script = NSAppleScript(source: source) else { throw AppError.engine("Pages integration could not start.") }
        script.executeAndReturnError(&error)
        if error != nil { throw AppError.permission("Pages could not save the document. Allow Miracle to control Pages in System Settings → Privacy & Security → Automation.") }
    }
}
