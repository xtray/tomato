import Foundation
import Combine
import SwiftUI

enum CompletionChimeEvent: Equatable {
    case workCompleted
    case breakCompleted
}

enum CompletionChimePreferences {
    static let enabledKey = "completionChimesEnabled"
    static let volumeKey = "completionChimeVolume"
    static let defaultEnabled = true
    static let defaultVolume = 0.8
    static let minVolume = 0.0
    static let maxVolume = 1.0

    static func loadEnabled(from defaults: UserDefaults = .standard, key: String = enabledKey) -> Bool {
        guard defaults.object(forKey: key) != nil else {
            return defaultEnabled
        }

        return defaults.bool(forKey: key)
    }

    static func saveEnabled(_ value: Bool, to defaults: UserDefaults = .standard, key: String = enabledKey) {
        defaults.set(value, forKey: key)
    }

    static func loadVolume(from defaults: UserDefaults = .standard, key: String = volumeKey) -> Double {
        guard defaults.object(forKey: key) != nil else {
            return defaultVolume
        }

        return normalized(defaults.double(forKey: key))
    }

    static func saveVolume(_ value: Double, to defaults: UserDefaults = .standard, key: String = volumeKey) {
        defaults.set(normalized(value), forKey: key)
    }

    static func normalized(_ value: Double) -> Double {
        guard value.isFinite else {
            return defaultVolume
        }

        return min(max(value, minVolume), maxVolume)
    }
}

enum TimerPhase: String, Codable {
    case work
    case shortBreak
    case longBreak
    
