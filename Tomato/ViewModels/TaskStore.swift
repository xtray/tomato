import Foundation
import Combine
import SwiftUI

enum TimerPhase: String, Codable {
    case work
    case shortBreak
    case longBreak
    
    var displayName: String {
        switch self {
        case .work: return "Focus Time"
        case .shortBreak: return "Short Break"
        case .longBreak: return "Long Break"
        }
    }
    
    var color: Color {
        switch self {
        case .work: return .red
        case .shortBreak: return .green
        case .longBreak: return .blue
        }
    }
}

class TaskStore: ObservableObject {
    @Published var tasks: [PomodoroTask] = []
    @Published var selectedTask: PomodoroTask?
    @Published var isTimerRunning: Bool = false
    @Published var remainingSeconds: Int = 25 * 60
    @Published var currentPhase: TimerPhase = .work
    @Published var showingFloatingWindow: Bool = false
    @Published var showingSettings: Bool = false
    @Published var themeMode: ThemeMode {
        didSet {
            ThemePreferences.save(themeMode)
        }
    }
    
    @Published var workDuration: Int {
        didSet {
            UserDefaults.standard.set(workDuration, forKey: "workDuration")
            if currentPhase == .work && !isTimerRunning {
                remainingSeconds = workDuration
            }
        }
    }
    
    @Published var shortBreakDuration: Int {
        didSet {
            UserDefaults.standard.set(shortBreakDuration, forKey: "shortBreakDuration")
            if currentPhase == .shortBreak && !isTimerRunning {
                remainingSeconds = shortBreakDuration
            }
        }
    }
    
    @Published var longBreakDuration: Int {
        didSet {
            UserDefaults.standard.set(longBreakDuration, forKey: "longBreakDuration")
            if currentPhase == .longBreak && !isTimerRunning {
                remainingSeconds = longBreakDuration
            }
        }
    }
    
    private var timer: Timer?
    private var completedWorkSessions: Int = 0
    
    init() {
        let savedWorkDuration = UserDefaults.standard.integer(forKey: "workDuration")
        let savedShortBreakDuration = UserDefaults.standard.integer(forKey: "shortBreakDuration")
        let savedLongBreakDuration = UserDefaults.standard.integer(forKey: "longBreakDuration")
        self.themeMode = ThemePreferences.load()
        
        self.workDuration = savedWorkDuration > 0 ? savedWorkDuration : 25 * 60
        self.shortBreakDuration = savedShortBreakDuration > 0 ? savedShortBreakDuration : 5 * 60
        self.longBreakDuration = savedLongBreakDuration > 0 ? savedLongBreakDuration : 15 * 60
        
        self.remainingSeconds = workDuration
        loadTasks()
    }
    
    func addTask(title: String) {
        let task = PomodoroTask(title: title)
        tasks.append(task)
        saveTasks()
    }
    
    func deleteTask(at offsets: IndexSet) {
        let selectedID = selectedTask?.id
        let isDeletingSelectedTask = offsets.contains { index in
            tasks.indices.contains(index) && tasks[index].id == selectedID
        }

        tasks.remove(atOffsets: offsets)
        if isDeletingSelectedTask {
            selectedTask = nil
        }
        saveTasks()
    }

    func deleteTask(id: UUID) {
        guard let index = tasks.firstIndex(where: { $0.id == id }) else { return }
        deleteTask(at: IndexSet(integer: index))
    }
    
    func selectTask(_ task: PomodoroTask?) {
        selectedTask = task
    }
    
    func startFocusSession() {
        guard selectedTask != nil else { return }
        isTimerRunning = true
        currentPhase = .work
        remainingSeconds = workDuration
        showingFloatingWindow = true
        startTimer()
    }
    
    func stopTimer() {
        timer?.invalidate()
        timer = nil
        isTimerRunning = false
    }
    
    func resetTimer() {
        stopTimer()
        remainingSeconds = workDuration
        currentPhase = .work
    }
    
    func closeFloatingWindow() {
        showingFloatingWindow = false
    }
    
    private func startTimer() {
        timer = Timer.scheduledTimer(withTimeInterval: 1.0, repeats: true) { [weak self] _ in
            guard let self = self else { return }
            if self.remainingSeconds > 0 {
                self.remainingSeconds -= 1
            } else {
                self.timerCompleted()
            }
        }
    }
    
    private func timerCompleted() {
        if currentPhase == .work {
            completedWorkSessions += 1
            if let index = tasks.firstIndex(where: { $0.id == selectedTask?.id }) {
                tasks[index].completedPomodoros += 1
                selectedTask = tasks[index]
                saveTasks()
            }
            
            if completedWorkSessions % 4 == 0 {
                currentPhase = .longBreak
                remainingSeconds = longBreakDuration
            } else {
                currentPhase = .shortBreak
                remainingSeconds = shortBreakDuration
            }
        } else {
            currentPhase = .work
            remainingSeconds = workDuration
            stopTimer()
            showingFloatingWindow = false
        }
    }
    
    private func saveTasks() {
        if let encoded = try? JSONEncoder().encode(tasks) {
            UserDefaults.standard.set(encoded, forKey: "tasks")
        }
    }
    
    private func loadTasks() {
        if let data = UserDefaults.standard.data(forKey: "tasks"),
           let decoded = try? JSONDecoder().decode([PomodoroTask].self, from: data) {
            tasks = decoded
        }
    }
    
    func updateTaskOrder(from source: IndexSet, to destination: Int) {
        tasks.move(fromOffsets: source, toOffset: destination)
        saveTasks()
    }
    
    func toggleTaskCompletion(_ task: PomodoroTask) {
        if let index = tasks.firstIndex(where: { $0.id == task.id }) {
            tasks[index].isCompleted.toggle()
            saveTasks()
        }
    }
}
