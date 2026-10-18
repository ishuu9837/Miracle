import AppKit

@MainActor enum ActionService {
    static func perform(_ kind: OperationKind, urls: [URL], anchor: NSView?, terminalBundleIdentifier: String = "com.apple.Terminal") async throws {
        switch kind {
        case .copy:
            NSPasteboard.general.clearContents()
            guard NSPasteboard.general.writeObjects(urls as [NSURL]) else { throw AppError.engine("The files could not be copied to the clipboard.") }
        case .copyPath:
            NSPasteboard.general.clearContents()
            NSPasteboard.general.setString(urls.map(\.path).joined(separator: "\n"), forType: .string)
        case .reveal: NSWorkspace.shared.activateFileViewerSelecting(urls)
        case .airDrop:
            guard let service = NSSharingService(named: .sendViaAirDrop) else { throw AppError.unsupported("AirDrop is not available on this Mac.") }
            service.perform(withItems: urls)
        case .terminal:
            guard let url = urls.first else { return }
            let configuration = NSWorkspace.OpenConfiguration()
            guard let appURL = NSWorkspace.shared.urlForApplication(withBundleIdentifier: terminalBundleIdentifier) else { throw AppError.unsupported("Your selected terminal app is not installed. Choose Terminal in Miracle Settings.") }
            let directory = (try? url.resourceValues(forKeys: [.isDirectoryKey]).isDirectory) == true ? url : url.deletingLastPathComponent()
            try await withCheckedThrowingContinuation { (continuation: CheckedContinuation<Void, Error>) in
                NSWorkspace.shared.open([directory], withApplicationAt: appURL, configuration: configuration) { _, error in
                    if error != nil { continuation.resume(throwing: AppError.engine("Terminal could not be opened. Open the folder in Finder and try again.")) }
                    else { continuation.resume() }
                }
            }
        case .move:
            let picker = NSOpenPanel(); picker.canChooseDirectories = true; picker.canChooseFiles = false
            picker.prompt = "Choose destination"
            guard picker.runModal() == .OK, let folder = picker.url else { throw CancellationError() }
            let alert = NSAlert()
            alert.messageText = "Move \(urls.count) file\(urls.count == 1 ? "" : "s") to \(folder.lastPathComponent)?"
            alert.informativeText = "The original files will be removed from their current locations."
            alert.addButton(withTitle: "Move"); alert.addButton(withTitle: "Cancel")
            guard alert.runModal() == .alertFirstButtonReturn else { throw CancellationError() }
            // Preflight all destinations before moving anything.
            let destinations = urls.map { folder.appendingPathComponent($0.lastPathComponent) }
            guard Set(destinations).count == destinations.count,
                  destinations.allSatisfy({ !FileManager.default.fileExists(atPath: $0.path) }) else {
                throw AppError.input("A file with the same name already exists in that folder. Choose another destination.")
            }
            var moved: [(URL, URL)] = []
            do {
                for (source, destination) in zip(urls, destinations) {
                    try FileManager.default.moveItem(at: source, to: destination)
                    moved.append((source, destination))
                }
            } catch {
                var rollbackFailed = false
                for (source, destination) in moved.reversed() {
                    do { try FileManager.default.moveItem(at: destination, to: source) } catch { rollbackFailed = true }
                }
                throw AppError.permission(rollbackFailed ? "The move stopped. Some files remain in the destination folder; check both locations." : "The files could not be moved. Check folder permissions and try again.")
            }
        default: throw AppError.unsupported("This file action is unavailable.")
        }
    }
}
