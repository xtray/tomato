import XCTest
@testable import Tomato

final class TaskStoreThemeTests: XCTestCase {
    override func setUp() {
        super.setUp()
        ThemePreferences.save(.glassVivid)
        LanguagePreferences.save(.english)
    }

    override func tearDown() {
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

    func test_timer_display_task_tracks_selection_after_timer_stops() {
        UserDefaults.standard.removeObject(forKey: "tasks")

        let store = TaskStore()
        store.addTask(title: "Task A")
        store.addTask(title: "Task B")

        let taskA = store.tasks[0]
        let taskB = store.tasks[1]

        store.selectTask(taskA)
        store.startFocusSession()
        store.selectTask(taskB)
        store.stopTimer()

        XCTAssertEqual(store.timerDisplayTask?.id, taskB.id)
    }
}
