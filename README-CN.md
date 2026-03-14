# Tomato

[English](README.md) | 简体中文

Tomato 是一个桌面端番茄钟应用，集成了任务管理、悬浮倒计时窗口和本地专注统计。目前项目已经同时支持 macOS 和 Windows。

## 平台支持

- macOS 版本基于 SwiftUI
- Windows 版本基于 WinForms，并复用共享的 .NET 计时核心

## 当前已实现功能

- 任务列表管理：支持新增、删除、选择和拖拽排序
- 任务完成状态：支持标记任务为已完成或未完成
- 按任务统计已完成番茄钟数量
- 选择任务后可一键开始专注
- 在没有冲突中的活动会话时，可双击任务直接开始专注
- 自动切换番茄钟阶段：
  - 专注阶段默认 25 分钟
  - 短休息默认 5 分钟
  - 长休息默认 15 分钟
  - 每完成 4 次专注后进入一次长休息
- 专注开始后可显示悬浮倒计时窗口
- 支持暂停、继续、重置，以及返回主窗口
- 设置中可调整专注、短休息、长休息时长
- 支持调整悬浮窗透明度
- 支持完成提示音开关与音量调节
- 内置多套主题样式，可快速切换
- 支持中文和英文界面
- 本地持久化保存任务、计时设置、界面偏好和专注进度

## 平台说明

### macOS

- 开始专注后会自动隐藏主窗口，并在屏幕右上区域显示悬浮番茄钟
- 悬浮窗口支持播放或暂停、重置、返回主窗口
- 悬浮窗口支持通过缩放热区调整大小
- 数据通过 `UserDefaults` 本地保存

### Windows

- Windows 版本可构建为独立的 `win-x64` 可执行文件
- 悬浮窗口的透明度和尺寸会被持久化保存
- 共享计时逻辑由 `Tomato.WindowsCore.Tests` 中的 .NET 测试覆盖
- 数据通过 Windows 端本地状态存储写入用户目录

## 构建要求

### macOS

- macOS 13.0+
- Xcode 15.0+
- Swift 5.9

### Windows

- 本地构建需安装 .NET SDK

## 编译与运行

### 使用 Xcode 运行 macOS 版本

```bash
open Tomato.xcodeproj
```

选择 `Tomato` Scheme，目标选择 `My Mac`，然后运行应用。

### 命令行编译 macOS 版本

```bash
xcodebuild -project Tomato.xcodeproj -scheme Tomato -configuration Release -derivedDataPath ./build/release build
```

产物路径：

`build/release/Build/Products/Release/Tomato.app`

### 编译 Windows 发布版可执行文件

请使用下面这条固定命令：

```bash
dotnet publish Tomato.WindowsGui/Tomato.WindowsGui.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -o build/windows-release \
  /p:PublishSingleFile=true \
  /p:EnableCompressionInSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:IncludeAllContentForSelfExtract=true \
  /p:DebugType=None \
  /p:DebugSymbols=false
```

产物路径：

`build/windows-release/Tomato.WindowsGui.exe`

## 发布流程

推送 `v*` 格式的 tag，例如 `v1.2.0`，会自动触发 GitHub Actions 发布流程。

该流程会：

- 构建 macOS 发布压缩包
- 构建 Windows 独立可执行文件
- 将两个产物一并上传到对应 tag 的 GitHub Release

工作流文件：

`.github/workflows/release-macos.yml`

示例：

```bash
git tag v1.2.0
git push origin v1.2.0
```

## 数据存储

Tomato 当前会在本地保存以下数据：

- 任务列表
- 任务完成状态
- 已完成番茄钟数量
- 专注与休息时长设置
- 语言设置
- 主题设置
- 悬浮窗偏好设置
- 完成提示音偏好设置
