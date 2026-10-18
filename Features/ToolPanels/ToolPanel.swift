import SwiftUI

struct ToolPanel: View {
    @ObservedObject var model: AppModel
    @State private var dimension = 1920
    @State private var quality = 0.8
    let close: () -> Void
    var body: some View {
        VStack(alignment: .leading, spacing: DesignTokens.spacing) {
            Text(model.pendingOption?.title ?? "Edit image").font(DesignTokens.title)
            if let first = model.selected.first {
                HStack(spacing: DesignTokens.spacing) {
                    FilePreview(url: first).frame(width: 140, height: 130)
                    VStack(alignment: .leading, spacing: 8) {
                        Text(first.lastPathComponent).lineLimit(2).truncationMode(.middle)
                        Text("\(model.selected.count) selected · Originals are kept").font(.caption).foregroundStyle(.secondary)
                    }
                }
            }
            if model.pendingOption?.kind == .resize {
                TextField("Maximum edge (pixels)", value: $dimension, format: .number)
                Text("Preserves aspect ratio and never enlarges the image.").font(.caption).foregroundStyle(.secondary)
                if !(16...16000).contains(dimension) { Label("Choose a size from 16 to 16,000 pixels.", systemImage: "exclamationmark.circle").foregroundStyle(.red).font(.caption) }
            } else {
                HStack { Text("JPEG quality"); Spacer(); Text(quality, format: .percent.precision(.fractionLength(0))).monospacedDigit() }
                Slider(value: $quality, in: 0.1...1).accessibilityLabel("JPEG quality")
                Label("Creates a lossy JPEG. Transparency and animation are not preserved. A smaller file is not guaranteed.", systemImage: "info.circle").font(.caption).foregroundStyle(.secondary)
            }
            Divider()
            HStack {
                Button("Cancel", action: close).keyboardShortcut(.cancelAction)
                Spacer()
                Button("Apply") {
                    if let option = model.pendingOption {
                        model.run(option, parameters: .init(quality: quality, maxDimension: dimension))
                    }
                    close()
                }.keyboardShortcut(.defaultAction).buttonStyle(.borderedProminent)
                    .disabled(model.pendingOption?.kind == .resize && !(16...16000).contains(dimension))
            }
        }.padding(24).frame(width: DesignTokens.panelWidth).onAppear { quality = model.settings.quality }
    }
}

struct FilePreview: View {
    let url: URL
    @State private var image: NSImage?
    var body: some View {
        ZStack {
            RoundedRectangle(cornerRadius: 10).fill(.quaternary)
            if let image { Image(nsImage: image).resizable().scaledToFit().padding(8) }
            else { ProgressView() }
        }
        .task(id: url) {
            let cgImage = await Task.detached(priority: .utility) { try? ImageEngine.load(url, maxDimension: 320) }.value
            if let cgImage { image = NSImage(cgImage: cgImage, size: .zero) }
            else { image = NSWorkspace.shared.icon(forFile: url.path) }
        }
        .accessibilityLabel("File preview")
    }
}
