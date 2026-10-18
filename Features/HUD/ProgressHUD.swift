import SwiftUI
import AppKit

struct ProgressHUD: View {
    @ObservedObject var model: AppModel
    private func openOutput() {
        guard let output = model.outputs.first else { return }
        NSWorkspace.shared.open(output)
        model.dismissHUD?()
    }
    private func revealOutputs() {
        NSWorkspace.shared.activateFileViewerSelecting(model.outputs)
        model.dismissHUD?()
    }
    var body: some View {
        VStack(alignment: .leading, spacing: DesignTokens.spacing) {
            HStack(spacing: 12) {
                Image(systemName: model.running ? "arrow.triangle.2.circlepath" : model.failed ? "exclamationmark.triangle" : "checkmark.circle.fill")
                    .font(.title2).foregroundStyle(model.failed ? .red : model.running ? .blue : .green)
                VStack(alignment: .leading, spacing: 4) {
                    Text(model.message).font(.headline)
                    Text(model.detail).font(.caption).foregroundStyle(.secondary).lineLimit(3)
                }
                Spacer()
                if model.running { Button("Cancel", action: model.cancel).keyboardShortcut(.cancelAction) }
                else { Button("Done") { model.dismissHUD?() }.keyboardShortcut(.cancelAction) }
            }
            if model.running {
                if model.progress.isFinite { ProgressView(value: model.progress).accessibilityLabel("Job progress") }
                else { ProgressView().controlSize(.small).accessibilityLabel("Converting file") }
            }
            if !model.outputs.isEmpty {
                HStack {
                    Button("Reveal in Finder", action: revealOutputs)
                    Button("Open", action: openOutput)
                }
            }
            if model.failed { Button("Retry", action: model.retry) }
        }.padding(DesignTokens.spacing).frame(width: 420).accessibilityElement(children: .contain)
    }
}
