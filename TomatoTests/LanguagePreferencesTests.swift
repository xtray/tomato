import XCTest
@testable import Tomato

final class LanguagePreferencesTests: XCTestCase {
    func test_language_preferences_falls_back_to_locale_for_invalid_raw_value() {
        let suite = "LanguagePrefsTests_\(UUID().uuidString)"
        guard let defaults = UserDefaults(suiteName: suite) else {
            XCTFail("Failed to create user defaults suite")
            return
        }

        defaults.set("invalid", forKey: "appLanguage")
        XCTAssertEqual(
            LanguagePreferences.load(from: defaults, key: "appLanguage", locale: Locale(identifier: "en_US")),
            .english
        )
        XCTAssertEqual(
            LanguagePreferences.load(from: defaults, key: "appLanguage", locale: Locale(identifier: "zh-Hans-CN")),
            .chinese
        )

        defaults.removePersistentDomain(forName: suite)
    }

    func test_language_preferences_round_trip() {
        let suite = "LanguagePrefsRoundTrip_\(UUID().uuidString)"
        guard let defaults = UserDefaults(suiteName: suite) else {
            XCTFail("Failed to create user defaults suite")
            return
        }

        LanguagePreferences.save(.english, to: defaults, key: "appLanguage")
        XCTAssertEqual(
            LanguagePreferences.load(from: defaults, key: "appLanguage", locale: Locale(identifier: "zh-Hans")),
            .english
        )

        defaults.removePersistentDomain(forName: suite)
    }
}
