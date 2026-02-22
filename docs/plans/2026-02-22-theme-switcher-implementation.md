# Theme Switcher (Business Motion) Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在现有 Glass UI 基础上新增“Business Motion”主题，并提供主界面与设置页双入口切换且可持久化。

**Architecture:** 使用 `ThemeMode` 作为主题枚举，并将当前主题状态集中在 `TaskStore.themeMode`。`AppTheme` 和通用组件按 `ThemeMode` 提供 token 与动效参数，三个窗口共享同一主题状态。所有改动保持业务逻辑不变，仅调整视觉层与交互层。

**Tech Stack:** SwiftUI, AppKit, XCTest, UserDefaults, xcodebuild, XcodeGen

---

### Task 1: 主题枚举与主题偏好测试先行

**Files:**
- Modify: `TomatoTests/ThemeSemanticsTests.swift`
- Create: `TomatoTests/ThemePreferencesTests.swift`

**Step 1: Write the failing test**

```swift
func test_theme_mode_has_two_options() {
    XCTAssertEqual(ThemeMode.allCases.count, 2)
}

func test_theme_preferences_falls_back_to_default_for_invalid_raw_value() {
    let defaults = UserDefaults(suiteName: "ThemePrefsTests")!
    defaults.set("invalid", forKey: "themeMode")
    XCTAssertEqual(ThemePreferences.load(from: defaults, key: "themeMode"), .glassVivid)
}
```

**Step 2: Run test to verify it fails**

Run: `xcodegen generate && xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/ThemeSemanticsTests -only-testing:TomatoTests/ThemePreferencesTests`
Expected: FAIL，`ThemeMode` / `ThemePreferences` 未定义。

**Step 3: Write minimal implementation**

```swift
enum ThemeMode: String, CaseIterable, Codable { case glassVivid, businessMotion }
enum ThemePreferences {
  static func load(from defaults: UserDefaults = .standard, key: String = "themeMode") -> ThemeMode
  static func save(_ mode: ThemeMode, to defaults: UserDefaults = .standard, key: String = "themeMode")
}
```

**Step 4: Run test to verify it passes**

Run: same as Step 2
Expected: PASS。

**Step 5: Commit**

```bash
git add TomatoTests/ThemeSemanticsTests.swift TomatoTests/ThemePreferencesTests.swift Tomato/Theme/AppTheme.swift
git commit -m "test: add theme mode and preferences semantics tests"
```

### Task 2: TaskStore 主题状态与持久化接入

**Files:**
- Modify: `Tomato/ViewModels/TaskStore.swift`
- Test: `TomatoTests/ThemePreferencesTests.swift`

**Step 1: Write the failing test**

```swift
func test_theme_preferences_round_trip() {
    let defaults = UserDefaults(suiteName: "ThemePrefsRoundTrip")!
    ThemePreferences.save(.businessMotion, to: defaults, key: "themeMode")
    XCTAssertEqual(ThemePreferences.load(from: defaults, key: "themeMode"), .businessMotion)
}
```

**Step 2: Run test to verify it fails**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/ThemePreferencesTests`
Expected: FAIL（round-trip 未实现或未接线）。

**Step 3: Write minimal implementation**

```swift
@Published var themeMode: ThemeMode {
  didSet { ThemePreferences.save(themeMode) }
}

init() {
  self.themeMode = ThemePreferences.load()
  // existing init behavior...
}
```

**Step 4: Run test to verify it passes**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/ThemePreferencesTests`
Expected: PASS。

**Step 5: Commit**

```bash
git add Tomato/ViewModels/TaskStore.swift TomatoTests/ThemePreferencesTests.swift
git commit -m "feat: persist selected theme mode in task store"
```

### Task 3: AppTheme 与组件支持双主题

**Files:**
- Modify: `Tomato/Theme/AppTheme.swift`
- Modify: `Tomato/Theme/GlassComponents.swift`
- Test: `TomatoTests/ThemeSemanticsTests.swift`

**Step 1: Write the failing test**

```swift
func test_business_theme_background_differs_from_glass_vivid() {
    let vivid = AppTheme.Backgrounds.mainGradient(for: .glassVivid)
    let business = AppTheme.Backgrounds.mainGradient(for: .businessMotion)
    XCTAssertNotEqual(vivid.stops.first?.color, business.stops.first?.color)
}
```

**Step 2: Run test to verify it fails**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/ThemeSemanticsTests`
Expected: FAIL，`for mode` API 不存在。

**Step 3: Write minimal implementation**

```swift
enum AppTheme {
  enum Backgrounds {
    static func mainGradient(for mode: ThemeMode) -> Gradient
  }
  static func cardShadow(for mode: ThemeMode) -> (Color, CGFloat, CGFloat)
}

extension TimerPhase {
  func themedColor(for mode: ThemeMode) -> Color
}
```

并让 `GlassCard`、按钮样式、背景组件支持 `mode`。

**Step 4: Run test to verify it passes**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/ThemeSemanticsTests`
Expected: PASS。

**Step 5: Commit**

```bash
git add Tomato/Theme/AppTheme.swift Tomato/Theme/GlassComponents.swift TomatoTests/ThemeSemanticsTests.swift
git commit -m "feat: support vivid and business visual tokens"
```

### Task 4: 主窗口与设置页双入口切换

**Files:**
- Modify: `Tomato/Views/ContentView.swift`
- Modify: `Tomato/Views/SettingsView.swift`

**Step 1: Write the failing test**

```swift
func test_theme_mode_display_names_are_stable() {
    XCTAssertEqual(ThemeMode.glassVivid.displayName, "Glass Vivid")
    XCTAssertEqual(ThemeMode.businessMotion.displayName, "Business Motion")
}
```

**Step 2: Run test to verify it fails**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/ThemeSemanticsTests`
Expected: FAIL，`displayName` 未实现。

**Step 3: Write minimal implementation**

```swift
extension ThemeMode {
  var displayName: String { ... }
}
```

并实现：
- `ContentView` 顶部快捷切换按钮
- `SettingsView` 主题 `Picker`
- 两入口都写入 `taskStore.themeMode`

**Step 4: Run test to verify it passes**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test -only-testing:TomatoTests/ThemeSemanticsTests`
Expected: PASS。

**Step 5: Commit**

```bash
git add Tomato/Views/ContentView.swift Tomato/Views/SettingsView.swift Tomato/Theme/AppTheme.swift TomatoTests/ThemeSemanticsTests.swift
git commit -m "feat: add dual-entry theme switch controls"
```

### Task 5: 悬浮窗同步主题与全量验证

**Files:**
- Modify: `Tomato/Views/FloatingWindowManager.swift`
- Verify: `Tomato/Theme/AppTheme.swift`
- Verify: `Tomato/Theme/GlassComponents.swift`
- Verify: `Tomato/ViewModels/TaskStore.swift`

**Step 1: Write the failing test**

```swift
// No new domain behavior; reuse existing theme tests as regression gate
```

**Step 2: Run test to verify it fails**

```bash
# N/A
```

**Step 3: Write minimal implementation**

```swift
// pass taskStore.themeMode through floating window components
// keep timer actions unchanged
```

**Step 4: Run test to verify it passes**

Run: `xcodegen generate && xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test`
Expected: PASS。

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -configuration Debug -sdk macosx build`
Expected: BUILD SUCCEEDED。

**Step 5: Commit**

```bash
git add Tomato/Views/FloatingWindowManager.swift Tomato/Theme TomatoTests project.yml Tomato.xcodeproj/project.pbxproj
git commit -m "feat: ship business motion theme with dual-entry switch"
```
