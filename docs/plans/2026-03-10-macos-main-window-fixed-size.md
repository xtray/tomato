# macOS Main Window Fixed Size Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Keep the macOS main window fixed at `760x500` and prevent user resizing so the existing layout never obscures text.

**Architecture:** Add a small AppKit-backed window configuration helper in the macOS app layer, then attach it to the main SwiftUI window content. Keep the view layout unchanged and validate the behavior with regression tests.

**Tech Stack:** SwiftUI, AppKit, XCTest

---

### Task 1: Add failing regression tests

**Files:**
- Modify: `TomatoTests/ThemeSemanticsTests.swift`

**Step 1: Write the failing tests**

Add tests for:
- a helper that removes the resizable style and locks an `NSWindow` to `760x500`
- app wiring that applies the helper to the main window

**Step 2: Run test to verify it fails**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/ThemeSemanticsTests test`
Expected: FAIL because the helper and wiring do not exist yet.

### Task 2: Add minimal fixed-size window configuration

**Files:**
- Modify: `Tomato/TomatoApp.swift`

**Step 1: Write minimal implementation**

Add:
- a shared fixed-size constant for the main window (`760x500`)
- a helper that applies fixed AppKit window sizing and removes `.resizable`
- a small representable/container that applies the helper to the main window

**Step 2: Run tests to verify they pass**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/ThemeSemanticsTests test`
Expected: PASS

### Task 3: Full verification

**Files:**
- Modify: `Tomato/TomatoApp.swift`
- Modify: `TomatoTests/ThemeSemanticsTests.swift`

**Step 1: Run full macOS test suite**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato test`
Expected: PASS
