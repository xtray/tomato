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

    func test_timer_status_text_key_is_ready_when_idle() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()

        XCTAssertEqual(store.timerStatusTextKey, "timer.phase.ready")
    }

    func test_timer_status_text_key_is_focusing_during_running_work_session() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")
        store.selectTask(store.tasks[0])

        store.startFocusSession()

        XCTAssertEqual(store.timerStatusTextKey, "timer.phase.work")
        store.stopTimer()
    }

    func test_timer_status_text_key_is_paused_for_resumable_work_session() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")
        store.selectTask(store.tasks[0])

        store.startFocusSession()
        store.stopTimer()

        XCTAssertEqual(store.timerStatusTextKey, "timer.status.paused")
    }

    func test_timer_status_text_key_uses_break_phase_keys() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")
        store.selectTask(store.tasks[0])
        store.startFocusSession()

        store.currentPhase = .shortBreak
        XCTAssertEqual(store.timerStatusTextKey, "timer.phase.short_break")

        store.currentPhase = .longBreak
        XCTAssertEqual(store.timerStatusTextKey, "timer.phase.long_break")
        store.stopTimer()
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

    func test_start_focus_for_task_when_idle_starts_session_and_shows_floating_window() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")
        store.addTask(title: "Task B")

        let taskB = store.tasks[1]

        store.startFocusSessionForDoubleClick(taskB)

        XCTAssertTrue(store.isTimerRunning)
        XCTAssertEqual(store.sessionTaskID, taskB.id)
        XCTAssertEqual(store.timerDisplayTask?.id, taskB.id)
        XCTAssertTrue(store.showingFloatingWindow)
        store.stopTimer()
    }

    func test_start_focus_for_task_when_paused_resumable_session_exists_does_not_replace_session() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")
        store.addTask(title: "Task B")

        let taskA = store.tasks[0]
        let taskB = store.tasks[1]

        store.selectTask(taskA)
        store.startFocusSession()
        store.remainingSeconds = store.workDuration - 30
        let pausedRemaining = store.remainingSeconds
        store.stopTimer()
        let floatingWindowVisibility = store.showingFloatingWindow

        store.startFocusSessionForDoubleClick(taskB)

        XCTAssertFalse(store.isTimerRunning)
        XCTAssertEqual(store.sessionTaskID, taskA.id)
        XCTAssertEqual(store.timerDisplayTask?.id, taskA.id)
        XCTAssertEqual(store.remainingSeconds, pausedRemaining)
        XCTAssertEqual(store.showingFloatingWindow, floatingWindowVisibility)
    }

    func test_start_focus_for_task_when_session_is_running_does_not_replace_session() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")
        store.addTask(title: "Task B")

        let taskA = store.tasks[0]
        let taskB = store.tasks[1]

        store.selectTask(taskA)
        store.startFocusSession()
        let runningRemaining = store.remainingSeconds

        store.startFocusSessionForDoubleClick(taskB)

        XCTAssertTrue(store.isTimerRunning)
        XCTAssertEqual(store.sessionTaskID, taskA.id)
        XCTAssertEqual(store.timerDisplayTask?.id, taskA.id)
        XCTAssertEqual(store.remainingSeconds, runningRemaining)
        XCTAssertTrue(store.showingFloatingWindow)
        store.stopTimer()
    }

    func test_start_focus_for_same_task_when_session_is_running_and_floating_window_is_closed_reopens_floating_window() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")

        let task = store.tasks[0]

        store.selectTask(task)
        store.startFocusSession()
        let runningRemaining = store.remainingSeconds
        store.closeFloatingWindow()

        store.startFocusSessionForDoubleClick(task)

        XCTAssertTrue(store.isTimerRunning)
        XCTAssertEqual(store.sessionTaskID, task.id)
        XCTAssertEqual(store.timerDisplayTask?.id, task.id)
        XCTAssertEqual(store.remainingSeconds, runningRemaining)
        XCTAssertTrue(store.showingFloatingWindow)
        store.stopTimer()
    }

    func test_same_task_double_click_resumes_paused_session() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")

        let task = store.tasks[0]

        store.selectTask(task)
        store.startFocusSession()
        store.remainingSeconds = store.workDuration - 30
        let pausedRemaining = store.remainingSeconds
        store.stopTimer()
        store.closeFloatingWindow()

        store.startFocusSessionForDoubleClick(task)

        XCTAssertTrue(store.isTimerRunning)
        XCTAssertEqual(store.sessionTaskID, task.id)
        XCTAssertEqual(store.timerDisplayTask?.id, task.id)
        XCTAssertEqual(store.remainingSeconds, pausedRemaining)
        XCTAssertTrue(store.showingFloatingWindow)
        store.stopTimer()
    }

    func test_start_focus_for_same_task_after_reset_starts_new_session_and_shows_floating_window() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")

        let task = store.tasks[0]

        store.startFocusSessionForDoubleClick(task)
        store.closeFloatingWindow()
        store.resetTimer()

        XCTAssertFalse(store.isTimerRunning)
        XCTAssertNil(store.sessionTaskID)
        XCTAssertFalse(store.showingFloatingWindow)

        store.startFocusSessionForDoubleClick(task)

        XCTAssertTrue(store.isTimerRunning)
        XCTAssertEqual(store.sessionTaskID, task.id)
        XCTAssertEqual(store.timerDisplayTask?.id, task.id)
        XCTAssertEqual(store.currentPhase, .work)
        XCTAssertEqual(store.remainingSeconds, store.workDuration)
        XCTAssertTrue(store.showingFloatingWindow)
        store.stopTimer()
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

    func test_task_store_loads_floating_window_opacity_from_preferences() {
        FloatingWindowOpacityPreferences.save(0.78)
        let store = TaskStore()
        XCTAssertEqual(store.floatingWindowOpacity, 0.78, accuracy: 0.0001)
    }

    func test_task_store_normalizes_and_persists_floating_window_opacity_changes() {
        let store = TaskStore()
        store.floatingWindowOpacity = 0.2

        XCTAssertEqual(store.floatingWindowOpacity, 0.5, accuracy: 0.0001)
        XCTAssertEqual(FloatingWindowOpacityPreferences.load(), 0.5, accuracy: 0.0001)
    }

    func test_reset_settings_restores_defaults_for_settings_menu_values() {
        let store = TaskStore()
        let expectedLanguage = AppLanguage.fallback()

        store.themeMode = .businessMotion
        store.appLanguage = expectedLanguage == .english ? .chinese : .english
        store.floatingWindowOpacity = 0.66
        store.workDuration = 45 * 60
        store.shortBreakDuration = 12 * 60
        store.longBreakDuration = 30 * 60

        store.resetSettings()

        XCTAssertEqual(store.themeMode, .glassVivid)
        XCTAssertEqual(store.appLanguage, expectedLanguage)
        XCTAssertEqual(store.floatingWindowOpacity, FloatingWindowOpacityPreferences.defaultValue, accuracy: 0.0001)
        XCTAssertEqual(store.workDuration, 25 * 60)
        XCTAssertEqual(store.shortBreakDuration, 5 * 60)
        XCTAssertEqual(store.longBreakDuration, 15 * 60)
    }

    func test_task_store_uses_default_completion_chime_settings() {
        let store = TaskStore()

        XCTAssertTrue(store.completionChimesEnabled)
        XCTAssertEqual(store.completionChimeVolume, 0.8, accuracy: 0.0001)
    }

    func test_reset_settings_restores_completion_chime_defaults() {
        let store = TaskStore()

        store.completionChimesEnabled = false
        store.completionChimeVolume = 0.35

        store.resetSettings()

        XCTAssertTrue(store.completionChimesEnabled)
        XCTAssertEqual(store.completionChimeVolume, 0.8, accuracy: 0.0001)
    }

    func test_timer_completion_after_work_phase_triggers_work_completion_chime() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        var playedEvents: [CompletionChimeEvent] = []
        store.playCompletionChime = { event, _ in
            playedEvents.append(event)
        }

        store.addTask(title: "Task A")
        store.selectTask(store.tasks[0])
        store.startFocusSession()
        store.remainingSeconds = 0

        store.timerCompletedForTesting()

        XCTAssertEqual(playedEvents, [.workCompleted])
        store.stopTimer()
    }

    func test_timer_completion_after_break_phase_triggers_break_completion_chime() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        var playedEvents: [CompletionChimeEvent] = []
        store.playCompletionChime = { event, _ in
            playedEvents.append(event)
        }

        store.currentPhase = .shortBreak
        store.remainingSeconds = 0
        store.startFocusSession()
        store.stopTimer()
        store.currentPhase = .shortBreak
        store.remainingSeconds = 0

        store.timerCompletedForTesting()

        XCTAssertEqual(playedEvents, [.breakCompleted])
        XCTAssertFalse(store.isTimerRunning)
    }

    func test_move_task_before_task_moves_item_upward() {
        let store = TaskStore()
        store.addTask(title: "A")
        store.addTask(title: "B")
        store.addTask(title: "C")
        store.addTask(title: "D")

        let draggedID = store.tasks[3].id
        let targetID = store.tasks[1].id
        store.moveTask(draggedTaskID: draggedID, beforeTaskID: targetID)

        XCTAssertEqual(store.tasks.map(\.title), ["A", "D", "B", "C"])
    }

    func test_move_task_before_task_moves_item_downward() {
        let store = TaskStore()
        store.addTask(title: "A")
        store.addTask(title: "B")
        store.addTask(title: "C")
        store.addTask(title: "D")

        let draggedID = store.tasks[1].id
        let targetID = store.tasks[3].id
        store.moveTask(draggedTaskID: draggedID, beforeTaskID: targetID)

        XCTAssertEqual(store.tasks.map(\.title), ["A", "C", "B", "D"])
    }

    func test_move_task_with_nil_target_moves_item_to_end() {
        let store = TaskStore()
        store.addTask(title: "A")
        store.addTask(title: "B")
        store.addTask(title: "C")

        let draggedID = store.tasks[0].id
        store.moveTask(draggedTaskID: draggedID, beforeTaskID: nil)

        XCTAssertEqual(store.tasks.map(\.title), ["B", "C", "A"])
    }

    func test_move_task_persists_order_after_reload() {
        let store = TaskStore()
        store.addTask(title: "A")
        store.addTask(title: "B")
        store.addTask(title: "C")

        let draggedID = store.tasks[2].id
        let targetID = store.tasks[0].id
        store.moveTask(draggedTaskID: draggedID, beforeTaskID: targetID)

        let reloadedStore = TaskStore()
        XCTAssertEqual(reloadedStore.tasks.map(\.title), ["C", "A", "B"])
    }

    private func resetPersistedState() {
        UserDefaults.standard.removeObject(forKey: "tasks")
        UserDefaults.standard.removeObject(forKey: "workDuration")
        UserDefaults.standard.removeObject(forKey: "shortBreakDuration")
        UserDefaults.standard.removeObject(forKey: "longBreakDuration")
        UserDefaults.standard.removeObject(forKey: "floatingWindowWidth")
        UserDefaults.standard.removeObject(forKey: "floatingWindowHeight")
        UserDefaults.standard.removeObject(forKey: FloatingWindowOpacityPreferences.defaultKey)
    }
}
