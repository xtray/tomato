import XCTest
@testable import Tomato

final class TimerProgressCalculatorTests: XCTestCase {
    func test_progress_clamps_between_zero_and_one() {
        XCTAssertEqual(TimerProgressCalculator.progress(remaining: 60, total: 0), 0)
        XCTAssertEqual(TimerProgressCalculator.progress(remaining: -1, total: 1500), 0)
        XCTAssertEqual(TimerProgressCalculator.progress(remaining: 2000, total: 1500), 1)
    }

    func test_progress_returns_fraction_for_valid_values() {
        XCTAssertEqual(TimerProgressCalculator.progress(remaining: 750, total: 1500), 0.5)
    }

    func test_elapsed_progress_returns_complement_for_valid_values() {
        XCTAssertEqual(TimerProgressCalculator.elapsedProgress(remaining: 1500, total: 1500), 0)
        XCTAssertEqual(TimerProgressCalculator.elapsedProgress(remaining: 750, total: 1500), 0.5)
        XCTAssertEqual(TimerProgressCalculator.elapsedProgress(remaining: 0, total: 1500), 1)
    }

    func test_elapsed_progress_clamps_between_zero_and_one() {
        XCTAssertEqual(TimerProgressCalculator.elapsedProgress(remaining: 60, total: 0), 1)
        XCTAssertEqual(TimerProgressCalculator.elapsedProgress(remaining: -1, total: 1500), 1)
        XCTAssertEqual(TimerProgressCalculator.elapsedProgress(remaining: 2000, total: 1500), 0)
    }
}
