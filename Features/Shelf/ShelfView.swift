import SwiftUI

struct ShelfView: View {
    @ObservedObject var model: AppModel
    @State private var targeted = false
    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Label("Float shelf", systemImage: "square.stack.3d.up").font(.headline)
                Spacer()
                Button { model.pinShelf?() } label: { Image(systemName: "pin") }.help("Pin shelf to the right screen edge").accessibilityLabel("Pin shelf to screen edge")
                Button("Clear") { model.shelf.removeAll() }.disabled(model.shelf.isEmpty)
            }
            if model.shelf.isEmpty {
                VStack(spacing: 8) {
                    Image(systemName: "tray").font(.largeTitle).foregroundStyle(.tertiary)
                    Text("Drop files here to keep them handy.").font(.caption).foregroundStyle(.secondary)
                }.frame(maxWidth: .infinity, minHeight: 100)
            } else {
                ScrollView {
                    VStack(spacing: 8) {
                        ForEach(model.shelf, id: \.self) { url in
                            HStack {
                                Image(nsImage: NSWorkspace.shared.icon(forFile: url.path)).resizable().frame(width: 24, height: 24)
                                Text(url.lastPathComponent).lineLimit(1).truncationMode(.middle)
                                Spacer()
                                Button { model.shelf.removeAll { $0 == url } } label: { Image(systemName: "xmark.circle.fill").foregroundStyle(.secondary) }
                                    .buttonStyle(.plain).accessibilityLabel("Remove \(url.lastPathComponent) from shelf")
                            }.padding(8).background(.quaternary, in: RoundedRectangle(cornerRadius: 8))
                                .onDrag { NSItemProvider(contentsOf: url) ?? NSItemProvider() }
                                .contextMenu {
                                    Button("Reveal in Finder") { NSWorkspace.shared.activateFileViewerSelecting([url]) }
                                    Button("Use in Miracle") { model.selected = [url]; model.wheelController?.show(at: NSEvent.mouseLocation, drag: false) }
                                }
                        }
                    }
                }.frame(maxHeight: 280)
            }
            Text("Drag a file back out whenever you need it.").font(DesignTokens.caption).foregroundStyle(.secondary)
        }.padding(DesignTokens.spacing).frame(width: 320)
            .background(targeted ? Color.green.opacity(0.1) : Color.clear)
            .onDrop(of: [.fileURL], isTargeted: $targeted) { providers in
                receiveFiles(providers) { urls in for url in urls where !model.shelf.contains(url) { model.shelf.append(url) } }
                return true
            }
    }
}