    func displayName(language: AppLanguage) -> String {
        switch self {
        case .work:
            return AppText.string("timer.phase.work", language: language)
        case .shortBreak:
            return AppText.string("timer.phase.short_break", language: language)
        case .longBreak:
            return AppText.string("timer.phase.long_break", language: language)
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

enum FocusControlState: Equatable {
    case focus
    case run
    case pause
}

class TaskStore: ObservableObject {
    private static let defaultWorkDuration = 25 * 60
    private static let defaultShortBreakDuration = 5 * 60
    private static let defaultLongBreakDuration = 15 * 60

    @Published var tasks: [PomodoroTask] = []
    @Published var selectedTask: PomodoroTask?
    @Published private(set) var sessionTaskID: UUID?
    @Published var isTimerRunning: Bool = false
    @Published var remainingSeconds: Int = 25 * 60
    @Published var currentPhase: TimerPhase = .work
    @Published var showingFloatingWindow: Bool = false
    @Published var showingSettings: Bool = false
    @Published var appLanguage: AppLanguage {
        didSet {
            LanguagePreferences.save(appLanguage)
        }
    }
    @Published var themeMode: ThemeMode {
        didSet {
            ThemePreferences.save(themeMode)
        }
    }
    @Published var floatingWindowOpacity: Double {
        didSet {
            let normalized = FloatingWindowOpacityPreferences.normalized(floatingWindowOpacity)
            if abs(normalized - floatingWindowOpacity) > 0.0001 {
                floatingWindowOpacity = normalized
                return
            }

            FloatingWindowOpacityPreferences.save(normalized)
        }
    }
    @Published var completionChimesEnabled: Bool {
        didSet {
            CompletionChimePreferences.saveEnabled(completionChimesEnabled)
        }
    }
    @Published var completionChimeVolume: Double {
        didSet {
            let normalized = CompletionChimePreferences.normalized(completionChimeVolume)
            if abs(normalized - completionChimeVolume) > 0.0001 {
                completionChimeVolume = normalized
                return
            }

            CompletionChimePreferences.saveVolume(normalized)
        }
    }
    
    @Published var workDuration: Int {
        didSet {
            UserDefaults.standard.set(workDuration, forKey: "workDuration")
            if currentPhase == .work && !isTimerRunning && sessionTaskID == nil {
                remainingSeconds = workDuration
            }
        }
    }
    
    @Published var shortBreakDuration: Int {
        didSet {
            UserDefaults.standard.set(shortBreakDuration, forKey: "shortBreakDuration")
            if currentPhase == .shortBreak && !isTimerRunning && sessionTaskID == nil {
                remainingSeconds = shortBreakDuration
            }
        }
    }
    
    @Published var longBreakDuration: Int {
        didSet {
            UserDefaults.standard.set(longBreakDuration, forKey: "longBreakDuration")
            if currentPhase == .longBreak && !isTimerRunning && sessionTaskID == nil {
                remainingSeconds = longBreakDuration
            }
        }
    }
    
    private var timer: Timer?
    private var completedWorkSessions: Int = 0
    private var sessionTaskSnapshot: PomodoroTask?
    var playCompletionChime: ((CompletionChimeEvent, Double) -> Void)?

    var timerDisplayTask: PomodoroTask? {
        guard let sessionTaskID else {
            return selectedTask
        }

        return tasks.first(where: { $0.id == sessionTaskID }) ?? sessionTaskSnapshot
    }

    var canStartOrResumeFocus: Bool {
        selectedTask != nil || sessionTaskID != nil
    }

    var focusControlState: FocusControlState {
        if isTimerRunning {
            return .pause
        }

        if sessionTaskID != nil {
            return .run
        }

        return .focus
    }

    var timerStatusTextKey: String {
        switch currentPhase {
        case .shortBreak:
            return "timer.phase.short_break"
        case .longBreak:
            return "timer.phase.long_break"
        case .work:
            if isTimerRunning {
                return "timer.phase.work"
            }

            return sessionTaskID != nil ? "timer.status.paused" : "timer.phase.ready"
        }
    }
    
    init() {
        let savedWorkDuration = UserDefaults.standard.integer(forKey: "workDuration")
        let savedShortBreakDuration = UserDefaults.standard.integer(forKey: "shortBreakDuration")
        let savedLongBreakDuration = UserDefaults.standard.integer(forKey: "longBreakDuration")
        self.appLanguage = LanguagePreferences.load()
        self.themeMode = ThemePreferences.load()
        self.floatingWindowOpacity = FloatingWindowOpacityPreferences.load()
        self.completionChimesEnabled = CompletionChimePreferences.loadEnabled()
        self.completionChimeVolume = CompletionChimePreferences.loadVolume()
        
        self.workDuration = savedWorkDuration > 0 ? savedWorkDuration : Self.defaultWorkDuration
        self.shortBreakDuration = savedShortBreakDuration > 0 ? savedShortBreakDuration : Self.defaultShortBreakDuration
        self.longBreakDuration = savedLongBreakDuration > 0 ? savedLongBreakDuration : Self.defaultLongBreakDuration
        self.playCompletionChime = { event, volume in
            CompletionChimePlayer.shared.play(event: event, volume: volume)
        }
        
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
        let activeSessionID = sessionTaskID
        let isDeletingSelectedTask = offsets.contains { index in
            tasks.indices.contains(index) && tasks[index].id == selectedID
        }
        let isDeletingActiveSessionTask = offsets.contains { index in
            tasks.indices.contains(index) && tasks[index].id == activeSessionID
        }

        tasks.remove(atOffsets: offsets)
        if isDeletingSelectedTask {
            selectedTask = nil
        }
        if isDeletingActiveSessionTask {
            resetTimer()
            showingFloatingWindow = false
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

    func startFocusSessionForDoubleClick(_ task: PomodoroTask) {
        selectedTask = task

        if sessionTaskID == task.id {
            if isTimerRunning {
                showingFloatingWindow = true
                return
            }

            startFocusSession()
            return
        }

        guard !isTimerRunning, sessionTaskID == nil else {
            return
        }
        startFocusSession()
    }
    
    func startFocusSession() {
        if isTimerRunning {
            return
        }

        if sessionTaskID != nil {
            isTimerRunning = true
            showingFloatingWindow = true
            startTimer()
            return
        }

        guard let selectedTask else { return }
        sessionTaskID = selectedTask.id
        sessionTaskSnapshot = selectedTask
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
        clearSessionTask()
        remainingSeconds = workDuration
        currentPhase = .work
    }

    func resetSettings() {
        themeMode = .glassVivid
        appLanguage = AppLanguage.fallback()
        floatingWindowOpacity = FloatingWindowOpacityPreferences.defaultValue
        completionChimesEnabled = CompletionChimePreferences.defaultEnabled
        completionChimeVolume = CompletionChimePreferences.defaultVolume
        workDuration = Self.defaultWorkDuration
        shortBreakDuration = Self.defaultShortBreakDuration
        longBreakDuration = Self.defaultLongBreakDuration
    }
    
    func closeFloatingWindow() {
        showingFloatingWindow = false
    }
    
    private func startTimer() {
        timer?.invalidate()
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
            if let sessionTaskID,
               let index = tasks.firstIndex(where: { $0.id == sessionTaskID }) {
                tasks[index].completedPomodoros += 1
                sessionTaskSnapshot = tasks[index]
                if selectedTask?.id == sessionTaskID {
                    selectedTask = tasks[index]
                }
                saveTasks()
            }
            
            if completedWorkSessions % 4 == 0 {
                currentPhase = .longBreak
                remainingSeconds = longBreakDuration
            } else {
                currentPhase = .shortBreak
                remainingSeconds = shortBreakDuration
            }
            triggerCompletionChime(.workCompleted)
        } else {
            currentPhase = .work
            remainingSeconds = workDuration
            stopTimer()
            clearSessionTask()
            showingFloatingWindow = false
            triggerCompletionChime(.breakCompleted)
        }
    }

    func timerCompletedForTesting() {
        timerCompleted()
    }

    private func triggerCompletionChime(_ event: CompletionChimeEvent) {
        guard completionChimesEnabled else {
            return
        }

        playCompletionChime?(event, completionChimeVolume)
    }

    private func clearSessionTask() {
        sessionTaskID = nil
        sessionTaskSnapshot = nil
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

    func moveTask(draggedTaskID: UUID, beforeTaskID: UUID?) {
        guard let fromIndex = tasks.firstIndex(where: { $0.id == draggedTaskID }) else { return }

        var reordered = tasks
        let draggedTask = reordered.remove(at: fromIndex)

        if let beforeTaskID {
            guard let targetIndex = reordered.firstIndex(where: { $0.id == beforeTaskID }) else { return }
            reordered.insert(draggedTask, at: targetIndex)
        } else {
            reordered.append(draggedTask)
        }

        tasks = reordered
        saveTasks()
    }
    
    func toggleTaskCompletion(_ task: PomodoroTask) {
        if let index = tasks.firstIndex(where: { $0.id == task.id }) {
            tasks[index].isCompleted.toggle()
            saveTasks()
        }
    }
}
