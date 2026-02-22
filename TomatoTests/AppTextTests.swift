import XCTest
@testable import Tomato

final class AppTextTests: XCTestCase {
    func test_settings_title_localizes_for_english_and_chinese() {
        XCTAssertEqual(AppText.string("settings.title", language: .english), "Settings")
        XCTAssertEqual(AppText.string("settings.title", language: .chinese), "设置")
    }

    func test_dynamic_duration_text_localizes_for_english_and_chinese() {
        XCTAssertEqual(
            AppText.string("settings.duration.current", language: .english, 25),
            "Current: 25 min"
        )
        XCTAssertEqual(
            AppText.string("settings.duration.current", language: .chinese, 25),
            "当前：25 分钟"
        )
    }
}
