import XCTest
@testable import Tomato

final class TaskStoreThemeTests: XCTestCase {
    override func setUp() {
        super.setUp()
        resetPersistedState()
        ThemePreferences.save(.glassVivid)
        LanguagePreferences.save(.english)
    }

    override func tearDown() {
        resetPersistedState()
        ThemePreferences.save(.glassVivid)
        LanguagePreferences.save(.english)
        super.tearDown()
    }

    func test_task_store_loads_theme_mode_from_preferences() {
        ThemePreferences.save(.businessMotion)
        let store = TaskStore()
        XCTAssertEqual(store.themeMode, .businessMotion)
    }

    func test_task_store_persists_theme_mode_changes() {
        let store = TaskStore()
        store.themeMode = .businessMotion
        XCTAssertEqual(ThemePreferences.load(), .businessMotion)
    }

    func test_task_store_loads_app_language_from_preferences() {
        LanguagePreferences.save(.chinese)
        let store = TaskStore()
        XCTAssertEqual(store.appLanguage, .chinese)
    }

    func test_task_store_persists_app_language_changes() {
        let store = TaskStore()
        store.appLanguage = .chinese
        XCTAssertEqual(LanguagePreferences.load(locale: Locale(identifier: "en_US")), .chinese)
    }

    func test_timer_display_task_stays_locked_after_start_even_if_selection_changes() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")
        store.addTask(title: "Task B")

        let taskA = store.tasks[0]
        let taskB = store.tasks[1]

        store.selectTask(taskA)
        store.startFocusSession()
        store.selectTask(taskB)

        XCTAssertEqual(store.timerDisplayTask?.id, taskA.id)
        store.stopTimer()
    }

    func test_timer_display_task_tracks_selection_after_timer_resets() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")
        store.addTask(title: "Task B")

        let taskA = store.tasks[0]
        let taskB = store.tasks[1]

        store.selectTask(taskA)
        store.startFocusSession()
        store.selectTask(taskB)
        store.resetTimer()

        XCTAssertEqual(store.timerDisplayTask?.id, taskB.id)
    }

    func test_start_focus_after_stop_resumes_remaining_time_without_reset() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")
        let taskA = store.tasks[0]

        store.selectTask(taskA)
        store.startFocusSession()

        store.remainingSeconds = store.workDuration - 30
        let pausedRemaining = store.remainingSeconds
        store.stopTimer()

        store.startFocusSession()

        XCTAssertTrue(store.isTimerRunning)
        XCTAssertEqual(store.remainingSeconds, pausedRemaining)
        XCTAssertEqual(store.currentPhase, .work)
        XCTAssertEqual(store.timerDisplayTask?.id, taskA.id)
        store.stopTimer()
    }

    func test_focus_control_state_is_focus_when_session_not_started() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")
        store.selectTask(store.tasks[0])

        XCTAssertEqual(store.focusControlState, .focus)
    }

    func test_focus_control_state_transitions_from_pause_to_run_for_resumable_session() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")
        store.selectTask(store.tasks[0])

        store.startFocusSession()
        XCTAssertEqual(store.focusControlState, .pause)

        store.stopTimer()
        XCTAssertEqual(store.focusControlState, .run)
    }

    func test_focus_control_state_returns_to_focus_after_reset() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")
        store.selectTask(store.tasks[0])
        store.startFocusSession()
        store.stopTimer()

        store.resetTimer()

        XCTAssertEqual(store.focusControlState, .focus)
    }

    func test_deleting_active_session_task_resets_timer_to_initial_state() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")
        let task = store.tasks[0]

        store.selectTask(task)
        store.startFocusSession()
        store.remainingSeconds = store.workDuration - 45

        store.deleteTask(id: task.id)

        XCTAssertFalse(store.isTimerRunning)
        XCTAssertNil(store.sessionTaskID)
        XCTAssertNil(store.timerDisplayTask)
        XCTAssertEqual(store.currentPhase, .work)
        XCTAssertEqual(store.remainingSeconds, store.workDuration)
        XCTAssertEqual(store.focusControlState, .focus)
    }

    func test_task_store_restores_task_and_completed_pomodoro_count_from_persistence() throws {
        let taskID = UUID()
        let persistedTasks = [PomodoroTask(id: taskID, title: "Task A", completedPomodoros: 3, isCompleted: false)]
        let encoded = try JSONEncoder().encode(persistedTasks)
        UserDefaults.standard.set(encoded, forKey: "tasks")

        let reloadedStore = TaskStore()

        XCTAssertEqual(reloadedStore.tasks.count, 1)
        XCTAssertEqual(reloadedStore.tasks[0].id, taskID)
        XCTAssertEqual(reloadedStore.tasks[0].title, "Task A")
        XCTAssertEqual(reloadedStore.tasks[0].completedPomodoros, 3)
    }

    func test_task_store_restores_theme_and_durations_from_persistence() {
        let store = TaskStore()
        store.themeMode = .businessMotion
        store.workDuration = 45 * 60
        store.shortBreakDuration = 8 * 60
        store.longBreakDuration = 25 * 60

        let reloadedStore = TaskStore()

        XCTAssertEqual(reloadedStore.themeMode, .businessMotion)
        XCTAssertEqual(reloadedStore.workDuration, 45 * 60)
        XCTAssertEqual(reloadedStore.shortBreakDuration, 8 * 60)
        XCTAssertEqual(reloadedStore.longBreakDuration, 25 * 60)
    }

    private func resetPersistedState() {
        UserDefaults.standard.removeObject(forKey: "tasks")
        UserDefaults.standard.removeObject(forKey: "workDuration")
        UserDefaults.standard.removeObject(forKey: "shortBreakDuration")
        UserDefaults.standard.removeObject(forKey: "longBreakDuration")
        UserDefaults.standard.removeObject(forKey: "floatingWindowWidth")
        UserDefaults.standard.removeObject(forKey: "floatingWindowHeight")
    }
}
