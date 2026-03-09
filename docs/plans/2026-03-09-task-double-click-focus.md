# Task Double-Click Focus Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add task-row double-click to start focus and open the floating countdown window on macOS and Windows, but only when no active or resumable session exists.

**Architecture:** Keep session gating in state/controller code instead of view event handlers. macOS gets a dedicated TaskStore API; Windows gets a dedicated MainForm helper for clicked tasks. Both reuse existing floating-window show logic.

**Tech Stack:** SwiftUI, Swift/XCTest, WinForms, C#/.NET, xUnit

---

### Task 1: Add macOS failing tests

**Files:**
- Modify: `TomatoTests/TaskStoreThemeTests.swift`
- Modify: `Tomato/ViewModels/TaskStore.swift`

**Step 1: Write the failing test**

Add tests for:
- idle store starts focus for a specific task and shows floating window
- paused resumable session blocks direct-start for another task
- running session blocks direct-start for another task

**Step 2: Run test to verify it fails**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/TaskStoreThemeTests test`
Expected: FAIL because the direct-start API does not exist yet.

**Step 3: Write minimal implementation**

Add a TaskStore method that accepts a task and starts focus only if no active or resumable session exists.

**Step 4: Run test to verify it passes**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/TaskStoreThemeTests test`
Expected: PASS

### Task 2: Wire macOS double-click gesture

**Files:**
- Modify: `Tomato/Views/ContentView.swift`
- Modify: `Tomato/ViewModels/TaskStore.swift`

**Step 1: Write the failing test**

Use existing TaskStore tests as the behavior contract. No UI automation is required if the view only calls the new TaskStore API.

**Step 2: Run test to verify contract is still red/green**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/TaskStoreThemeTests test`
Expected: PASS after Task 1, then keep it green through the view wiring.

**Step 3: Write minimal implementation**

Add `.onTapGesture(count: 2)` to task rows and call the new TaskStore API.

**Step 4: Run test to verify it passes**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/TaskStoreThemeTests test`
Expected: PASS

### Task 3: Add Windows failing tests and helper seam

**Files:**
- Modify: `Tomato.WindowsGui/Program.cs`
- Create or Modify: `Tomato.WindowsCore.Tests/TaskDoubleClickFocusTests.cs`

**Step 1: Write the failing test**

Add tests for extracted helper logic:
- idle state allows direct-start for clicked task
- paused resumable session blocks direct-start
- running session blocks direct-start

**Step 2: Run test to verify it fails**

Run: `dotnet test Tomato.WindowsCore.Tests/Tomato.WindowsCore.Tests.csproj`
Expected: FAIL because helper/API does not exist yet.

**Step 3: Write minimal implementation**

Extract a small pure helper or internal method for the direct-start gate and use it from `MainForm`.

**Step 4: Run test to verify it passes**

Run: `dotnet test Tomato.WindowsCore.Tests/Tomato.WindowsCore.Tests.csproj`
Expected: PASS

### Task 4: Wire Windows double-click handler

**Files:**
- Modify: `Tomato.WindowsGui/Program.cs`

**Step 1: Write the failing test**

Reuse Task 3 helper tests as the contract; no full GUI automation is required if the event handler delegates to the tested helper.

**Step 2: Run test to verify contract is still red/green**

Run: `dotnet test Tomato.WindowsCore.Tests/Tomato.WindowsCore.Tests.csproj`
Expected: PASS after Task 3, then keep it green through the UI wiring.

**Step 3: Write minimal implementation**

Add `MouseDoubleClick`, resolve the clicked task row, and invoke the new direct-start helper.

**Step 4: Run test to verify it passes**

Run: `dotnet test Tomato.WindowsCore.Tests/Tomato.WindowsCore.Tests.csproj`
Expected: PASS

### Task 5: Verify both platforms and rebuild Windows release

**Files:**
- Modify: `Tomato/Views/ContentView.swift`
- Modify: `Tomato/ViewModels/TaskStore.swift`
- Modify: `TomatoTests/TaskStoreThemeTests.swift`
- Modify: `Tomato.WindowsGui/Program.cs`
- Create or Modify: `Tomato.WindowsCore.Tests/TaskDoubleClickFocusTests.cs`

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
