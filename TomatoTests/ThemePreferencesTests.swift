import XCTest
import CoreGraphics
@testable import Tomato

final class ThemePreferencesTests: XCTestCase {
    func test_theme_preferences_falls_back_to_default_for_invalid_raw_value() {
        let suite = "ThemePrefsTests_\(UUID().uuidString)"
        guard let defaults = UserDefaults(suiteName: suite) else {
            XCTFail("Failed to create user defaults suite")
            return
        }

        defaults.set("invalid", forKey: "themeMode")
        XCTAssertEqual(ThemePreferences.load(from: defaults, key: "themeMode"), .glassVivid)

        defaults.removePersistentDomain(forName: suite)
    }

    func test_theme_preferences_round_trip() {
        let suite = "ThemePrefsRoundTrip_\(UUID().uuidString)"
        guard let defaults = UserDefaults(suiteName: suite) else {
            XCTFail("Failed to create user defaults suite")
            return
        }

        ThemePreferences.save(.businessMotion, to: defaults, key: "themeMode")
        XCTAssertEqual(ThemePreferences.load(from: defaults, key: "themeMode"), .businessMotion)

        defaults.removePersistentDomain(forName: suite)
    }

    func test_floating_window_size_preferences_falls_back_to_default_for_invalid_values() {
        let suite = "FloatingWindowSizeInvalid_\(UUID().uuidString)"
        guard let defaults = UserDefaults(suiteName: suite) else {
            XCTFail("Failed to create user defaults suite")
            return
        }

        defaults.set(-1, forKey: "floatingWindowWidth")
        defaults.set(0, forKey: "floatingWindowHeight")

        let size = FloatingWindowSizePreferences.load(from: defaults)
        XCTAssertEqual(size.width, FloatingWindowSizePreferences.defaultSize.width)
        XCTAssertEqual(size.height, FloatingWindowSizePreferences.defaultSize.height)

        defaults.removePersistentDomain(forName: suite)
    }

    func test_floating_window_size_preferences_round_trip() {
        let suite = "FloatingWindowSizeRoundTrip_\(UUID().uuidString)"
        guard let defaults = UserDefaults(suiteName: suite) else {
            XCTFail("Failed to create user defaults suite")
            return
        }

        FloatingWindowSizePreferences.save(CGSize(width: 372, height: 460), to: defaults)
        let restored = FloatingWindowSizePreferences.load(from: defaults)

        XCTAssertEqual(restored.width, 372)
        XCTAssertEqual(restored.height, 460)
        defaults.removePersistentDomain(forName: suite)
    }

    func test_bottom_left_drag_resize_proposal_uses_drag_translation() {
        let start = CGSize(width: 260, height: 300)
        let translation = CGSize(width: 32, height: -24)

        let proposed = FloatingWindowLayout.proposedSizeFromBottomLeftDrag(
            startSize: start,
            translation: translation
        )

        XCTAssertEqual(proposed.width, 228)
        XCTAssertEqual(proposed.height, 324)
    }

    func test_bottom_left_drag_resize_proposal_reduces_height_when_cursor_moves_up() {
        let start = CGSize(width: 260, height: 300)
        let translation = CGSize(width: 0, height: 20)

        let proposed = FloatingWindowLayout.proposedSizeFromBottomLeftDrag(
            startSize: start,
            translation: translation
        )

        XCTAssertEqual(proposed.height, 280)
    }

    func test_resized_frame_keeps_top_right_anchor() {
        let original = CGRect(x: 100, y: 300, width: 240, height: 260)
        let proposedSize = CGSize(width: 300, height: 220)

        let resized = FloatingWindowLayout.frameKeepingTopRight(
            originalFrame: original,
            proposedSize: proposedSize
        )

        XCTAssertEqual(resized.maxX, original.maxX)
        XCTAssertEqual(resized.maxY, original.maxY)
        XCTAssertEqual(resized.width, 300)
        XCTAssertEqual(resized.height, 220)
    }

    func test_resized_frame_with_fixed_anchor_does_not_accumulate_drift() {
        let anchor = CGPoint(x: 520, y: 640)
        let proposedSize = CGSize(width: 286, height: 314)

        let first = FloatingWindowLayout.frameKeepingTopRight(
            anchorTopRight: anchor,
            proposedSize: proposedSize
        )
        let second = FloatingWindowLayout.frameKeepingTopRight(
            anchorTopRight: anchor,
            proposedSize: proposedSize
        )

        XCTAssertEqual(first.origin.x, second.origin.x)
        XCTAssertEqual(first.origin.y, second.origin.y)
        XCTAssertEqual(first.maxX, anchor.x)
        XCTAssertEqual(first.maxY, anchor.y)
    }

    func test_timer_ring_diameter_scales_up_with_window_size() {
        let compact = FloatingWindowLayout.timerRingDiameter(
            for: CGSize(width: 208, height: 252),
            showsTaskTitle: true
        )
        let large = FloatingWindowLayout.timerRingDiameter(
            for: CGSize(width: 420, height: 460),
            showsTaskTitle: true
        )

        XCTAssertGreaterThan(large, compact)
        XCTAssertGreaterThanOrEqual(compact, FloatingWindowLayout.minimumRingDiameter)
    }

    func test_timer_ring_diameter_reserves_more_space_when_task_title_is_visible() {
        let withTask = FloatingWindowLayout.timerRingDiameter(
            for: CGSize(width: 320, height: 360),
            showsTaskTitle: true
        )
        let withoutTask = FloatingWindowLayout.timerRingDiameter(
            for: CGSize(width: 320, height: 360),
            showsTaskTitle: false
        )

        XCTAssertLessThan(withTask, withoutTask)
    }
}
