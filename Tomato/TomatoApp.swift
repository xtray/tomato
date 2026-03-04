import SwiftUI
import AppKit

protocol ApplicationIconSetting: AnyObject {
    var applicationIconImage: NSImage! { get set }
}

extension NSApplication: ApplicationIconSetting {}

enum RuntimeAppIconApplier {
    static func applyIfPossible(
        to application: ApplicationIconSetting?,
        loadIcon: () -> NSImage? = { RuntimeAppIconApplier.loadIconFromMainBundle() }
    ) {
        guard let application else { return }
        guard let icon = loadIcon() else { return }
        application.applicationIconImage = icon
    }

    static func loadIconFromMainBundle(bundle: Bundle = .main) -> NSImage? {
        guard let iconPath = bundle.path(forResource: "AppIcon", ofType: "icns") else {
            return nil
        }

        return NSImage(contentsOfFile: iconPath)
    }
}

@main
struct TomatoApp: App {
    @StateObject private var taskStore = TaskStore()
    @State private var commandsLanguage = LanguagePreferences.load()
    @State private var hasAppliedRuntimeIcon = false
    
    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(taskStore)
                .onAppear {
                    applyRuntimeIconIfNeeded()
                }
        }
        .windowStyle(.hiddenTitleBar)
        .windowResizability(.contentSize)
        .commands {
            CommandGroup(replacing: .newItem) {}
            // Keep Commands menu language stable during runtime.
            // Updating CommandMenu titles live can trigger AppKit menu mapping warnings/crashes.
            CommandMenu(AppText.string("menu.timer", language: commandsLanguage)) {
                Button(AppText.string("menu.start_focus", language: commandsLanguage)) {
                    taskStore.startFocusSession()
                }
                .keyboardShortcut("s", modifiers: [.command])
                .disabled(!taskStore.canStartOrResumeFocus)
                
                Button(AppText.string("common.stop", language: commandsLanguage)) {
                    taskStore.stopTimer()
                }
                .keyboardShortcut(".", modifiers: [.command])
                .disabled(!taskStore.isTimerRunning)
                
                Button(AppText.string("common.reset", language: commandsLanguage)) {
                    taskStore.resetTimer()
                }
                .keyboardShortcut("r", modifiers: [.command])
                
                Divider()
                
                Button(AppText.string("menu.settings", language: commandsLanguage)) {
                    taskStore.showingSettings = true
                }
                .keyboardShortcut(",", modifiers: [.command])
            }
        }
    }

    private func applyRuntimeIconIfNeeded() {
        guard !hasAppliedRuntimeIcon else { return }
        RuntimeAppIconApplier.applyIfPossible(to: NSApp)
        hasAppliedRuntimeIcon = true
    }
}
