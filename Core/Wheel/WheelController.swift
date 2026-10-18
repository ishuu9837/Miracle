import AppKit
import SwiftUI

final class WheelPanel: NSPanel {
    override var canBecomeKey: Bool { true }
    override func cancelOperation(_ sender: Any?) { orderOut(nil) }
}

@MainActor final class WheelController {
    private var panel: WheelPanel?
    private let model: AppModel
    init(model: AppModel) { self.model = model }
    var visible: Bool { panel?.isVisible == true }
    func show(at point: NSPoint, drag: Bool) {
        guard !model.paused, !model.running else { return }
        hide()
        let size = NSSize(width: DesignTokens.wheelSize, height: 570)
        let screen = NSScreen.screens.first(where: { $0.frame.contains(point) }) ?? NSScreen.main
        let frame = screen?.visibleFrame ?? NSRect(x: 0, y: 0, width: 1200, height: 800)
        let origin = NSPoint(x: max(frame.minX + 8, min(point.x - size.width / 2, frame.maxX - size.width - 8)),
                             y: max(frame.minY + 8, min(point.y - size.height / 2, frame.maxY - size.height - 8)))
        let panel = WheelPanel(contentRect: NSRect(origin: origin, size: size),
                               styleMask: [.borderless, .nonactivatingPanel], backing: .buffered, defer: false)
        panel.isOpaque = false; panel.backgroundColor = .clear; panel.hasShadow = true
        panel.level = .popUpMenu
        panel.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary]
        panel.contentView = NSHostingView(rootView: WheelView(model: model, dismiss: { [weak self] in self?.hide() }))
        self.panel = panel
        if drag { panel.orderFrontRegardless() }
        else { NSApp.activate(ignoringOtherApps: true); panel.makeKeyAndOrderFront(nil) }
    }
    func hide() { panel?.orderOut(nil); panel = nil }
}
