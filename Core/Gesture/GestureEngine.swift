import AppKit

/// macOS has no public global file-drag API. Observe the drag pasteboard only while a mouse drag is active.
@MainActor final class GestureEngine {
    private var timer: Timer?
    private var monitor: Any?
    private var localMonitor: Any?
    private var dragActive = false
    private var lastFlags: NSEvent.ModifierFlags = []
    private var dragChangeCount = NSPasteboard(name: .drag).changeCount
    private var lastMouseDown = false
    private let model: AppModel
    init(model: AppModel) { self.model = model }
    func start() {
        timer = Timer.scheduledTimer(withTimeInterval: 0.06, repeats: true) { [weak self] _ in
            Task { @MainActor in self?.poll() }
        }
        monitor = NSEvent.addGlobalMonitorForEvents(matching: [.keyDown]) { [weak self] event in
            if event.keyCode == 53 { Task { @MainActor in self?.model.wheelController?.hide() } }
        }
        localMonitor = NSEvent.addLocalMonitorForEvents(matching: [.keyDown]) { [weak self] event in
            if event.keyCode == 53, self?.model.wheelController?.visible == true {
                self?.model.wheelController?.hide(); return nil
            }
            return event
        }
    }
    func stop() {
        timer?.invalidate(); timer = nil
        if let monitor { NSEvent.removeMonitor(monitor) }
        if let localMonitor { NSEvent.removeMonitor(localMonitor) }
    }
    private func poll() {
        let mouseDown = NSEvent.pressedMouseButtons & 1 != 0
        let board = NSPasteboard(name: .drag)
        let flags = NSEvent.modifierFlags.intersection([.shift, .command, .control])
        defer { lastFlags = flags; lastMouseDown = mouseDown }
        if !mouseDown {
            if flags == .shift, !lastFlags.contains(.shift), !model.paused, !model.running,
               NSWorkspace.shared.frontmostApplication?.bundleIdentifier == "com.apple.finder" {
                do {
                    let urls = try FinderSelection.read()
                    if !urls.isEmpty {
                        model.selected = urls; model.mode = .convert
                        model.wheelController?.show(at: NSEvent.mouseLocation, drag: false)
                    }
                } catch { model.fail(error); model.showHUD?() }
            }
            if lastMouseDown && dragActive {
                // Allow the destination's drop callback to run first.
                DispatchQueue.main.asyncAfter(deadline: .now() + 0.3) { [weak self] in self?.model.wheelController?.hide() }
            }
            dragActive = false; dragChangeCount = board.changeCount
            return
        }
        if board.changeCount != dragChangeCount {
            dragChangeCount = board.changeCount
            if let urls = board.readObjects(forClasses: [NSURL.self], options: [.urlReadingFileURLsOnly: true]) as? [URL], !urls.isEmpty {
                model.selected = urls; dragActive = true
            }
        }
        if !dragActive, flags == .shift, !lastFlags.contains(.shift), !model.paused, !model.running,
           NSWorkspace.shared.frontmostApplication?.bundleIdentifier == "com.apple.finder" {
            do {
                let urls = try FinderSelection.read()
                if !urls.isEmpty {
                    model.selected = urls; model.mode = .convert
                    model.wheelController?.show(at: NSEvent.mouseLocation, drag: false)
                }
            } catch { model.fail(error); model.showHUD?() }
        }
        guard dragActive, flags != lastFlags, !model.paused else { return }
        let mode: WheelMode?
        if flags.contains(.control) { mode = .actions }
        else if flags.contains(.command) { mode = .tools }
        else if flags.contains(.shift) { mode = .convert }
        else { mode = nil }
        if let mode {
            model.mode = mode
            if model.wheelController?.visible != true { model.wheelController?.show(at: NSEvent.mouseLocation, drag: true) }
        }
    }
}
