import AppKit
import SwiftUI

@MainActor final class AppModel: ObservableObject {
    @Published var selected: [URL] = []
    @Published var mode: WheelMode = .convert
    @Published var paused = false
    @Published var running = false
    @Published var progress = 0.0
    @Published var message = "Ready when you are"
    @Published var detail = "Your files stay on your Mac."
    @Published var failed = false
    @Published var outputs: [URL] = []
    @Published var shelf: [URL] = []
    @Published var history: [HistoryEntry] = []
    @Published var pendingOption: FileOption?
    let settings: SettingsStore
    let queue = JobQueue()
    var historyStore: HistoryStore?
    var task: Task<Void, Never>?
    var wheelController: WheelController?
    var showHUD: (() -> Void)?
    var finishHUD: (() -> Void)?
    var dismissHUD: (() -> Void)?
    var showShelf: (() -> Void)?
    var pinShelf: (() -> Void)?
    var showTool: (() -> Void)?
    private var lastOption: FileOption?
    private var lastParameters = JobParameters()
    private var lastInputs: [URL] = []

    init(historyLocation: URL? = nil, settings: SettingsStore = SettingsStore()) {
        self.settings = settings
        do { historyStore = try HistoryStore(location: historyLocation); try refreshHistory() }
        catch { message = "History is unavailable"; detail = error.localizedDescription; failed = true }
    }
    var options: [FileOption] { CapabilityRegistry.options(for: selected, mode: mode) }
    func chooseFiles() {
        let panel = NSOpenPanel(); panel.allowsMultipleSelection = true; panel.canChooseDirectories = true
        panel.message = "Choose files to convert, edit, or use in a file action."
        guard panel.runModal() == .OK else { return }
        selected = panel.urls
        wheelController?.show(at: NSEvent.mouseLocation, drag: false)
    }
    func select(_ option: FileOption) {
        if let reason = option.unavailableReason { fail(AppError.unsupported(reason)); showHUD?(); return }
        wheelController?.hide()
        if option.needsPanel { pendingOption = option; showTool?(); return }
        run(option)
    }
    func run(_ option: FileOption, parameters: JobParameters? = nil) {
        guard !running, !selected.isEmpty else { return }
        var params = parameters ?? JobParameters(quality: settings.quality)
        params.outputFolder = settings.outputFolder
        lastOption = option; lastParameters = params; lastInputs = selected
        failed = false; outputs = []; message = option.title; detail = selected.count == 1 ? selected[0].lastPathComponent : "\(selected.count) files"
        progress = 0
        running = true
        showHUD?()
        if option.id.hasPrefix("action.") {
            let inputs = selected
            running = true
            task = Task {
                defer { running = false; finishHUD?() }
                do {
                    if option.kind == .float {
                        for url in inputs where !shelf.contains(url) { shelf.append(url) }
                        showShelf?()
                    } else { try await ActionService.perform(option.kind, urls: inputs, anchor: nil, terminalBundleIdentifier: settings.terminalApp) }
                    message = option.kind == .airDrop ? "AirDrop opened" : "\(option.title.replacingOccurrences(of: "…", with: "")) complete"
                    if option.kind == .airDrop { detail = "Choose a recipient in AirDrop to send your files." }
                    record(option, status: "succeeded", inputs: inputs)
                } catch is CancellationError { message = "Cancelled"; detail = "No action was taken." }
                catch { fail(error); record(option, status: "failed", inputs: inputs) }
            }
            return
        }
        running = true
        let inputs = selected
        task = Task {
            defer { running = false; finishHUD?() }
            do {
                outputs = try await queue.run(option: option, inputs: inputs, parameters: params) { value in
                    await MainActor.run { self.progress = value }
                }
                running = false; message = "All done"; detail = "\(outputs.count) file\(outputs.count == 1 ? "" : "s") saved. Originals kept."
                record(option, status: "succeeded", inputs: inputs)
            } catch is CancellationError {
                running = false; message = "Cancelled"; detail = "Created outputs were removed. Originals kept."
                record(option, status: "cancelled", inputs: inputs)
            } catch {
                running = false; fail(error); record(option, status: "failed", inputs: inputs)
            }
        }
    }
    func retry() { if let lastOption { selected = lastInputs; run(lastOption, parameters: lastParameters) } }
    func cancel() { task?.cancel() }
    func fail(_ error: Error) {
        failed = true; message = "Couldn’t finish"
        if let error = error as? AppError { detail = error.localizedDescription }
        else { detail = "The file could not be processed. Check that it is readable and the output folder is writable." }
    }
    func refreshHistory() throws { history = try historyStore?.recent() ?? [] }
    func clearHistory() {
        do { try historyStore?.clear(); try refreshHistory() } catch { fail(error) }
    }
    private func record(_ option: FileOption, status: String, inputs: [URL]) {
        guard settings.historyEnabled else { return }
        do {
            try historyStore?.add(operation: option.title, name: inputs.first?.lastPathComponent ?? "Files", status: status)
            try refreshHistory()
        } catch { detail += " History could not be saved." }
    }
}
