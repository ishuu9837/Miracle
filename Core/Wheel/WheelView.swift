import SwiftUI
import AppKit

extension WheelMode {
    var color: Color { switch self { case .convert: return Color(red: 1, green: 0.31, blue: 0); case .tools: return .blue; case .actions: return .green } }
}

struct WheelView: View {
    @ObservedObject var model: AppModel
    @Environment(\.accessibilityReduceTransparency) private var opaque
    @State private var hovered: String?
    @State private var keyboardIndex = 0
    @State private var page = 0
    let dismiss: () -> Void
    private var options: [FileOption] { Array(model.options.dropFirst(page * 6).prefix(6)) }
    private var current: FileOption? { options.first { $0.id == hovered } }
    private var modeKey: String { model.mode == .convert ? "shift" : model.mode == .tools ? "command" : "control" }

    var body: some View {
        VStack(spacing: 12) {
            HStack(spacing: 10) {
                VStack(spacing: 3) {
                    Text(model.mode.key).font(.system(size: 23, weight: .medium))
                    Text(modeKey).font(DesignTokens.caption)
                }
                .frame(width: 62, height: 54)
                .foregroundStyle(model.mode.color)
                .background(model.mode.color.opacity(0.08), in: RoundedRectangle(cornerRadius: 10))
                .overlay(RoundedRectangle(cornerRadius: 10).stroke(model.mode.color.opacity(0.25)))
                VStack(alignment: .leading, spacing: 4) {
                    Text(model.mode == .convert ? "Convert formats" : model.mode == .tools ? "Edit your files" : "File actions")
                        .font(DesignTokens.label)
                    Text("\(model.selected.count) file\(model.selected.count == 1 ? "" : "s") selected")
                        .font(DesignTokens.caption).foregroundStyle(.secondary)
                }
            }.padding(.top, 16)
            ZStack {
                Circle().fill(Color(nsColor: .controlBackgroundColor)).shadow(color: .black.opacity(0.06), radius: 10, y: 3)
                ForEach(Array(options.enumerated()), id: \.element.id) { index, option in
                    sectorButton(index: index, option: option)
                }
                Circle().fill(Color(nsColor: .windowBackgroundColor)).frame(width: 118, height: 118)
                    .shadow(color: .black.opacity(0.04), radius: 6, y: -2)
                Text(current?.title ?? model.mode.rawValue)
                    .font(.system(size: 15, weight: .medium, design: .rounded))
                    .lineLimit(2).multilineTextAlignment(.center)
                    .frame(width: 96, height: 48)
                    .background(Color(nsColor: .controlBackgroundColor), in: Capsule())
                    .shadow(color: .black.opacity(0.06), radius: 8, y: 4)
                if options.isEmpty {
                    Text("No options for this format. Try Actions.")
                        .font(DesignTokens.caption).multilineTextAlignment(.center)
                        .frame(width: 160).offset(y: 105)
                }
            }.frame(width: 340, height: 340)
            .onContinuousHover { phase in
                switch phase {
                case .active(let point): hovered = WheelHitTesting.index(at: point, count: options.count).map { options[$0].id }
                case .ended: hovered = nil
                }
            }
            .onDrop(of: [.fileURL], delegate: WheelDropDelegate(options: options, hovered: $hovered, model: model))
            Text(current.map { model.mode == .convert && $0.title != "Create PDF" ? "Convert to \($0.title)" : $0.title } ?? "Drop on an option · Esc to dismiss")
                .font(DesignTokens.label).foregroundStyle(.secondary)
            if model.options.count > 6 {
                HStack {
                    Button("Previous") { page -= 1; hovered = nil; keyboardIndex = 0 }.disabled(page == 0)
                    Text("\(page + 1) / \((model.options.count + 5) / 6)").font(DesignTokens.caption)
                    Button("Next") { page += 1; hovered = nil; keyboardIndex = 0 }.disabled((page + 1) * 6 >= model.options.count)
                }
            }
            if let note = current?.unavailableReason ?? current?.note {
                Text(note).font(DesignTokens.caption).foregroundStyle(.secondary).multilineTextAlignment(.center).frame(width: 350).fixedSize(horizontal: false, vertical: true)
            }
            HStack(spacing: 16) {
                ForEach(WheelMode.allCases) { mode in
                    Button { model.mode = mode } label: {
                        Text("\(mode.key) \(mode.rawValue)").font(DesignTokens.caption)
                            .foregroundStyle(model.mode == mode ? mode.color : .secondary)
                    }.buttonStyle(.plain)
                }
            }.padding(.bottom, 14)
        }
        .frame(width: DesignTokens.wheelSize)
        .background {
            if opaque { RoundedRectangle(cornerRadius: 24).fill(Color(nsColor: .windowBackgroundColor)) }
            else { RoundedRectangle(cornerRadius: 24).fill(.regularMaterial) }
        }
        .focusable()
        .onChange(of: model.mode) { _ in keyboardIndex = 0; hovered = nil; page = 0 }
        .onExitCommand(perform: dismiss)
        .onMoveCommand { direction in
            guard !options.isEmpty else { return }
            keyboardIndex = (keyboardIndex + ((direction == .left || direction == .up) ? options.count - 1 : 1)) % options.count
            hovered = options[keyboardIndex].id
        }
        .background(Button("") {
            if let selection = current ?? options.first { model.select(selection) }
        }.keyboardShortcut(.return, modifiers: []).hidden())
    }

    private func sectorButton(index: Int, option: FileOption) -> some View {
        let sector = WheelSector(index: index, count: options.count)
        let angle = Double(index) / Double(max(options.count, 1)) * 2 * Double.pi - Double.pi / 2
        let active = hovered == option.id
        return Button { model.select(option) } label: {
            ZStack {
                sector.fill(active ? model.mode.color : Color(nsColor: .controlBackgroundColor))
                sector.stroke(active ? model.mode.color : Color.primary.opacity(0.13), lineWidth: 1)
                VStack(spacing: 4) {
                    if model.mode != .convert { Image(systemName: option.symbol).font(.system(size: 18)) }
                    Text(option.title == "Create PDF" ? "PDF" : option.title)
                        .font(.system(size: model.mode == .convert ? 17 : 11, weight: .semibold, design: .rounded))
                        .multilineTextAlignment(.center).lineLimit(2)
                }
                .foregroundStyle(active ? Color.white : Color.primary)
                .frame(width: 82, height: 48)
                .position(x: 170 + cos(angle) * 112, y: 170 + sin(angle) * 112)
            }.frame(width: 340, height: 340).contentShape(sector)
        }
        .buttonStyle(.plain)
        .contentShape(sector)
        .opacity(option.unavailableReason == nil ? 1 : 0.45)
        .help(option.unavailableReason ?? option.note ?? option.title)
        .accessibilityLabel("\(model.mode.rawValue): \(option.title)")

    }
}

/// File promises are read asynchronously and restored to their original selection order.
@MainActor func receiveFiles(_ providers: [NSItemProvider], completion: @escaping @MainActor ([URL]) -> Void) {
    Task { @MainActor in
        var urls: [URL] = []
        for provider in providers {
            let url: URL? = await withCheckedContinuation { continuation in
                provider.loadItem(forTypeIdentifier: "public.file-url", options: nil) { item, _ in
                    if let data = item as? Data { continuation.resume(returning: URL(dataRepresentation: data, relativeTo: nil)) }
                    else if let url = item as? URL { continuation.resume(returning: url) }
                    else { continuation.resume(returning: nil) }
                }
            }
            if let url, url.isFileURL { urls.append(url) }
        }
        if !urls.isEmpty { completion(urls) }
    }
}
