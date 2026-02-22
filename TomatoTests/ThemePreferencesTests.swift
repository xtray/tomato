import XCTest
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
}
