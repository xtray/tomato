# Timer Ring Countdown Direction Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将主界面与悬浮框计时环改为“12 点起顺时针变空白”的倒计时视觉语义。

**Architecture:** 在 `TimerProgressCalculator` 新增 `elapsedProgress`，保持 `remaining` 计算逻辑不变；`ContentView` 与 `FloatingTimerContentView` 改用同一已过比例来驱动 `trim(from:to:)`。仅改表现层，不改定时业务状态机。

**Tech Stack:** SwiftUI, CoreGraphics, XCTest, xcodebuild

---

### Task 1: 先写失败测试覆盖已过比例语义

**Files:**
- Modify: `TomatoTests/TimerProgressCalculatorTests.swift`

**Step 1: Write the failing test**

```swift
func test_elapsed_progress_returns_complement_of_remaining_progress() {
    XCTAssertEqual(TimerProgressCalculator.elapsedProgress(remaining: 1500, total: 1500), 0)
    XCTAssertEqual(TimerProgressCalculator.elapsedProgress(remaining: 750, total: 1500), 0.5)
    XCTAssertEqual(TimerProgressCalculator.elapsedProgress(remaining: 0, total: 1500), 1)
}
```

**Step 2: Run test to verify it fails**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/TimerProgressCalculatorTests`
Expected: FAIL，`elapsedProgress` 尚不存在。

**Step 3: Write minimal implementation**

```swift
static func elapsedProgress(remaining: Int, total: Int) -> CGFloat {
    1 - progress(remaining: remaining, total: total)
}
```

**Step 4: Run test to verify it passes**

Run: same as Step 2
Expected: PASS。

### Task 2: 主界面改用已过比例绘制环形圈

**Files:**
- Modify: `Tomato/Views/ContentView.swift`

**Step 1: Write the failing test**

```swift
// 视图层渲染行为，无单元测试钩子；由 Task 1 的计算测试 + 手工验证覆盖
```

**Step 2: Run test to verify it fails**

```bash
# N/A
```

**Step 3: Write minimal implementation**

```swift
Circle().trim(from: timerElapsedProgress, to: 1)
```

并新增计算属性：

```swift
var timerElapsedProgress: CGFloat {
    TimerProgressCalculator.elapsedProgress(remaining: taskStore.remainingSeconds, total: currentPhaseDuration)
}
```

**Step 4: Run test to verify it passes**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/TimerProgressCalculatorTests`
Expected: PASS。

### Task 3: 悬浮框改用同一已过比例逻辑并回归验证

**Files:**
- Modify: `Tomato/Views/FloatingWindowManager.swift`
- Verify: `TomatoTests/TimerProgressCalculatorTests.swift`

**Step 1: Write the failing test**

```swift
// 同 Task 2，无额外渲染单元测试钩子
```

**Step 2: Run test to verify it fails**

```bash
# N/A
```

**Step 3: Write minimal implementation**

```swift
Circle().trim(from: timerElapsedProgress, to: 1)
```

并新增：

```swift
var timerElapsedProgress: CGFloat {
    TimerProgressCalculator.elapsedProgress(remaining: taskStore.remainingSeconds, total: currentPhaseDuration)
}
```

**Step 4: Run test to verify it passes**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/TimerProgressCalculatorTests`
Expected: PASS。

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -configuration Debug -sdk macosx build`
Expected: BUILD SUCCEEDED。
