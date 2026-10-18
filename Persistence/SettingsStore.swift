import Foundation
import Combine

@MainActor final class SettingsStore: ObservableObject {
    var hasOpenedBefore: Bool {
        get { defaults.bool(forKey: "onboarding.hasOpened") }
        set { defaults.set(newValue, forKey: "onboarding.hasOpened") }
    }
    @Published var historyEnabled: Bool { didSet { defaults.set(historyEnabled, forKey: "history.enabled") } }
    @Published var quality: Double { didSet { defaults.set(quality, forKey: "defaults.image.quality") } }
    @Published var terminalApp: String { didSet { defaults.set(terminalApp, forKey: "terminal.app") } }
    @Published var outputFolder: URL? {
        didSet {
            defaults.set(try? outputFolder?.bookmarkData(options: .minimalBookmark), forKey: "output.folderBookmark")
        }
    }
    private let defaults: UserDefaults
    init(defaults: UserDefaults = .standard) {
        self.defaults = defaults
        terminalApp = defaults.string(forKey: "terminal.app") == "com.googlecode.iterm2" ? "com.googlecode.iterm2" : "com.apple.Terminal"
        historyEnabled = defaults.object(forKey: "history.enabled") as? Bool ?? true
        let saved = defaults.object(forKey: "defaults.image.quality") as? Double ?? 0.8
        quality = (0.1...1).contains(saved) ? saved : 0.8
        if let data = defaults.data(forKey: "output.folderBookmark") {
            var stale = false
            outputFolder = try? URL(resolvingBookmarkData: data, options: .withoutUI, bookmarkDataIsStale: &stale)
        }
    }
}
