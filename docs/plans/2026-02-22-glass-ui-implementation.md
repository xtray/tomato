# Glass UI Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将 Tomato 的主窗口、设置窗和悬浮计时窗升级为统一的现代 Glass 风格，并保持现有业务行为不变。

**Architecture:** 新增 `Theme` 层集中定义颜色、材质、圆角和阴影；新增可复用 Glass 组件承载卡片和按钮样式；在三个现有视图中替换视觉结构但不改 `TaskStore` 业务流。为降低回归风险，先补最小化 `TomatoTests` 并通过纯函数与主题语义测试驱动关键样式逻辑。

**Tech Stack:** Swift 5.9, SwiftUI, AppKit, XCTest, XcodeGen, xcodebuild

---

### Task 1: 建立最小测试基线（TomatoTests）

**Files:**
- Create: `TomatoTests/ThemeSemanticsTests.swift`
- Modify: `project.yml`
- Modify: `Tomato.xcodeproj/project.pbxproj` (通过 `xcodegen` 生成)

**Step 1: Write the failing test**

```swift
import XCTest
@testable import Tomato

final class ThemeSemanticsTests: XCTestCase {
    func test_primary_tomato_color_is_defined() {
        let color = AppTheme.Colors.tomatoPrimary
        XCTAssertNotNil(color)
    }
}
```

**Step 2: Run test to verify it fails**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test`
Expected: FAIL，提示 `AppTheme` 未定义或测试 target 未配置完成。

**Step 3: Write minimal implementation**

```yaml
targets:
  TomatoTests:
    type: bundle.unit-test
    platform: macOS
    sources:
      - TomatoTests
    dependencies:
      - target: Tomato
```

执行：`xcodegen generate` 以生成测试 target。

**Step 4: Run test to verify it passes**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test`
Expected: PASS（测试基础设施可运行；此时主题语义测试在 Task 2 通过）。

**Step 5: Commit**

```bash
git add project.yml Tomato.xcodeproj/project.pbxproj TomatoTests/ThemeSemanticsTests.swift
git commit -m "test: add TomatoTests baseline for UI theme TDD"
```

### Task 2: 主题层实现（AppTheme）

**Files:**
- Create: `Tomato/Theme/AppTheme.swift`
- Modify: `TomatoTests/ThemeSemanticsTests.swift`

**Step 1: Write the failing test**

```swift
func test_glass_gradient_has_multiple_stops() {
    let gradient = AppTheme.Backgrounds.mainGradient
    XCTAssertGreaterThanOrEqual(gradient.stops.count, 3)
}

func test_timer_ring_colors_cover_three_phases() {
    XCTAssertEqual(AppTheme.Colors.phaseWorkName, "work")
    XCTAssertEqual(AppTheme.Colors.phaseShortBreakName, "shortBreak")
    XCTAssertEqual(AppTheme.Colors.phaseLongBreakName, "longBreak")
}
```

**Step 2: Run test to verify it fails**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/ThemeSemanticsTests`
Expected: FAIL，缺少 `Backgrounds` 或 phase 语义常量。

**Step 3: Write minimal implementation**

```swift
enum AppTheme {
    enum Colors {
        static let tomatoPrimary = Color(red: 0.86, green: 0.22, blue: 0.20)
        static let phaseWorkName = "work"
        static let phaseShortBreakName = "shortBreak"
        static let phaseLongBreakName = "longBreak"
    }

    enum Backgrounds {
        static let mainGradient = Gradient(stops: [
            .init(color: Color(red: 0.98, green: 0.96, blue: 0.95), location: 0.0),
            .init(color: Color(red: 0.94, green: 0.96, blue: 0.98), location: 0.5),
            .init(color: Color(red: 0.98, green: 0.98, blue: 0.99), location: 1.0)
        ])
    }
}
```

**Step 4: Run test to verify it passes**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/ThemeSemanticsTests`
Expected: PASS。

**Step 5: Commit**

```bash
git add Tomato/Theme/AppTheme.swift TomatoTests/ThemeSemanticsTests.swift
git commit -m "feat: add centralized glass theme tokens"
```

### Task 3: 组件层实现（GlassComponents）

**Files:**
- Create: `Tomato/Theme/GlassComponents.swift`
- Create: `Tomato/Theme/TimerProgressCalculator.swift`
- Create: `TomatoTests/TimerProgressCalculatorTests.swift`

**Step 1: Write the failing test**

