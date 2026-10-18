import SwiftUI
import AppKit

@main struct MiracleApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) private var delegate
    var body: some Scene { Settings { EmptyView() } }
}

@MainActor final class AppDelegate: NSObject, NSApplicationDelegate {
    let model = AppModel()
    private var statusItem: NSStatusItem?
    private var gesture: GestureEngine?
    private var windows: [String: NSWindow] = [:]
    private var hudDismissal: Task<Void, Never>?
    private var hudGeneration = 0
    var resultDismissDelay: Duration = .seconds(4)
    var resultVisible: Bool { windows["hud"]?.isVisible == true }
    var resultWindowLevel: NSWindow.Level? { windows["hud"]?.level }

    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.setActivationPolicy(.accessory)
        let wheel = WheelController(model: model); model.wheelController = wheel
        gesture = GestureEngine(model: model); gesture?.start()
        model.showHUD = { [weak self] in self?.openHUD() }
        model.finishHUD = { [weak self] in self?.completeHUD() }
        model.dismissHUD = { [weak self] in self?.dismissHUD() }
        model.showShelf = { [weak self] in self?.openShelf() }
        model.pinShelf = { [weak self] in
            guard let self, let shelf = self.windows["shelf"], let frame = NSScreen.main?.visibleFrame else { return }
            shelf.setFrameOrigin(NSPoint(x: frame.maxX - shelf.frame.width - 12, y: frame.midY - shelf.frame.height / 2))
        }
        model.showTool = { [weak self] in self?.openTool() }
        setupMenu()
        if !model.settings.hasOpenedBefore && !CommandLine.arguments.contains("--background") {
            openWelcome(); model.settings.hasOpenedBefore = true
        }
    }
    func applicationShouldTerminate(_ sender: NSApplication) -> NSApplication.TerminateReply {
        guard model.running else { return .terminateNow }
        model.cancel()
        Task { [model] in
            await model.task?.value
            sender.reply(toApplicationShouldTerminate: true)
        }
        return .terminateLater
    }
    func applicationWillTerminate(_ notification: Notification) { gesture?.stop(); hudDismissal?.cancel() }
    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows flag: Bool) -> Bool {
        if !flag { openWelcome() }
        return true
    }
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { false }
    private func setupMenu() {
        let item = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
        item.button?.image = NSImage(systemSymbolName: "circle.hexagongrid", accessibilityDescription: "miracle file converter")
        item.button?.toolTip = "miracle — offline file tools"
        let menu = NSMenu()
        func add(_ title: String, _ selector: Selector, key: String = "") {
            let row = NSMenuItem(title: title, action: selector, keyEquivalent: key); row.target = self; menu.addItem(row)
        }
        add("miracle", #selector(welcome))
        menu.addItem(.separator())
        add("Choose File…", #selector(choose), key: "o")
        add("Float Shelf", #selector(shelf))
        add("Recent Jobs", #selector(history))
        add("Last Result…", #selector(lastResult))
        menu.addItem(.separator())
        add("Pause / Resume Shortcuts", #selector(pause))
        add("Settings…", #selector(settings), key: ",")
        menu.addItem(.separator())
        add("Quit miracle", #selector(quit), key: "q")
        item.menu = menu; statusItem = item
        let mainMenu = NSMenu()
        let appItem = NSMenuItem(); appItem.submenu = menu.copy() as? NSMenu; mainMenu.addItem(appItem)
        let edit = NSMenuItem(title: "Edit", action: nil, keyEquivalent: "")
        let editMenu = NSMenu(title: "Edit")
        for (title, action, key) in [("Cut", #selector(NSText.cut(_:)), "x"), ("Copy", #selector(NSText.copy(_:)), "c"), ("Paste", #selector(NSText.paste(_:)), "v"), ("Select All", #selector(NSText.selectAll(_:)), "a")] {
            editMenu.addItem(withTitle: title, action: action, keyEquivalent: key)
        }
        edit.submenu = editMenu; mainMenu.addItem(edit); NSApp.mainMenu = mainMenu
    }
    private func present<V: View>(_ id: String, title: String, floating: Bool = false, activate: Bool = true, @ViewBuilder content: () -> V) {
        if let window = windows[id] {
            if activate { NSApp.activate(ignoringOtherApps: true); window.makeKeyAndOrderFront(nil) } else { window.orderFrontRegardless() }
            return
        }
        let window = NSWindow(contentRect: .zero, styleMask: [.titled, .closable, .miniaturizable], backing: .buffered, defer: false)
        window.title = title; window.isReleasedWhenClosed = false
        window.contentView = NSHostingView(rootView: content())
        window.center()
        if floating { window.level = .floating; window.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary] }
        windows[id] = window
        if activate { NSApp.activate(ignoringOtherApps: true); window.makeKeyAndOrderFront(nil) } else { window.orderFrontRegardless() }
    }
    func openWelcome() { present("welcome", title: "miracle") { WelcomeView(model: model, settings: openSettings, shelf: openShelf) } }
    func openSettings() { present("settings", title: "miracle Settings") { SettingsView(model: model, settings: model.settings) } }
    func openShelf() { present("shelf", title: "Float Shelf", floating: true) { ShelfView(model: model) } }
    func openHUD() {
        hudDismissal?.cancel(); hudGeneration += 1
        if windows["hud"] == nil {
            let panel = WheelPanel(contentRect: .zero, styleMask: [.titled, .closable, .nonactivatingPanel], backing: .buffered, defer: false)
            panel.title = "miracle"; panel.isReleasedWhenClosed = false
            panel.contentView = NSHostingView(rootView: ProgressHUD(model: model))
            panel.center(); windows["hud"] = panel
        }
        windows["hud"]?.level = model.running ? .floating : .normal
        windows["hud"]?.orderFrontRegardless()
    }
    func dismissHUD() {
        hudDismissal?.cancel(); hudGeneration += 1
        windows["hud"]?.orderOut(nil)
    }
    func completeHUD() {
        windows["hud"]?.level = .normal
        hudDismissal?.cancel()
        guard !model.failed else { return }
        let generation = hudGeneration
        hudDismissal = Task { [weak self] in
            try? await Task.sleep(for: self?.resultDismissDelay ?? .seconds(4))
            guard !Task.isCancelled, let self, self.hudGeneration == generation, !self.model.running else { return }
            self.windows["hud"]?.orderOut(nil)
        }
    }
    func openTool() {
        // Replace the content so each tool starts with fresh parameter state.
        windows["tool"]?.close(); windows.removeValue(forKey: "tool")
        present("tool", title: model.pendingOption?.title ?? "Edit image") {
            ToolPanel(model: model, close: { [weak self] in self?.windows["tool"]?.close() })
        }
    }
    @objc private func lastResult() { openHUD() }
    @objc private func welcome() { openWelcome() }
    @objc private func choose() { model.chooseFiles() }
    @objc private func shelf() { openShelf() }
    @objc private func settings() { openSettings() }
    @objc private func history() { present("history", title: "Recent Jobs") { HistoryView(model: model) } }
    @objc private func pause() { model.paused.toggle(); statusItem?.button?.appearsDisabled = model.paused }
    @objc private func quit() { NSApp.terminate(nil) }
}
