import SwiftUI
import ServiceManagement

struct BackgroundSettings: View {
    @ObservedObject var model: AppModel
    @State private var launchAtLogin = SMAppService.mainApp.status == .enabled
    @State private var status = ""
    var body: some View {
        Section("Background shortcuts") {
            Text("Select a file or folder in Finder, then tap Shift. You can also hold the mouse on a selected file and tap Shift. The menu bar app keeps running after you close its windows.")
                .font(.caption).foregroundStyle(.secondary)
            Button("Allow Finder selection…") {
                do { try FinderSelection.requestAccess(); status = "Finder access is ready. Select a file in Finder and tap Shift." }
                catch { status = error.localizedDescription }
            }
            Toggle("Open Miracle at login", isOn: $launchAtLogin)
                .onChange(of: launchAtLogin) { enabled in
                    do {
                        if enabled { try SMAppService.mainApp.register() }
                        else { try SMAppService.mainApp.unregister() }
                        status = ""
                    } catch { status = "Install Miracle in Applications first, then enable background startup here." }
                }
            if !status.isEmpty { Text(status).font(.caption).foregroundStyle(.secondary) }
        }
    }
}