```swift
import XCTest
@testable import Tomato

final class TimerProgressCalculatorTests: XCTestCase {
    func test_progress_clamps_between_zero_and_one() {
        XCTAssertEqual(TimerProgressCalculator.progress(remaining: 60, total: 0), 0)
        XCTAssertEqual(TimerProgressCalculator.progress(remaining: -1, total: 1500), 0)
        XCTAssertEqual(TimerProgressCalculator.progress(remaining: 2000, total: 1500), 1)
    }
}
```

**Step 2: Run test to verify it fails**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/TimerProgressCalculatorTests`
Expected: FAIL，`TimerProgressCalculator` 未定义。

**Step 3: Write minimal implementation**

```swift
enum TimerProgressCalculator {
    static func progress(remaining: Int, total: Int) -> CGFloat {
        guard total > 0 else { return 0 }
        let raw = CGFloat(remaining) / CGFloat(total)
        return min(max(raw, 0), 1)
    }
}
```

并实现 `GlassCard`、`PrimaryGlassButtonStyle`、`SecondaryGlassButtonStyle`。

**Step 4: Run test to verify it passes**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/TimerProgressCalculatorTests`
Expected: PASS。

**Step 5: Commit**

```bash
git add Tomato/Theme/GlassComponents.swift Tomato/Theme/TimerProgressCalculator.swift TomatoTests/TimerProgressCalculatorTests.swift
git commit -m "feat: add glass UI primitives and timer progress helper"
```

### Task 4: 主窗口视觉改造（ContentView）

**Files:**
- Modify: `Tomato/Views/ContentView.swift`
- Test: `TomatoTests/TimerProgressCalculatorTests.swift`

**Step 1: Write the failing test**

```swift
func test_progress_returns_one_for_full_remaining_time() {
    XCTAssertEqual(TimerProgressCalculator.progress(remaining: 1500, total: 1500), 1)
}
```

**Step 2: Run test to verify it fails**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/TimerProgressCalculatorTests`
Expected: FAIL（在修改 helper 时先引入预期失败案例）。

**Step 3: Write minimal implementation**

```swift
var timerProgress: CGFloat {
    TimerProgressCalculator.progress(
        remaining: taskStore.remainingSeconds,
        total: currentPhaseDuration
    )
}
```

并将主界面替换为 `GlassCard` 分层与番茄红主视觉按钮。

**Step 4: Run test to verify it passes**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/TimerProgressCalculatorTests`
Expected: PASS。

**Step 5: Commit**

```bash
git add Tomato/Views/ContentView.swift TomatoTests/TimerProgressCalculatorTests.swift
git commit -m "feat: redesign main window with glass visual language"
```

### Task 5: 设置窗与悬浮窗统一改造

**Files:**
- Modify: `Tomato/Views/SettingsView.swift`
- Modify: `Tomato/Views/FloatingWindowManager.swift`

**Step 1: Write the failing test**

```swift
func test_progress_handles_remaining_less_than_zero() {
    XCTAssertEqual(TimerProgressCalculator.progress(remaining: -10, total: 1200), 0)
}
```

**Step 2: Run test to verify it fails**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/TimerProgressCalculatorTests`
Expected: FAIL（先写边界期望）。

**Step 3: Write minimal implementation**

```swift
// reuse GlassCard and button styles in SettingsView / FloatingTimerContentView
// keep existing TaskStore actions unchanged
```

**Step 4: Run test to verify it passes**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/TimerProgressCalculatorTests`
Expected: PASS。

**Step 5: Commit**

```bash
git add Tomato/Views/SettingsView.swift Tomato/Views/FloatingWindowManager.swift
git commit -m "feat: apply glass styling to settings and floating timer"
```

### Task 6: 全量验证与收尾

**Files:**
- Verify: `Tomato/Views/ContentView.swift`
- Verify: `Tomato/Views/SettingsView.swift`
- Verify: `Tomato/Views/FloatingWindowManager.swift`
- Verify: `Tomato/Theme/AppTheme.swift`
- Verify: `Tomato/Theme/GlassComponents.swift`

**Step 1: Write the failing test**

```swift
// N/A: 收尾任务不新增行为，仅执行全量验证
```

**Step 2: Run test to verify it fails**

```bash
# N/A
```

**Step 3: Write minimal implementation**

```swift
// N/A
```

**Step 4: Run test to verify it passes**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test`
Expected: PASS。

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -configuration Debug -sdk macosx build`
Expected: BUILD SUCCEEDED。

**Step 5: Commit**

```bash
git add Tomato project.yml TomatoTests
git commit -m "chore: verify glass UI redesign with tests and build"
```
