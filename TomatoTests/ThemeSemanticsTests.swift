import XCTest
import AppKit
import SwiftUI
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

    func test_first_mouse_hosting_view_accepts_click_in_inactive_window() {
        let view = FirstMouseHostingView(rootView: Text("Tomato"))
        XCTAssertTrue(view.acceptsFirstMouse(for: nil))
    }

    func test_main_window_configuration_locks_window_to_fixed_size_and_disables_resize() {
        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 920, height: 640),
            styleMask: [.titled, .closable, .miniaturizable, .resizable],
            backing: .buffered,
            defer: false
        )

        MainWindowConfiguration.apply(to: window)

        XCTAssertFalse(window.styleMask.contains(.resizable))
        XCTAssertEqual(window.minSize.width, 760, accuracy: 0.001)
        XCTAssertEqual(window.minSize.height, 500, accuracy: 0.001)
        XCTAssertEqual(window.maxSize.width, 760, accuracy: 0.001)
        XCTAssertEqual(window.maxSize.height, 500, accuracy: 0.001)
        XCTAssertEqual(window.contentRect(forFrameRect: window.frame).size.width, 760, accuracy: 0.001)
        XCTAssertEqual(window.contentRect(forFrameRect: window.frame).size.height, 500, accuracy: 0.001)
    }

    func test_main_window_configuration_installs_resize_guard_that_rejects_resize_and_zoom() throws {
        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 920, height: 640),
            styleMask: [.titled, .closable, .miniaturizable, .resizable],
            backing: .buffered,
            defer: false
        )

        MainWindowConfiguration.apply(to: window)

        let resizeGuard = try XCTUnwrap(window.delegate as? MainWindowResizeGuard)
        let fixedFrameSize = window.frameRect(
            forContentRect: NSRect(origin: .zero, size: MainWindowConfiguration.fixedContentSize)
        ).size

        XCTAssertEqual(resizeGuard.windowWillResize(window, to: NSSize(width: 1100, height: 720)), fixedFrameSize)
        XCTAssertFalse(resizeGuard.windowShouldZoom(window, toFrame: NSRect(x: 0, y: 0, width: 1200, height: 800)))
    }

    func test_main_window_configuration_installs_frame_cursor_shields_on_window_frame_edges() throws {
        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 760, height: 500),
            styleMask: [.titled, .closable, .miniaturizable, .resizable],
            backing: .buffered,
            defer: false
        )

        MainWindowConfiguration.apply(to: window)

        let contentView = try XCTUnwrap(window.contentView)
        let frameView = try XCTUnwrap(contentView.superview)
        let shieldViews = frameView.subviews.compactMap { $0 as? MainWindowFrameCursorShieldView }

        XCTAssertFalse(shieldViews.isEmpty)
        XCTAssertTrue(shieldViews.allSatisfy { $0.hitTest(NSPoint(x: 1, y: 1)) == nil })
        XCTAssertTrue(shieldViews.contains { $0.frame.contains(NSPoint(x: 4, y: 250)) })
        XCTAssertTrue(shieldViews.contains { $0.frame.contains(NSPoint(x: 756, y: 250)) })
        XCTAssertTrue(shieldViews.contains { $0.frame.contains(NSPoint(x: 380, y: 4)) })
        XCTAssertTrue(shieldViews.contains { $0.frame.contains(NSPoint(x: 380, y: 516)) })
        XCTAssertFalse(
            shieldViews.contains { $0.frame.contains(NSPoint(x: contentView.frame.midX, y: contentView.frame.midY)) },
            "Frame cursor shields should not cover the content center."
        )
    }

    func test_main_window_cursor_shield_layout_covers_frame_edges_without_covering_content_center() {
        let frameBounds = NSRect(x: 0, y: 0, width: 760, height: 532)
        let contentFrame = NSRect(x: 0, y: 0, width: 760, height: 500)

        let rects = MainWindowFrameCursorShieldLayout.makeFrameRects(
            frameBounds: frameBounds,
            contentFrame: contentFrame
        )

        XCTAssertFalse(rects.isEmpty)
        XCTAssertTrue(rects.contains { $0.contains(NSPoint(x: 4, y: 250)) })
        XCTAssertTrue(rects.contains { $0.contains(NSPoint(x: 756, y: 250)) })
        XCTAssertTrue(rects.contains { $0.contains(NSPoint(x: 380, y: 4)) })
        XCTAssertTrue(rects.contains { $0.contains(NSPoint(x: 380, y: 516)) })
        XCTAssertFalse(rects.contains { $0.contains(NSPoint(x: 380, y: 250)) })
    }

    func test_main_window_hosting_view_does_not_override_cursor_rects_to_arrow() throws {
        let thisFileURL = URL(fileURLWithPath: #filePath)
        let projectRoot = thisFileURL.deletingLastPathComponent().deletingLastPathComponent()
        let appPath = projectRoot
            .appendingPathComponent("Tomato")
            .appendingPathComponent("TomatoApp.swift")

        let content = try String(contentsOf: appPath, encoding: .utf8)

        XCTAssertFalse(
            content.contains("final class MainWindowConfigurationHostingView<Content: View>: NSHostingView<Content> {\n    override func viewDidMoveToWindow() {\n        super.viewDidMoveToWindow()\n        MainWindowConfiguration.apply(to: window)\n        window?.invalidateCursorRects(for: self)\n    }\n\n    override func resetCursorRects()"),
            "The content hosting view should not own the frame cursor override because that suppresses content-area cursors instead of fixing the window frame."
        )
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

    func test_task_row_uses_exclusive_tap_gesture_for_single_and_double_click() throws {
        let thisFileURL = URL(fileURLWithPath: #filePath)
        let projectRoot = thisFileURL.deletingLastPathComponent().deletingLastPathComponent()
        let contentViewPath = projectRoot
            .appendingPathComponent("Tomato")
            .appendingPathComponent("Views")
            .appendingPathComponent("ContentView.swift")

        let content = try String(contentsOf: contentViewPath, encoding: .utf8)
        XCTAssertTrue(
            content.contains(".exclusively(before:"),
            "Task rows should use one exclusive gesture chain so double-click remains reliable after returning from the floating window."
        )
        XCTAssertFalse(
            content.contains(".onTapGesture(count: 2)"),
            "Task rows should not stack an independent double-click recognizer on top of the single-click recognizer."
        )
    }

    func test_app_wraps_content_in_first_mouse_container() throws {
        let thisFileURL = URL(fileURLWithPath: #filePath)
        let projectRoot = thisFileURL.deletingLastPathComponent().deletingLastPathComponent()
        let appPath = projectRoot
            .appendingPathComponent("Tomato")
            .appendingPathComponent("TomatoApp.swift")

        let content = try String(contentsOf: appPath, encoding: .utf8)
        XCTAssertTrue(
            content.contains("FirstMouseContainer"),
            "The main window content should accept first mouse after returning from the floating panel."
        )
    }

    func test_app_wraps_content_in_main_window_configuration_container() throws {
        let thisFileURL = URL(fileURLWithPath: #filePath)
        let projectRoot = thisFileURL.deletingLastPathComponent().deletingLastPathComponent()
        let appPath = projectRoot
            .appendingPathComponent("Tomato")
            .appendingPathComponent("TomatoApp.swift")

        let content = try String(contentsOf: appPath, encoding: .utf8)
        XCTAssertTrue(
            content.contains("MainWindowConfigurationContainer"),
            "The main window content should apply the fixed-size macOS window configuration."
        )
    }

    func test_app_does_not_use_content_size_window_resizability() throws {
        let thisFileURL = URL(fileURLWithPath: #filePath)
        let projectRoot = thisFileURL.deletingLastPathComponent().deletingLastPathComponent()
        let appPath = projectRoot
            .appendingPathComponent("Tomato")
            .appendingPathComponent("TomatoApp.swift")

        let content = try String(contentsOf: appPath, encoding: .utf8)
        XCTAssertFalse(
            content.contains(".windowResizability(.contentSize)"),
            "The fixed-size main window should not expose SwiftUI content-size resizing hooks."
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
