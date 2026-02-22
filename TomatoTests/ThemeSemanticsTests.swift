import XCTest
@testable import Tomato

final class ThemeSemanticsTests: XCTestCase {
    func test_theme_mode_has_two_options() {
        XCTAssertEqual(ThemeMode.allCases.count, 2)
        XCTAssertEqual(ThemeMode.allCases, [.glassVivid, .businessMotion])
    }

    func test_theme_mode_display_names_are_stable() {
        XCTAssertEqual(ThemeMode.glassVivid.displayName, "Glass Vivid")
        XCTAssertEqual(ThemeMode.businessMotion.displayName, "Business Motion")
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
}
