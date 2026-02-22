import SwiftUI
import AppKit

@main
struct TomatoApp: App {
    @StateObject private var taskStore = TaskStore()
    @State private var commandsLanguage = LanguagePreferences.load()

    init() {
        applyRuntimeAppIcon()
    }
    
    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(taskStore)
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
                .disabled(taskStore.selectedTask == nil)
                
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

    private func applyRuntimeAppIcon() {
        if let iconPath = Bundle.main.path(forResource: "AppIcon", ofType: "icns"),
           let icon = NSImage(contentsOfFile: iconPath) {
            NSApplication.shared.applicationIconImage = icon
        }
    }
}
