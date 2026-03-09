# Task Double-Click Focus Design

**Problem**

Users want to double-click a task in the task list and jump directly into the floating focus countdown window. This behavior must work on both macOS and Windows.

**Approved Behavior**

- Double-click on a task row starts focus for that task and opens the floating countdown window.
- This only happens when there is no active session and no resumable paused session.
- If there is a running session or a resumable paused session, double-click only keeps normal selection behavior and must not interrupt or replace the existing session.

**macOS Design**

- Add a double-click gesture on task rows in `Tomato/Views/ContentView.swift`.
- Keep single-click selection unchanged.
- Add a TaskStore entry point that accepts a specific task and starts focus only when session state is idle and non-resumable.
- Keep the floating window behavior unchanged by reusing existing `showingFloatingWindow` state transitions.

**Windows Design**

- Add a task-list `MouseDoubleClick` handler in `Tomato.WindowsGui/Program.cs`.
- Resolve the clicked row from the pointer location so empty-area double-clicks do nothing.
- Add a helper that receives the clicked task/index and starts focus only when there is no active or resumable session.
- Reuse the existing floating-window creation and refresh path.

**Session Gate Rule**

- Allowed: engine idle, no `_sessionTaskId` / `sessionTaskID`.
- Blocked: currently running focus or break session.
- Blocked: paused but resumable session with retained session task.

**Testing**

- macOS tests in `TomatoTests/TaskStoreThemeTests.swift` cover the new TaskStore entry point:
  - starts focus for a provided task when idle
  - does not start or replace session when a paused resumable session exists
  - does not replace session when a session is running
- Windows tests focus on extracted GUI helper logic in a new or existing Windows test file:
  - idle state allows direct-start for clicked task
  - resumable session blocks direct-start
  - running session blocks direct-start

**Why This Design**

- It keeps business rules out of view code.
- It avoids changing existing main Focus button semantics.
- It minimizes platform-specific changes while making the new behavior explicit and testable.
