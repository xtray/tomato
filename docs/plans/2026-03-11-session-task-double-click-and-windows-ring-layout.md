# Session Task Double Click And Windows Ring Layout Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Keep the Windows main-window timer ring centered within the right card regardless of long task titles, and make double-clicking the current paused session task reopen the floating window and resume timing on both Windows and macOS.

**Architecture:** On Windows, split the timer card into a title area and a dedicated centering host for the ring so the ring aligns to its own container instead of the text stack. For double-click behavior, extend the existing session-task resolver to distinguish paused-session resume from simple floating-window reopen, then wire both desktop implementations to reopen the floating window and resume only when the clicked task matches the current session task.

**Tech Stack:** WinForms, SwiftUI/AppKit view models, xUnit, XCTest, .NET 9

---

### Task 1: Windows timer card centering

**Files:**
- Modify: `Tomato.WindowsGui/Program.cs`

**Step 1: Write the failing test**

Add a source-level regression test that asserts `BuildTimerCard` uses a dedicated centering host for `_ringControl`, not a layout that can be shifted by `_taskTitleLabel`.

**Step 2: Run test to verify it fails**

Run: `dotnet test Tomato.WindowsCore.Tests/Tomato.WindowsCore.Tests.csproj --filter WindowsMainForm*`
Expected: FAIL because the current layout still docks the ring directly in a simple fill panel.

**Step 3: Write minimal implementation**

Refactor the WinForms timer card to use a separate fill container with explicit centered child layout for `_ringControl`, while keeping the text stack docked independently above it.

**Step 4: Run test to verify it passes**

Run: `dotnet test Tomato.WindowsCore.Tests/Tomato.WindowsCore.Tests.csproj --filter WindowsMainForm*`
Expected: PASS

### Task 2: Double-click paused current session task resumes focus

**Files:**
- Modify: `Tomato.WindowsCore/WindowsTaskDirectFocusGate.cs`
- Modify: `Tomato.WindowsCore.Tests/WindowsTaskDirectFocusGateTests.cs`
- Modify: `Tomato.WindowsCore.Tests/WindowsMainFormFocusRestoreTests.cs`
- Modify: `Tomato.WindowsGui/Program.cs`
- Modify: `Tomato/ViewModels/TaskStore.swift`
- Modify: `TomatoTests/TaskStoreThemeTests.swift`

**Step 1: Write the failing tests**

Add .NET tests for a new paused-session resume action and main-form source assertions, plus XCTest coverage proving double-clicking the current paused session task reopens the floating window and resumes the timer without replacing the session.

**Step 2: Run tests to verify they fail**

Run: `dotnet test Tomato.WindowsCore.Tests/Tomato.WindowsCore.Tests.csproj --filter WindowsTaskDirectFocusGateTests|WindowsMainFormFocusRestoreTests`
Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/TaskStoreThemeTests test`
Expected: FAIL until the new action is handled in both implementations.

**Step 3: Write minimal implementation**

Extend the Windows double-click action model with a paused-session resume path, update the Windows handler to reopen the floating window and start the timer, and update `TaskStore.startFocusSessionForDoubleClick(_:)` so the same-task paused path resumes and shows the floating window.

**Step 4: Run tests to verify they pass**

Run: `dotnet test Tomato.WindowsCore.Tests/Tomato.WindowsCore.Tests.csproj --filter WindowsTaskDirectFocusGateTests|WindowsMainFormFocusRestoreTests`
Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/TaskStoreThemeTests test`
Expected: PASS

### Task 3: Full verification and Windows publish

**Files:**
- Verify only

**Step 1: Run targeted verification**

Run the Windows core tests and the macOS focused tests again after integrating both tasks.

**Step 2: Rebuild Windows release output**

Run the required publish command:

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

**Step 3: Request code review**

Run a code review pass on the final diff before reporting completion.
