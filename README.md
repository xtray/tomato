# Tomato (macOS 番茄钟)

一个基于 SwiftUI 的 macOS 番茄钟应用，支持任务管理、悬浮倒计时窗口和任务番茄数统计。

## 功能概览

1. 主界面可以新增/删除/排序任务，选择任务后开始番茄钟。
2. 开始计时后会自动隐藏主窗口，并在桌面右上角显示悬浮番茄钟；点击悬浮窗口中的返回按钮可回到主界面。
3. 每完成一个专注番茄钟（Work Session），会累计到对应任务的 `completedPomodoros`，并显示在任务下方。

## 运行环境

- macOS 13.0+
- Xcode 15.0+（`project.yml` 中指定）
- Swift 5.9

## 安装与编译

### 方式一：Xcode（推荐）

1. 打开工程：
```bash
open Tomato.xcodeproj
```
2. 选择 `Tomato` Scheme，目标设备选择 `My Mac`。
3. 点击 Run（或按 `Cmd + R`）启动应用。

### 方式二：命令行编译

```bash
xcodebuild -project Tomato.xcodeproj -scheme Tomato -configuration Debug -derivedDataPath ./build/DerivedData build
```

编译产物路径：

`build/DerivedData/Build/Products/Debug/Tomato.app`

可直接运行：

```bash
open build/DerivedData/Build/Products/Debug/Tomato.app
```

## GitHub Tag 自动发布（macOS + Windows）

推送 `v*` 格式的 tag（例如 `v1.2.0`）后，GitHub Actions 会自动触发发布流程：

1. 在 macOS runner 编译并上传 `Tomato-<tag>-macOS.zip`
2. 在 Windows runner 编译并上传 `Tomato-<tag>-windows-x64.exe`
3. 自动创建（或更新）对应 tag 的 GitHub Release，并附带以上产物

工作流文件：`.github/workflows/release-macos.yml`（同时负责 macOS + Windows）。
也可以在 GitHub Actions 页面手动触发同一流程（`workflow_dispatch`）。

示例命令：

```bash
git tag v1.2.0
git push origin v1.2.0
```

Windows 可执行文件来自 `Tomato.WindowsGui`（WinForms 窗口版本），下载后可直接双击运行。

如果你希望在 macOS 本地手动编译 Windows `exe`，先安装 .NET SDK：

```bash
brew install --cask dotnet-sdk
```

然后执行：

```bash
dotnet publish Tomato.WindowsGui/Tomato.WindowsGui.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:DebugType=None \
  /p:DebugSymbols=false
```

默认产物：

`Tomato.WindowsGui/bin/Release/net10.0-windows/win-x64/publish/Tomato.WindowsGui.exe`

## 使用文档

### 1. 创建并选择任务

1. 在左侧任务列表底部输入框填写任务名。
2. 回车或点击 `+` 按钮创建任务。
3. 点击任务行选中该任务。

### 2. 开始番茄钟

1. 选中任务后，点击右侧 `Focus` 按钮开始专注。
2. 应用会自动切换为悬浮番茄钟（位于当前屏幕右上角）。
3. 悬浮窗口支持：
   - `播放/暂停`
   - `重置`
   - `返回主窗口`

### 3. 番茄钟阶段规则

- 专注时长：默认 25 分钟
- 短休息：默认 5 分钟
- 长休息：默认 15 分钟
- 每完成 4 个专注番茄钟，进入一次长休息；其余为短休息。

### 4. 任务统计展示

- 每完成 1 次专注番茄钟，对应任务 `completedPomodoros +1`。
- 统计会在任务行下方展示（如 `3X`），并配合番茄图标显示。

### 5. 参数设置

点击左上角齿轮 `Settings` 可调整：

- Focus Duration（1-60 分钟）
- Short Break（1-30 分钟）
- Long Break（1-60 分钟）

## 数据存储说明

应用使用 `UserDefaults` 本地保存：

- 任务列表（含完成状态、番茄统计）
- 番茄钟时长配置
