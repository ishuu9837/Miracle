import AppKit

@MainActor enum FinderSelection {
    static func read() throws -> [URL] {
        guard NSWorkspace.shared.frontmostApplication?.bundleIdentifier == "com.apple.finder" else { return [] }
        return try query()
    }
    static func requestAccess() throws {
        _ = try query()
    }
    private static func query() throws -> [URL] {
        let source = """
        tell application id "com.apple.finder"
            set filePaths to {}
            repeat with selectedItem in (get selection)
                set end of filePaths to POSIX path of (selectedItem as alias)
            end repeat
            return filePaths
        end tell
        """
        guard let script = NSAppleScript(source: source) else { throw AppError.engine("The Finder shortcut could not start.") }
        var error: NSDictionary?
        let result = script.executeAndReturnError(&error)
        if let error {
            let code = (error[NSAppleScript.errorNumber] as? NSNumber)?.intValue
            if code == -1743 { throw AppError.permission("Allow miracle to control Finder in System Settings → Privacy & Security → Automation, then select a file and tap Shift again.") }
            throw AppError.permission("miracle could not read Finder’s selection. Select a local file in Finder and try again.")
        }
        guard result.numberOfItems > 0 else { return [] }
        return (1...result.numberOfItems).compactMap { index in
            guard let path = result.atIndex(index)?.stringValue, path.hasPrefix("/") else { return nil }
            return URL(fileURLWithPath: path)
        }
    }
}
