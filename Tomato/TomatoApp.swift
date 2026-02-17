import SwiftUI

@main
struct TomatoApp: App {
    @StateObject private var taskStore = TaskStore()
    
    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(taskStore)
        }
        .windowStyle(.hiddenTitleBar)
        .windowResizability(.contentSize)
        .commands {
            CommandGroup(replacing: .newItem) {}
            CommandMenu("Timer") {
                Button("Start Focus") {
                    taskStore.startFocusSession()
                }
                .keyboardShortcut("s", modifiers: [.command])
                .disabled(taskStore.selectedTask == nil)
                
                Button("Stop") {
                    taskStore.stopTimer()
                }
                .keyboardShortcut(".", modifiers: [.command])
                .disabled(!taskStore.isTimerRunning)
                
                Button("Reset") {
                    taskStore.resetTimer()
                }
                .keyboardShortcut("r", modifiers: [.command])
                
                Divider()
                
                Button("Settings...") {
                    taskStore.showingSettings = true
                }
                .keyboardShortcut(",", modifiers: [.command])
            }
        }
    }
}
