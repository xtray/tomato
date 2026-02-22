# Timer Ring Countdown Direction Design

**Date:** 2026-02-22  
**Status:** Approved

## Goal
让主界面与悬浮框中的时间环形圈按“剩余时间比例”实时倒计时，并从 12 点方向开始按顺时针方向逐步变为空白。

## Confirmed Decisions
- 空白增长方向：顺时针
- 起始位置：12 点方向
- 范围：主窗口 + 悬浮窗口两处环形圈统一行为

## Architecture
- 不改 `TaskStore` 的计时与阶段切换逻辑。
- 在 `TimerProgressCalculator` 增加“已过时间比例”计算接口，复用当前 clamp 规则。
- 视图层仅替换环形弧线的 `trim` 区间表达，保持颜色、阴影、动画节奏不变。

## Rendering Strategy
- 维持底层轨道圈不变。
- 原实现：`trim(from: 0, to: remainingRatio)`。
- 新实现：`trim(from: elapsedRatio, to: 1)`，其中 `elapsedRatio = 1 - remainingRatio`。
- 保持 `rotationEffect(.degrees(-90))`，确保起点固定在 12 点方向。

## Data Flow
- `remainingSeconds` 每秒变化。
- `currentPhaseDuration` 作为总时长。
- `TimerProgressCalculator` 输出已过比例给两个视图，两个视图使用相同计算口径。

## Error Handling And Boundaries
- `total <= 0` 时返回 0。
- `remaining < 0`、`remaining > total` 均被 clamp 到合法范围。
- 所有比例值保证在 `0...1`。

## Testing Strategy
- 在 `TimerProgressCalculatorTests` 新增已过比例测试：
  - 开始时为 0
  - 结束时为 1
  - 中间值正确
  - 异常输入被 clamp
- 回归验证：
  - 主窗口环形圈方向正确
  - 悬浮窗口环形圈方向正确
  - 数字时间与环形变化同步

## Non-Goals
- 不调整任务选择、计时开始/停止、番茄阶段切换逻辑
- 不改主题系统与颜色 token
