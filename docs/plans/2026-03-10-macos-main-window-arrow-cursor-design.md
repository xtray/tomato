# macOS Main Window Arrow Cursor Design

## Goal

Keep the macOS main window fixed at `760x500` and make the mouse stay as the normal arrow when hovering the main window corners or edges.

## Context

The previous fix removed actual resizing and attached a resize guard, but AppKit still exposes resize cursor hints around the main window frame. That leaves the UX looking resizable even though the window no longer changes size.

## Approaches

### Option A: Override main window cursor rects to arrow

Add a custom main-window hosting view that registers arrow cursor rects across its visible bounds and refreshes them when the view or window changes.

Pros:
- Smallest code change
- Targets cursor presentation directly
- Does not affect floating window behavior

Cons:
- Needs a regression test around the custom hosting view behavior

### Option B: Add a transparent overlay view around the frame area

Insert an overlay layer/view to absorb corner hover updates and force the arrow cursor.

Pros:
- Can isolate hover behavior from content

Cons:
- More moving pieces
- Higher risk of interfering with clicks and accessibility

## Selected Design

Use Option A. The main window hosting view will override cursor-rect management so the visible area always registers `NSCursor.arrow`. The fixed-size resize guard remains in place; this change only removes misleading resize cursor feedback.

## Testing

- Add a failing test proving the main window hosting view registers an arrow cursor rect.
- Keep the existing fixed-size and resize-guard tests.
- Run targeted semantic tests first, then the full macOS test suite.
