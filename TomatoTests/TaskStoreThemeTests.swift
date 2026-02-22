import XCTest
@testable import Tomato

final class TaskStoreThemeTests: XCTestCase {
    override func setUp() {
        super.setUp()
        ThemePreferences.save(.glassVivid)
    }

    override func tearDown() {
        ThemePreferences.save(.glassVivid)
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
}
