# macOS Main Window Frame Cursor Shield Design

## Goal

Keep the macOS main window fixed at `760x500` and show the normal arrow cursor when hovering the frame edges or corners, without affecting content-area cursors.

## Root Cause

The previous cursor fix targeted the SwiftUI hosting view. That only affects the content area. The corner resize cursor still comes from the outer `NSThemeFrame` window frame, so the content-level cursor rect override cannot suppress it.

## Selected Approach

Install transparent cursor-shield views on the main window frame view (`contentView.superview`), not on the content hosting view.

The shields will:
- cover only the thin edge and corner regions that currently show resize cursors
- register `NSCursor.arrow`
- return `nil` from hit-testing so dragging, clicking, and content interaction still pass through

## Why This Approach

- It targets the actual source of the resize cursor
- It avoids overriding text or control cursors inside the content area
- It keeps the existing fixed-size window behavior unchanged

## Testing

- Add a failing test for frame shield rect generation and installation
- Add a failing test proving the content hosting view no longer owns the arrow-cursor override
- Run targeted semantic tests, then the full macOS suite
