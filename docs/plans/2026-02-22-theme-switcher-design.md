# Theme Switcher Design (Glass Vivid + Business Motion)

**Date:** 2026-02-22  
**Status:** Approved

## Goal
在现有 Glass 基础上新增第二套“更商务克制 + 更强动效”主题，并提供双入口切换（主界面快捷切换 + 设置页下拉切换），主题状态持久化。

## Confirmed Decisions
- 切换入口：主界面 + 设置页（双入口）
- 架构方案：`ThemeMode` + `TaskStore` 持有主题状态（推荐方案 1）
- 范围：主窗口、设置弹窗、悬浮计时窗统一生效

## Architecture
### Theme Mode
新增 `ThemeMode` 枚举：
- `glassVivid`：当前亮色玻璃风格
- `businessMotion`：低饱和商务风格，增强但克制的动效

### State Source
在 `TaskStore` 中新增：
- `@Published var themeMode: ThemeMode`
- 通过 `UserDefaults` 持久化主题
- 所有视图从同一状态源读取，避免状态分叉

### Theme Tokens
`AppTheme` 改为“按模式返回 token”设计：
- 颜色、背景、描边、阴影
- 交互与动效参数（时长、缓动）
- `TimerPhase` 颜色按模式映射

### Reusable Components
`GlassComponents` 接收 `themeMode`：
- `GlassCard(mode:)`
- `PrimaryGlassButtonStyle(mode:)`
- `SecondaryGlassButtonStyle(mode:)`
- `GlassBackground(mode:)`

## UI Changes
### Main Window
`ContentView` 增加快捷切换控件：
- 可在标题区域一键切换主题
- 切换后即时刷新整个界面样式

### Settings Window
`SettingsView` 增加主题 Picker：
- 明确显示当前主题名称
- 变更实时生效并持久化

### Floating Window
`FloatingTimerContentView` 同步读取 `themeMode`：
- 与主窗保持一致的视觉与动效策略

## Data Flow And Boundaries
- `TaskStore.themeMode` 是唯一主题状态源。
- 视图层不直接读写 `UserDefaults`。
- 仅表现层变更：不修改任务、计时、悬浮窗业务逻辑。

## Testing Strategy
### Automated
新增/调整测试：
- `ThemeMode` token 映射正确性（两主题关键 token 不同）
- 主题偏好持久化读取/回退逻辑
- 既有 `TimerProgressCalculator` 测试继续通过

### Manual Regression
- 主窗快捷切换可用，设置页切换可用
- 主窗/设置窗/悬浮窗切换后样式同步
- 重启后主题保持
- 任务与计时行为不回归

## Verification Commands
- `xcodebuild -project Tomato.xcodeproj -scheme Tomato -destination 'platform=macOS' test`
- `xcodebuild -project Tomato.xcodeproj -scheme Tomato -configuration Debug -sdk macosx build`

## Non-Goals
- 不引入第三方依赖
- 不重构 `TaskStore` 业务定时逻辑
- 不改任务模型与持久化协议（除主题偏好）
