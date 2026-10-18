import SwiftUI

struct WelcomeView: View {
    @ObservedObject var model: AppModel
    @State private var targeted = false
    let settings: () -> Void
    let shelf: () -> Void
    var body: some View {
        VStack(spacing: 24) {
            HStack {
                HStack(spacing: 10) { Image("BrandLogo").resizable().scaledToFill().frame(width: 38, height: 38).clipShape(RoundedRectangle(cornerRadius: 8)); Text("miracle").font(.headline) }
                Spacer()
                Label("On-device", systemImage: "lock.shield").font(.caption).foregroundStyle(.secondary)
            }
            VStack(spacing: 10) {
                Image("BrandLogo").resizable().scaledToFit().frame(width: 132, height: 132).clipShape(RoundedRectangle(cornerRadius: 24)).accessibilityLabel("miracle logo")
                Text("Your files. A shortcut away.").font(.system(size: 24, weight: .semibold))
                Text("Select a file in Finder, then tap Shift.").foregroundStyle(.secondary)
            }.padding(.top, 8)
            HStack(spacing: 12) {
                ForEach(WheelMode.allCases) { mode in
                    VStack(spacing: 8) {
                        Text(mode.key).font(.system(size: 27, weight: .medium)).foregroundStyle(mode.color)
                        Text(mode.rawValue).font(.headline)
                        Text(mode == .convert ? "Change the format" : mode == .tools ? "Make a quick edit" : "Put files to work").font(DesignTokens.caption).foregroundStyle(.secondary)
                    }.frame(maxWidth: .infinity).padding(.vertical, 18)
                        .background(mode.color.opacity(0.05), in: RoundedRectangle(cornerRadius: DesignTokens.radius))
                }
            }
            VStack(spacing: 12) {
                Image(systemName: "square.and.arrow.down").font(.title2).foregroundStyle(targeted ? .blue : .secondary)
                Text(targeted ? "Release to see your options" : "Drop files here, or choose them below").font(DesignTokens.label)
                Button("Choose files…", action: model.chooseFiles).buttonStyle(.borderedProminent).controlSize(.large)
                    .keyboardShortcut("o", modifiers: .command)
            }.frame(maxWidth: .infinity).padding(.vertical, 24)
                .background(targeted ? Color.blue.opacity(0.07) : Color(nsColor: .controlBackgroundColor), in: RoundedRectangle(cornerRadius: DesignTokens.radius))
                .overlay(RoundedRectangle(cornerRadius: DesignTokens.radius).strokeBorder(style: StrokeStyle(lineWidth: 1, dash: [5, 4])).foregroundStyle(targeted ? Color.blue : Color.primary.opacity(0.15)))
                .onDrop(of: [.fileURL], isTargeted: $targeted) { providers in
                    receiveFiles(providers) { urls in
                        model.selected = urls
                        model.wheelController?.show(at: NSEvent.mouseLocation, drag: false)
                    }
                    return true
                }
            HStack {
                Text("Runs in the menu bar. Enable Finder access in Settings.").font(.caption).foregroundStyle(.secondary)
                Spacer()
                Button("Float shelf", action: shelf)
                Button("Settings…", action: settings)
            }
        }.padding(28).frame(width: 580)
    }
}

struct HistoryView: View {
    @ObservedObject var model: AppModel
    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            HStack { Text("Recent jobs").font(DesignTokens.title); Spacer(); Button("Clear", action: model.clearHistory).disabled(model.history.isEmpty) }
            if model.history.isEmpty {
                VStack(spacing: 10) {
                    Image(systemName: "clock").font(.largeTitle).foregroundStyle(.tertiary)
                    Text("No conversions yet.").foregroundStyle(.secondary)
                }.frame(maxWidth: .infinity, minHeight: 180)
            } else {
                List(model.history) { entry in
                    HStack(spacing: 12) {
                        Image(systemName: entry.status == "succeeded" ? "checkmark.circle" : entry.status == "cancelled" ? "minus.circle" : "exclamationmark.circle")
                            .foregroundStyle(entry.status == "succeeded" ? .green : .secondary)
                        VStack(alignment: .leading, spacing: 4) {
                            Text(entry.operation).font(.headline)
                            Text(entry.name).lineLimit(1).truncationMode(.middle).font(.caption).foregroundStyle(.secondary)
                        }
                        Spacer()
                        Text(entry.date, style: .relative).font(.caption).foregroundStyle(.secondary)
                    }.padding(.vertical, 4)
                }.frame(height: 300)
            }
        }.padding(20).frame(width: 480)
    }
}
