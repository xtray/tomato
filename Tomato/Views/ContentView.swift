import SwiftUI
import AppKit

struct ContentView: View {
    @EnvironmentObject var taskStore: TaskStore
    @State private var newTaskTitle: String = ""
    
    var body: some View {
        mainView
            .sheet(isPresented: $taskStore.showingSettings) {
                SettingsView(taskStore: taskStore)
            }
            .onChange(of: taskStore.showingFloatingWindow) { newValue in
                if newValue {
                    FloatingWindowController.shared.show(taskStore: taskStore) {
                        taskStore.showingFloatingWindow = false
                    }
                    mainAppWindow()?.orderOut(nil)
                } else {
                    FloatingWindowController.shared.hide()
                    DispatchQueue.main.asyncAfter(deadline: .now() + 0.1) {
                        mainAppWindow()?.makeKeyAndOrderFront(nil)
                        NSApp.activate(ignoringOtherApps: true)
                    }
                }
            }
    }

    private func mainAppWindow() -> NSWindow? {
        NSApp.windows.first(where: { !($0 is NSPanel) })
    }
    
    var mainView: some View {
        HStack(spacing: 0) {
            taskListView
                .frame(width: 280)
            
            Divider()
            
            timerView
                .frame(maxWidth: .infinity)
        }
        .frame(width: 500, height: 450)
        .background(Color(NSColor.windowBackgroundColor))
    }
    
    var taskListView: some View {
        VStack(alignment: .leading, spacing: 0) {
            HStack {
                Text("Tasks")
                    .font(.headline)
                
                Spacer()
                
                Button {
                    taskStore.showingSettings = true
                } label: {
                    Image(systemName: "gear")
                        .font(.body)
                }
                .buttonStyle(.plain)
                .help("Settings")
            }
            .padding()
            .background(Color(NSColor.controlBackgroundColor))
            
            List(selection: $taskStore.selectedTask) {
                ForEach(taskStore.tasks) { task in
                    TaskRowView(
                        task: task,
                        isSelected: taskStore.selectedTask?.id == task.id,
                        onToggleComplete: {
                            taskStore.toggleTaskCompletion(task)
                        }
                    )
                    .tag(task)
                    .onTapGesture {
                        taskStore.selectTask(task)
                    }
                }
                .onDelete(perform: taskStore.deleteTask)
                .onMove(perform: taskStore.updateTaskOrder)
            }
            .listStyle(.sidebar)
            
            HStack {
                TextField("Add new task...", text: $newTaskTitle)
                    .textFieldStyle(.roundedBorder)
                    .onSubmit {
                        let title = newTaskTitle.trimmingCharacters(in: .whitespacesAndNewlines)
                        if !title.isEmpty {
                            taskStore.addTask(title: title)
                            newTaskTitle = ""
                        }
                    }

                Button {
                    let title = newTaskTitle.trimmingCharacters(in: .whitespacesAndNewlines)
                    if !title.isEmpty {
                        taskStore.addTask(title: title)
                        newTaskTitle = ""
                    }
                } label: {
                    Image(systemName: "plus.circle.fill")
                        .foregroundColor(.accentColor)
                }
                .buttonStyle(.plain)
            }
            .padding()
        }
    }
    
