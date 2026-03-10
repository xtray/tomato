# macOS Main Window Arrow Cursor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make the macOS main window keep the normal arrow cursor at corners and edges while remaining fixed-size.

**Architecture:** Extend the existing main-window hosting view so it owns cursor-rect registration for the main window. Preserve the fixed-size window guard and only change cursor presentation on the main window path.

**Tech Stack:** SwiftUI, AppKit, XCTest

---

### Task 1: Add a failing cursor regression test

**Files:**
- Modify: `TomatoTests/ThemeSemanticsTests.swift`

**Step 1: Write the failing test**

Add a test that instantiates the main-window hosting view and asserts it exposes `NSCursor.arrow` for the main window cursor override path.

**Step 2: Run test to verify it fails**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/ThemeSemanticsTests test`
Expected: FAIL because the hosting view does not yet expose the arrow-cursor override helper.

### Task 2: Implement the minimal cursor override

**Files:**
- Modify: `Tomato/TomatoApp.swift`

**Step 1: Add the smallest implementation**

Update the main-window hosting view to rebuild cursor rects with `NSCursor.arrow` and expose the minimal helper needed by the new test.

**Step 2: Run targeted tests**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/ThemeSemanticsTests test`
Expected: PASS

### Task 3: Verify no macOS regressions

**Files:**
- Modify: none

**Step 1: Run the full macOS suite**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato test`
Expected: PASS with zero failures
