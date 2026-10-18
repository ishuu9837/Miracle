import SwiftUI

struct SettingsView: View {
    @ObservedObject var model: AppModel
    @ObservedObject var settings: SettingsStore
    var body: some View {
        TabView {
            Form {
                BackgroundSettings(model: model)
                Section("General") {
                    Picker("Terminal app", selection: $settings.terminalApp) {
                        Text("Terminal").tag("com.apple.Terminal")
                        Text("iTerm2").tag("com.googlecode.iterm2")
                    }
                    Toggle("Pause drag shortcuts", isOn: $model.paused)
                    Toggle("Keep recent job history", isOn: $settings.historyEnabled)
                    Text("History saves file names locally, up to 50 jobs. File contents are never stored.").font(.caption).foregroundStyle(.secondary)
                    Button("Clear history", action: model.clearHistory)
                }
                Section("Privacy") {
                    Label("Works entirely offline", systemImage: "lock.shield")
                    Text("No account, analytics, or file uploads. This build makes no network requests.").foregroundStyle(.secondary)
                }
            }.formStyle(.grouped).tabItem { Label("General", systemImage: "gearshape") }
            Form {
                Section("During a file drag") {
                    LabeledContent("Convert", value: "⇧ Shift")
                    LabeledContent("Tools", value: "⌘ Command")
                    LabeledContent("Actions", value: "⌃ Control")
                    Text("Tap a modifier while dragging, then drop onto an option. Press Esc to dismiss.").font(.caption).foregroundStyle(.secondary)
                }
                Section("Keyboard alternative") {
                    Text("Choose File from the menu bar. Use Tab or arrow keys to select an option, then press Return.")
                }
            }.formStyle(.grouped).tabItem { Label("Keys", systemImage: "keyboard") }
            Form {
                Section("Save new files") {
                    LabeledContent("Location", value: settings.outputFolder?.lastPathComponent ?? "Beside the original")
                    HStack {
                        Button("Choose folder…") {
                            let panel = NSOpenPanel(); panel.canChooseDirectories = true; panel.canChooseFiles = false
                            if panel.runModal() == .OK { settings.outputFolder = panel.url }
                        }
                        Button("Use source folder") { settings.outputFolder = nil }.disabled(settings.outputFolder == nil)
                    }
                    Text("Original files are kept. A numeric suffix prevents overwriting existing files.").font(.caption).foregroundStyle(.secondary)
                }
                Section("Default JPEG / HEIC quality") {
                    Slider(value: $settings.quality, in: 0.1...1) { Text("Quality") }
                    Text(settings.quality, format: .percent.precision(.fractionLength(0)))
                }
            }.formStyle(.grouped).tabItem { Label("Output", systemImage: "folder") }
            Form {
                Section("Offline conversion formats") {
                    Text("Images: JPG, PNG, WebP, HEIC, TIFF, AVIF; PDF export. SVG and BMP input only.\n\nAudio: MP3, M4A, WAV, FLAC, OGG, Opus, AIFF; WMA input only.\n\nVideo: MP4, MOV, MKV, WebM, GIF; MP3 export. AVI and WMV input only.\n\nPDF: DOCX, Pages*, PPTX, XLSX, JPG, PNG, EPUB, TXT, MD. Every page, with images at 300 DPI.\n\nDocuments: DOCX, Pages*, Markdown, TXT, EPUB; RTF, RTFD and text clippings input, HTML export.\n\nSlides: PPT/PPTX to PDF, JPG, PNG.\n\nSpreadsheets: XLSX, ODS, CSV, TSV; PDF export. XLS input only.\n\nSubtitles: SRT, VTT; TXT export.\n\nArchives and folders: ZIP, TAR, GZIP (.tar.gz); RAR input only.")
                }
                Section("Conversion notes") {
                    Text("*Native Pages export requires Apple Pages to be installed. PDF text exports use local OCR where needed. DOCX uses reflowed text, XLSX uses Page/Text columns, and PPTX imports a slide per page. Complex layouts and embedded media may change in text-first document conversions. CSV/TSV export uses the first sheet. Originals are kept. Archives have a 4 GB expanded-data limit.").font(.caption).foregroundStyle(.secondary)
                }
            }.formStyle(.grouped).tabItem { Label("Formats", systemImage: "doc.on.doc") }
        }.padding(DesignTokens.spacing).frame(width: 600, height: 520)
    }
}
