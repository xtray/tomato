# Tomato

English | [简体中文](README-CN.md)

Tomato is a desktop Pomodoro timer with task management, floating countdown windows, and local progress tracking. The project currently supports both macOS and Windows.

## Platform Support

- macOS app built with SwiftUI
- Windows app built with WinForms and a shared .NET timer core

## Implemented Features

- Task list management: add, delete, select, and reorder tasks
- Task completion state: mark tasks as completed or incomplete
- Pomodoro tracking per task with completed session counts
- One-click focus start from the selected task
- Double-click a task to start focus directly when no conflicting active session exists
- Automatic Pomodoro cycle switching:
  - Focus session: 25 minutes by default
  - Short break: 5 minutes by default
  - Long break: 15 minutes by default
  - Long break after every 4 completed focus sessions
- Floating countdown window during active sessions
- Pause, resume, reset, and return-to-main-window controls
- Adjustable session durations in settings
- Adjustable floating window opacity
- Completion chimes with enable/disable and volume controls
- Theme switching with multiple built-in visual styles
- Built-in Chinese and English interface support
- Local persistence for tasks, timer settings, UI preferences, and progress data

## Platform Notes

### macOS

- The main window automatically hides after focus starts and the floating timer appears near the top-right of the screen
- The floating timer supports play/pause, reset, and return-to-main-window actions
- The floating window can be resized from its resize hotspot
- Data is stored locally with `UserDefaults`

### Windows

- The app ships as a standalone `win-x64` executable
- The floating timer supports opacity and size persistence
- Shared timer logic is covered by .NET test projects in `Tomato.WindowsCore.Tests`
- Data is stored locally through the Windows app state store in the user profile

## Build Requirements

### macOS

- macOS 13.0+
- Xcode 15.0+
- Swift 5.9

### Windows

- .NET SDK installed for local builds

## Build And Run

### Run on macOS with Xcode

```bash
open Tomato.xcodeproj
```

Choose the `Tomato` scheme, select `My Mac`, then run the app.

### Build macOS from the command line

```bash
xcodebuild -project Tomato.xcodeproj -scheme Tomato -configuration Release -derivedDataPath ./build/release build
```

Build output:

`build/release/Build/Products/Release/Tomato.app`

### Build Windows release executable

Use this exact command:

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

Build output:

`build/windows-release/Tomato.WindowsGui.exe`

## Release Workflow

Pushing a tag in the `v*` format, such as `v1.2.0`, triggers the GitHub Actions release workflow.

The workflow:

- Builds a macOS release archive
- Builds a Windows standalone executable
- Publishes both assets to the GitHub Release for that tag

Workflow file:

`.github/workflows/release-macos.yml`

Example:

```bash
git tag v1.2.0
git push origin v1.2.0
```

## Data Persistence

Tomato currently stores data locally, including:

- Task list
- Task completion state
- Completed Pomodoro counts
- Focus and break durations
- Selected language
- Selected theme
- Floating window preferences
- Completion chime preferences
