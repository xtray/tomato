import CoreGraphics

enum TimerProgressCalculator {
    static func progress(remaining: Int, total: Int) -> CGFloat {
        guard total > 0 else { return 0 }
        let raw = CGFloat(remaining) / CGFloat(total)
        if raw < 0 { return 0 }
        if raw > 1 { return 1 }
        return raw
    }
}
