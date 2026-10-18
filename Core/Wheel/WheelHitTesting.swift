import SwiftUI

/// One coordinate-based hit test is shared by hover and drop. Each sector's
/// SwiftUI layout fills the same square, so separate drop modifiers overlap.
enum WheelHitTesting {
    static let size: CGFloat = 340
    static func index(at point: CGPoint, count: Int) -> Int? {
        guard count > 0, count <= 6 else { return nil }
        let bounds = CGRect(x: 0, y: 0, width: size, height: size)
        return (0..<count).first { WheelSector(index: $0, count: count).path(in: bounds).contains(point) }
    }
}

@MainActor struct WheelDropDelegate: DropDelegate {
    let options: [FileOption]
    @Binding var hovered: String?
    let model: AppModel
    func validateDrop(info: DropInfo) -> Bool { info.hasItemsConforming(to: [.fileURL]) }
    func dropEntered(info: DropInfo) { update(info.location) }
    func dropExited(info: DropInfo) { hovered = nil }
    func dropUpdated(info: DropInfo) -> DropProposal? {
        update(info.location)
        return DropProposal(operation: hovered == nil ? .forbidden : .copy)
    }
    func performDrop(info: DropInfo) -> Bool {
        // Never use the hover label to decide the operation: it may be stale
        // after a mode switch or after macOS sends overlapping hover callbacks.
        guard let index = WheelHitTesting.index(at: info.location, count: options.count) else { return false }
        let option = options[index]
        hovered = option.id
        receiveFiles(info.itemProviders(for: [.fileURL])) { urls in
            model.selected = urls
            model.select(option)
        }
        return true
    }
    private func update(_ point: CGPoint) {
        hovered = WheelHitTesting.index(at: point, count: options.count).map { options[$0].id }
    }
}
