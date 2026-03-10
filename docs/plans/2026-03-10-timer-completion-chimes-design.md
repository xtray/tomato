# Timer Completion Chimes Design

**Problem**

Users want the app to play a short built-in chime when a work countdown finishes and a different built-in chime when a short or long break finishes. This must work in both the macOS app and the Windows app. Users also want a settings toggle and volume control for these chimes.

**Approved Behavior**

- Play one built-in completion chime when a work phase ends and the timer enters a short or long break.
- Play a different built-in completion chime when a short or long break ends and the timer returns to ready-to-focus state.
- Add a settings toggle to enable or disable completion chimes.
- Add a settings volume control that affects both chime types.
- Use fixed built-in sounds for now; no custom file picker is required.
- Each chime should be a short musical cue around 3 to 5 seconds.

**Shared Audio Design**

- Introduce a small cross-platform concept of `completion chime settings` with:
  - enabled flag
  - normalized volume value
  - event type enum for `workCompleted` and `breakCompleted`
- Keep the melodic definitions built in rather than shipping external audio files.
- Generate simple PCM audio data at runtime from note sequences so both platforms can share the same behavior contract while keeping platform playback implementation small.

**macOS Design**

- Extend `TaskStore` with persisted settings for chime enabled state and chime volume.
- Add a lightweight macOS audio service that converts a fixed note pattern into a short in-memory WAV/PCM clip and plays it with `AVAudioPlayer`.
- Trigger the work-complete chime inside `TaskStore.timerCompleted()` when work rolls into a break.
- Trigger the break-complete chime inside `TaskStore.timerCompleted()` when a break finishes and the app resets to ready state.
- Keep playback best-effort so timer transitions never depend on audio success.

**Windows Design**

- Extend `WindowsAppState` persistence with chime enabled state and chime volume.
- Add a small Windows-side audio helper that builds a short WAV stream from fixed note patterns and plays it asynchronously.
- Keep chime trigger decisions in `MainForm.OnTick` because phase transitions are already observed there for task increment and completion messaging.
- Play the work-complete chime when the engine transitions from work into short or long break.
- Play the break-complete chime when the engine transitions from a break back to stopped work-ready state.

**Settings UI**

- macOS `SettingsView` gets:
  - a toggle for completion chimes
  - a slider for volume
  - the volume control stays visible but disabled when chimes are off
- Windows `SettingsForm` gets matching controls:
  - checkbox for completion chimes
  - percentage numeric input for volume
- Reset settings restores chimes enabled and a sensible default volume.

**Testing**

- macOS unit tests cover:
  - default chime settings
  - persisted normalization/reset behavior
  - timer completion invokes the correct chime event through an injectable audio seam
- Windows tests cover:
  - state normalization and persistence defaults for chime settings
  - generated WAV data or note-plan selection sanity
  - `OnTick`-level trigger helper behavior for work-complete versus break-complete transitions

**Why This Design**

- It keeps the sounds fixed and consistent without introducing external assets or custom file management.
- It avoids adding heavy audio dependencies.
- It attaches playback at the existing phase-transition seams on both platforms, which minimizes behavior risk.