    var timerView: some View {
        VStack(spacing: 24) {
            if let task = taskStore.selectedTask {
                VStack(spacing: 4) {
                    Text(task.title)
                        .font(.title2)
                        .fontWeight(.medium)
                    
                    HStack(spacing: 4) {
                        Image(systemName: "flame.fill")
                            .foregroundColor(.orange)
                        Text("\(task.completedPomodoros) pomodoros completed")
                    }
                    .font(.subheadline)
                    .foregroundColor(.secondary)
                }
            } else {
                VStack(spacing: 8) {
                    Image(systemName: "timer")
                        .font(.system(size: 40))
                        .foregroundColor(.secondary)
                    Text("Select a task")
                        .font(.title2)
                        .foregroundColor(.secondary)
                }
            }
            
            ZStack {
                Circle()
                    .stroke(Color.gray.opacity(0.2), lineWidth: 10)
                    .frame(width: 200, height: 200)
                
                Circle()
                    .trim(from: 0, to: timerProgress)
                    .stroke(timerColor, style: StrokeStyle(lineWidth: 10, lineCap: .round))
                    .frame(width: 200, height: 200)
                    .rotationEffect(.degrees(-90))
                    .animation(.linear(duration: 1), value: taskStore.remainingSeconds)
                
                VStack {
                    Text(timeString)
                        .font(.system(size: 44, weight: .light, design: .monospaced))
                        .contentTransition(.numericText())
                        .animation(.linear(duration: 0.5), value: taskStore.remainingSeconds)
                    
                    Text(phaseText)
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                }
            }
            
            HStack(spacing: 20) {
                if taskStore.isTimerRunning {
                    Button {
                        taskStore.stopTimer()
                    } label: {
                        Label("Stop", systemImage: "stop.fill")
                    }
                    .buttonStyle(.borderedProminent)
                    .tint(.red)
                } else {
                    Button {
                        taskStore.startFocusSession()
                    } label: {
                        Label("Focus", systemImage: "play.fill")
                    }
                    .buttonStyle(.borderedProminent)
                    .disabled(taskStore.selectedTask == nil)
                }
                
                Button {
                    taskStore.resetTimer()
                } label: {
                    Label("Reset", systemImage: "arrow.counterclockwise")
                }
                .buttonStyle(.bordered)
                
                Button {
                    taskStore.showingFloatingWindow = true
                } label: {
                    Label("Float", systemImage: "rectangle.portrait.and.arrow.right")
                }
                .buttonStyle(.bordered)
                .disabled(taskStore.selectedTask == nil)
            }
            .disabled(taskStore.selectedTask == nil && !taskStore.isTimerRunning)
        }
        .padding()
    }
    
    var timerColor: Color {
        taskStore.currentPhase.color
    }
    
    var phaseText: String {
        taskStore.currentPhase.displayName
    }
    
    var timeString: String {
        let minutes = taskStore.remainingSeconds / 60
        let seconds = taskStore.remainingSeconds % 60
        return String(format: "%02d:%02d", minutes, seconds)
    }
    
    var timerProgress: CGFloat {
        let total = CGFloat(currentPhaseDuration)
        return CGFloat(taskStore.remainingSeconds) / total
    }
    
    var currentPhaseDuration: Int {
        switch taskStore.currentPhase {
        case .work:
            return taskStore.workDuration
        case .shortBreak:
            return taskStore.shortBreakDuration
        case .longBreak:
            return taskStore.longBreakDuration
        }
    }
}

struct TaskRowView: View {
    let task: PomodoroTask
    let isSelected: Bool
    let onToggleComplete: () -> Void
    
    var body: some View {
        HStack {
            Button {
                onToggleComplete()
            } label: {
                Image(systemName: task.isCompleted ? "checkmark.circle.fill" : "circle")
                    .foregroundColor(task.isCompleted ? .green : .secondary)
            }
            .buttonStyle(.plain)
            
            VStack(alignment: .leading, spacing: 2) {
                Text(task.title)
                    .font(.body)
                    .strikethrough(task.isCompleted)
                    .foregroundColor(task.isCompleted ? .secondary : .primary)
                
                if task.completedPomodoros > 0 {
                    HStack(spacing: 2) {
                        Text("\(task.completedPomodoros)X")
                            .font(.caption)
                            .foregroundColor(.secondary)
                        ForEach(0..<min(task.completedPomodoros, 5), id: \.self) { _ in
                            TinyTomatoIcon(size: 8)
                        }
                        if task.completedPomodoros > 5 {
                            Text("+")
                                .font(.caption2)
                                .foregroundColor(.secondary)
                        }
                    }
                }
            }
            
            Spacer()
        }
        .padding(.vertical, 4)
        .contentShape(Rectangle())
        .background(isSelected ? Color.accentColor.opacity(0.1) : Color.clear)
        .cornerRadius(6)
    }
}

struct TinyTomatoIcon: View {
    let size: CGFloat

    var body: some View {
        ZStack(alignment: .top) {
            Circle()
                .fill(Color.red)
                .frame(width: size, height: size)

            Capsule()
                .fill(Color.green)
                .frame(width: size * 0.36, height: size * 0.24)
                .offset(y: -size * 0.35)
        }
        .frame(width: size, height: size)
    }
}
