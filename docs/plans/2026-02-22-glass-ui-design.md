# Glass UI Design (Tomato)

**Date:** 2026-02-22  
**Status:** Approved

## Goal
将当前 Tomato macOS 客户端整体视觉升级为现代精美的 `Glass` 风格，覆盖主窗口、设置弹窗与悬浮计时窗，同时保持现有任务与计时行为不变。

## Visual Direction
- 风格：Glass（半透明磨砂、柔和渐变、卡片层级）
- 主色：番茄红（辅以橙/青作为状态点缀）
- 范围：主窗口 + 设置弹窗 + 悬浮计时窗（全部）

## Architecture And Components
### Theme Layer
新增统一主题文件：`Tomato/Theme/AppTheme.swift`
- 色板：主色、辅助色、语义色（成功/警示/次级）
- 背景：窗口渐变、卡片材质参数
- 视觉常量：圆角、阴影、间距、描边

### Reusable UI Primitives
新增通用组件文件：`Tomato/Theme/GlassComponents.swift`
- `GlassCard`：玻璃卡片容器
- `PrimaryGlassButtonStyle` / `SecondaryGlassButtonStyle`
- 统一输入框与状态徽标样式

### View Refactor Scope
- `Tomato/Views/ContentView.swift`
  - 主背景改为渐变层
  - 左右区块卡片化，强化层级和留白
  - 计时区按钮与进度环视觉统一
- `Tomato/Views/SettingsView.swift`
  - 设置项改为卡片化行布局
  - 标题、间距、控件视觉统一
- `Tomato/Views/FloatingWindowManager.swift`
  - 悬浮窗内容改为统一 glass panel
  - 强化边框高光与阴影，保持紧凑可读

## Data Flow And State Boundaries
- 不新增业务状态，不改 `TaskStore` 核心逻辑。
- 继续沿用现有状态来源：
  - `ContentView` 使用 `@EnvironmentObject taskStore`
  - `SettingsView` / `FloatingTimerContentView` 使用同一 `taskStore`
- 新增动画仅限表现层，不改变计时节拍与状态切换逻辑。

## Compatibility And Behavior Guarantees
保持以下行为不变：
- 任务新增/删除/完成切换
- 任务选择、拖拽排序
- 开始/停止/重置计时
- 设置时长修改后的联动更新
- 悬浮窗显示/返回主窗流程
- 菜单命令与快捷键

## Error Handling Strategy
本次无新增 I/O、网络与持久化协议；风险集中在 UI 回归：
- 通过构建验证 + 关键路径手工回归控制风险
- 若出现异常，优先回退局部样式容器，不触碰业务层

## Testing And Verification
### Build Verification
- `xcodebuild -project Tomato.xcodeproj -scheme Tomato -configuration Debug -sdk macosx build`

### Manual Regression Checklist
- 空任务、已选任务、计时中三种主态渲染
- work/shortBreak/longBreak 三阶段颜色与文案
- 设置页三种时长修改后行为
- 悬浮窗打开、计时控制、Back 返回
- 删除任务确认与上下文菜单操作

## Non-Goals
- 不重写业务架构
- 不引入新依赖
- 不调整数据模型和持久化格式
