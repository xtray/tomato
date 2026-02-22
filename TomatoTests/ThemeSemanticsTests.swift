import XCTest
import AppKit
@testable import Tomato

final class ThemeSemanticsTests: XCTestCase {
    func test_theme_mode_has_two_options() {
        XCTAssertEqual(ThemeMode.allCases.count, 2)
        XCTAssertEqual(ThemeMode.allCases, [.glassVivid, .businessMotion])
    }

    func test_theme_mode_display_names_are_stable() {
        XCTAssertEqual(ThemeMode.glassVivid.displayName(language: .english), "Glass Vivid")
        XCTAssertEqual(ThemeMode.businessMotion.displayName(language: .english), "Business Motion")
        XCTAssertEqual(ThemeMode.glassVivid.displayName(language: .chinese), "玻璃炫彩")
        XCTAssertEqual(ThemeMode.businessMotion.displayName(language: .chinese), "商务律动")
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
        XCTAssertNotEqual(vividWork, businessWork)
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

    func test_floating_window_panel_is_draggable_by_background() {
        let panel = FloatingWindowController.makePanel(
            contentRect: NSRect(x: 0, y: 0, width: 208, height: 232)
        )
        XCTAssertTrue(panel.isMovable)
        XCTAssertTrue(panel.isMovableByWindowBackground)
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
}
