# macOS Main Window Frame Cursor Shield Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the ineffective content-level cursor override with frame-level cursor shields so the macOS main window corners keep the normal arrow cursor.

**Architecture:** Keep `MainWindowConfiguration` responsible for fixed-size window setup, and add a small frame-level installer that attaches transparent arrow-cursor shield views to the window frame view. Remove the content-wide cursor override from the hosting view.

**Tech Stack:** SwiftUI, AppKit, XCTest

---

### Task 1: Write failing tests for frame cursor shielding

**Files:**
- Modify: `TomatoTests/ThemeSemanticsTests.swift`

**Step 1: Add failing tests**

Add tests that require:
- a frame-level cursor shield installer/layout helper
- no content-wide `resetCursorRects` arrow override on `MainWindowConfigurationHostingView`

**Step 2: Verify red**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/ThemeSemanticsTests test`
Expected: FAIL

### Task 2: Implement frame cursor shields

**Files:**
- Modify: `Tomato/TomatoApp.swift`

**Step 1: Add minimal implementation**

Create the frame shield layout/installer and arrow-cursor shield view, install it from `MainWindowConfiguration.apply`, and remove the old content-wide cursor override.

**Step 2: Verify targeted tests**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato -only-testing:TomatoTests/ThemeSemanticsTests test`
Expected: PASS

### Task 3: Verify macOS regressions

**Files:**
- Modify: none

**Step 1: Run the full macOS suite**

Run: `xcodebuild -project Tomato.xcodeproj -scheme Tomato test`
Expected: PASS with zero failures
