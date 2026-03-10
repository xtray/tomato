# Timer Completion Chimes Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add distinct built-in completion chimes for work-end and break-end transitions, plus settings for enable/disable and volume, on macOS and Windows.

**Architecture:** Keep timer transition rules where they already live, and inject or isolate audio playback behind small helpers. Persist shared user-facing settings separately on each platform, but keep the behavioral split the same: work-complete chime on transition into break, break-complete chime on transition back to ready work state.

**Tech Stack:** SwiftUI, Swift/AVFoundation, XCTest, WinForms, C#/.NET, xUnit

---

### Task 1: Add macOS failing tests for chime settings and trigger events

**Files:**
- Modify: `TomatoTests/TaskStoreThemeTests.swift`
- Modify: `Tomato/ViewModels/TaskStore.swift`

**Step 1: Write the failing test**

Add tests for:
- default chime settings are enabled with default volume
- reset settings restores default chime settings
- work completion triggers a work-complete chime event
- break completion triggers a break-complete chime event

**Step 2: Run test to verify it fails**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/TaskStoreThemeTests test`
Expected: FAIL because the new settings and audio seam do not exist yet.

**Step 3: Write minimal implementation**

Add persisted chime settings and a simple injectable playback seam to `TaskStore`.

**Step 4: Run test to verify it passes**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/TaskStoreThemeTests test`
Expected: PASS

### Task 2: Implement macOS audio playback and settings UI

**Files:**
- Modify: `Tomato/Views/SettingsView.swift`
- Modify: `Tomato/Localization/AppLocalization.swift`
- Create: `Tomato/Audio/CompletionChimePlayer.swift`
- Modify: `Tomato/ViewModels/TaskStore.swift`

**Step 1: Write the failing test**

Reuse Task 1 tests as the behavior contract and add any localization assertions only if needed.

**Step 2: Run test to verify contract is still red/green**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/TaskStoreThemeTests test`
Expected: PASS after Task 1, then keep it green through playback wiring and settings UI work.

**Step 3: Write minimal implementation**

Implement a macOS chime player that generates built-in PCM/WAV data for two note sequences and plays them through `AVAudioPlayer`. Wire settings controls into `SettingsView`.

**Step 4: Run test to verify it passes**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/TaskStoreThemeTests test`
Expected: PASS

### Task 3: Add Windows failing tests for chime settings and transition triggers

**Files:**
- Modify: `Tomato.WindowsCore.Tests/WindowsAppStateStoreTests.cs`
- Create: `Tomato.WindowsCore.Tests/WindowsTimerCompletionAudioTests.cs`
- Create or Modify: `Tomato.WindowsCore/WindowsAppStateStore.cs`

**Step 1: Write the failing test**

Add tests for:
- default Windows chime settings
- normalization of chime volume
- work-to-break transition resolves to work-complete chime
- break-to-ready transition resolves to break-complete chime

**Step 2: Run test to verify it fails**

Run: `dotnet test Tomato.WindowsCore.Tests/Tomato.WindowsCore.Tests.csproj`
Expected: FAIL because the new settings and trigger helper do not exist yet.

**Step 3: Write minimal implementation**

Add the persisted settings and a small helper that maps snapshot transitions to an audio event.

**Step 4: Run test to verify it passes**

Run: `dotnet test Tomato.WindowsCore.Tests/Tomato.WindowsCore.Tests.csproj`
Expected: PASS

### Task 4: Implement Windows audio playback and settings UI

**Files:**
- Modify: `Tomato.WindowsGui/Program.cs`
- Modify: `Tomato.WindowsGui/Tomato.WindowsGui.csproj`
- Create: `Tomato.WindowsCore/WindowsCompletionChime.cs`
- Create or Modify: `Tomato.WindowsCore/WindowsTimerCompletionAudioEvent.cs`
- Modify: `Tomato.WindowsCore/WindowsUiText.cs`

**Step 1: Write the failing test**

Reuse Task 3 tests as the contract and add focused tests for generated WAV bytes if needed.

**Step 2: Run test to verify contract is still red/green**

Run: `dotnet test Tomato.WindowsCore.Tests/Tomato.WindowsCore.Tests.csproj`
Expected: PASS after Task 3, then keep it green through playback and UI wiring.

**Step 3: Write minimal implementation**

Add runtime WAV generation and asynchronous playback, wire trigger calls from `OnTick`, and extend the Windows settings dialog with chime controls.

**Step 4: Run test to verify it passes**

Run: `dotnet test Tomato.WindowsCore.Tests/Tomato.WindowsCore.Tests.csproj`
Expected: PASS

### Task 5: Verify both platforms and rebuild the Windows release package

**Files:**
- Modify: `Tomato/ViewModels/TaskStore.swift`
- Modify: `Tomato/Views/SettingsView.swift`
- Modify: `Tomato/Localization/AppLocalization.swift`
- Create: `Tomato/Audio/CompletionChimePlayer.swift`
- Modify: `TomatoTests/TaskStoreThemeTests.swift`
- Modify: `Tomato.WindowsCore/WindowsAppStateStore.cs`
- Create: `Tomato.WindowsCore/WindowsCompletionChime.cs`
- Create or Modify: `Tomato.WindowsCore/WindowsTimerCompletionAudioEvent.cs`
- Modify: `Tomato.WindowsCore/WindowsUiText.cs`
- Modify: `Tomato.WindowsGui/Program.cs`
- Modify: `Tomato.WindowsGui/Tomato.WindowsGui.csproj`
- Create: `Tomato.WindowsCore.Tests/WindowsTimerCompletionAudioTests.cs`
- Modify: `Tomato.WindowsCore.Tests/WindowsAppStateStoreTests.cs`

**Step 1: Run macOS tests**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/TaskStoreThemeTests test`
Expected: PASS

**Step 2: Run Windows tests**

Run: `dotnet test Tomato.WindowsCore.Tests/Tomato.WindowsCore.Tests.csproj`
Expected: PASS

**Step 3: Rebuild Windows release executable**

Run:

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

Expected: PASS and output in `build/windows-release`
