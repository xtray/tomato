# macOS Main Window Fixed Size Design

**Goal:** Keep the macOS main window at the existing `760x500` size and prevent user resizing so the current layout never compresses or obscures text.

**Context**

The main window content in `ContentView` is already designed around a fixed `760x500` layout. Users can still resize the host window on macOS, which can make the layout inconsistent with that design target and create text overlap or clipping risk.

**Decision**

Use the current `760x500` size as the single fixed main-window size and disable resizing at the AppKit window level. Do not redesign the internal layout or make it responsive for smaller sizes.

**Approach**

1. Add a small macOS-only window configuration helper that:
   - removes the `.resizable` style mask
   - sets `minSize` and `maxSize` to `760x500`
   - forces the window content size back to `760x500`
2. Attach that helper to the main SwiftUI window content so the configuration is applied whenever the main window becomes available.
3. Add regression tests for:
   - the helper behavior on an `NSWindow`
   - the app wiring that applies the helper to the main window

**Why This Approach**

- It is the smallest change that matches the requested behavior exactly.
- It preserves the existing visual layout instead of introducing broader responsive-layout work.
- It gives us direct regression coverage for both the AppKit behavior and the SwiftUI wiring.

**Non-Goals**

- Redesigning the main window layout
- Making the main window responsive across multiple sizes
- Changing floating window sizing behavior
