import XCTest
import AppKit
@testable import Tomato

final class ThemeSemanticsTests: XCTestCase {
    private final class TestApplication: ApplicationIconSetting {
        var applicationIconImage: NSImage!
    }

    func test_theme_mode_has_three_options() {
        XCTAssertEqual(ThemeMode.allCases.count, 3)
        XCTAssertEqual(ThemeMode.allCases, [.glassVivid, .businessMotion, .verdantCalm])
    }

    func test_theme_mode_display_names_are_stable() {
        XCTAssertEqual(ThemeMode.glassVivid.displayName(language: .english), "Glass Vivid")
        XCTAssertEqual(ThemeMode.businessMotion.displayName(language: .english), "Business Motion")
        XCTAssertEqual(ThemeMode.verdantCalm.displayName(language: .english), "Verdant Calm")
        XCTAssertEqual(ThemeMode.glassVivid.displayName(language: .chinese), "玻璃炫彩")
        XCTAssertEqual(ThemeMode.businessMotion.displayName(language: .chinese), "商务律动")
        XCTAssertEqual(ThemeMode.verdantCalm.displayName(language: .chinese), "青岚护目")
    }

    func test_primary_tomato_color_is_defined() {
        let color = AppTheme.Colors.tomatoPrimary
        XCTAssertNotNil(color)
    }

    func test_glass_gradient_has_multiple_stops() {
        let gradient = AppTheme.Backgrounds.mainGradient
        XCTAssertGreaterThanOrEqual(gradient.stops.count, 3)
    }

    func test_business_theme_background_differs_from_glass_vivid() {
        let vivid = AppTheme.Backgrounds.mainGradient(for: .glassVivid)
        let business = AppTheme.Backgrounds.mainGradient(for: .businessMotion)
        XCTAssertNotEqual(vivid.stops.first?.color, business.stops.first?.color)
    }

    func test_phase_color_mapping_depends_on_theme_mode() {
        let vividWork = TimerPhase.work.themedColor(for: .glassVivid)
        let businessWork = TimerPhase.work.themedColor(for: .businessMotion)
        let verdantWork = TimerPhase.work.themedColor(for: .verdantCalm)
        XCTAssertNotEqual(vividWork, businessWork)
        XCTAssertNotEqual(vividWork, verdantWork)
        XCTAssertNotEqual(businessWork, verdantWork)
    }

    func test_verdant_calm_break_colors_use_distinct_green_levels() {
        let work = TimerPhase.work.themedColor(for: .verdantCalm)
        let shortBreak = TimerPhase.shortBreak.themedColor(for: .verdantCalm)
        let longBreak = TimerPhase.longBreak.themedColor(for: .verdantCalm)

        XCTAssertNotEqual(work, shortBreak)
        XCTAssertNotEqual(work, longBreak)
        XCTAssertNotEqual(shortBreak, longBreak)
    }

    func test_business_motion_uses_stronger_background_motion_tokens() {
        XCTAssertLessThan(
            AppTheme.Motion.backgroundDriftDuration(for: .businessMotion),
            AppTheme.Motion.backgroundDriftDuration(for: .glassVivid)
        )
        XCTAssertGreaterThan(
            AppTheme.Motion.backgroundDriftDistance(for: .businessMotion),
            AppTheme.Motion.backgroundDriftDistance(for: .glassVivid)
        )
    }

    func test_floating_window_panel_is_draggable_and_keeps_top_level() {
        let panel = FloatingWindowController.makePanel(
            contentRect: NSRect(x: 0, y: 0, width: 208, height: 232)
        )
        XCTAssertTrue(panel.isMovable)
        XCTAssertTrue(panel.isMovableByWindowBackground)
        XCTAssertEqual(panel.level, .statusBar)
    }

    func test_task_list_row_does_not_reselect_task_in_on_tap_gesture() throws {
        let thisFileURL = URL(fileURLWithPath: #filePath)
        let projectRoot = thisFileURL.deletingLastPathComponent().deletingLastPathComponent()
        let contentViewPath = projectRoot
            .appendingPathComponent("Tomato")
            .appendingPathComponent("Views")
            .appendingPathComponent("ContentView.swift")

        let content = try String(contentsOf: contentViewPath, encoding: .utf8)
        let pattern = #"\.onTapGesture\s*\{\s*taskStore\.selectTask\(task\)"#
        let hasManualReselect = content.range(of: pattern, options: .regularExpression) != nil

        XCTAssertFalse(
            hasManualReselect,
            "List(selection:) should own selection updates; avoid publishing selectedTask inside row tap gesture."
        )
    }

    func test_task_list_does_not_bind_selection_directly_to_observable_object() throws {
        let thisFileURL = URL(fileURLWithPath: #filePath)
        let projectRoot = thisFileURL.deletingLastPathComponent().deletingLastPathComponent()
        let contentViewPath = projectRoot
            .appendingPathComponent("Tomato")
            .appendingPathComponent("Views")
            .appendingPathComponent("ContentView.swift")

        let content = try String(contentsOf: contentViewPath, encoding: .utf8)
        XCTAssertFalse(
            content.contains("List(selection: $taskStore.selectedTask)"),
            "Avoid binding List(selection:) directly to ObservableObject @Published state."
        )
    }

    func test_task_list_does_not_use_bidirectional_scroll_layout() throws {
        let thisFileURL = URL(fileURLWithPath: #filePath)
        let projectRoot = thisFileURL.deletingLastPathComponent().deletingLastPathComponent()
        let contentViewPath = projectRoot
            .appendingPathComponent("Tomato")
            .appendingPathComponent("Views")
            .appendingPathComponent("ContentView.swift")

        let content = try String(contentsOf: contentViewPath, encoding: .utf8)
        XCTAssertFalse(
            content.contains("ScrollView([.horizontal, .vertical], showsIndicators: true)"),
            "Task list should keep a vertical stack aligned from top-left instead of a centered two-axis scroll layout."
        )
    }

    func test_runtime_icon_apply_skips_loading_when_application_is_missing() {
        var loadCalls = 0

        RuntimeAppIconApplier.applyIfPossible(to: nil) {
            loadCalls += 1
            return NSImage(size: NSSize(width: 1, height: 1))
        }

        XCTAssertEqual(loadCalls, 0)
    }

    func test_runtime_icon_apply_sets_icon_when_available() {
        let app = TestApplication()
        let expectedIcon = NSImage(size: NSSize(width: 1, height: 1))

        RuntimeAppIconApplier.applyIfPossible(to: app) {
            expectedIcon
        }

        XCTAssertTrue(app.applicationIconImage === expectedIcon)
    }
}
