import SwiftUI
import AppKit

enum FloatingWindowLayout {
    static let resizeHandleSize: CGFloat = 24
    static let minimumRingDiameter: CGFloat = 84
    static let maximumRingDiameter: CGFloat = 420
    static let ringBackgroundPadding: CGFloat = 10

    private static let cardInnerPadding: CGFloat = 8
    private static let horizontalSafetyInset: CGFloat = 16
    private static let topRowHeight: CGFloat = 28
    private static let taskTitleHeight: CGFloat = 17
    private static let buttonRowHeight: CGFloat = 34
    private static let verticalSpacing: CGFloat = 8
    private static let extraVerticalBuffer: CGFloat = 12

    static func proposedSizeFromBottomLeftDrag(startSize: CGSize, translation: CGSize) -> CGSize {
        CGSize(
            width: startSize.width - translation.width,
            height: startSize.height - translation.height
        )
    }

    static func frameKeepingTopRight(originalFrame: CGRect, proposedSize: CGSize) -> CGRect {
        let anchorTopRight = CGPoint(x: originalFrame.maxX, y: originalFrame.maxY)
        return frameKeepingTopRight(anchorTopRight: anchorTopRight, proposedSize: proposedSize)
    }

    static func frameKeepingTopRight(anchorTopRight: CGPoint, proposedSize: CGSize) -> CGRect {
        CGRect(
            x: anchorTopRight.x - proposedSize.width,
            y: anchorTopRight.y - proposedSize.height,
            width: proposedSize.width,
            height: proposedSize.height
        )
    }

    static func timerRingDiameter(for windowSize: CGSize, showsTaskTitle: Bool) -> CGFloat {
        let spacingCount: CGFloat = showsTaskTitle ? 3 : 2
        let titleHeight = showsTaskTitle ? taskTitleHeight : 0
        let reservedHeight =
            (cardInnerPadding * 2) +
            topRowHeight +
            titleHeight +
            buttonRowHeight +
            (verticalSpacing * spacingCount) +
            extraVerticalBuffer
        let reservedWidth = (cardInnerPadding * 2) + horizontalSafetyInset

        let availableWidth = max(0, windowSize.width - reservedWidth)
        let availableHeight = max(0, windowSize.height - reservedHeight)
        let maxFittingDiameter = max(
            0,
            min(availableWidth, availableHeight) - (ringBackgroundPadding * 2)
        )

        let preferredDiameter = min(
            maximumRingDiameter,
            max(minimumRingDiameter, maxFittingDiameter)
        )
        // Keep the preferred minimum when possible, but never exceed currently available space.
        return min(preferredDiameter, maxFittingDiameter)
    }
}

enum FloatingWindowSizePreferences {
    static let widthKey = "floatingWindowWidth"
    static let heightKey = "floatingWindowHeight"

    // Compact and slightly squarer than before.
    static let defaultSize = CGSize(width: 212, height: 244)
    // Keep all controls fully visible while preventing an overly narrow look.
    static let minSize = CGSize(width: 196, height: 238)
    static let maxSize = CGSize(width: 1280, height: 900)

    static func load(from defaults: UserDefaults = .standard) -> CGSize {
        let width = defaults.double(forKey: widthKey)
        let height = defaults.double(forKey: heightKey)

        guard width > 0, height > 0 else {
            return defaultSize
        }

        let stored = normalized(CGSize(width: width, height: height))
        if isLegacyDefaultSize(stored) {
            return defaultSize
        }

        return stored
    }

    static func save(_ size: CGSize, to defaults: UserDefaults = .standard) {
        let normalizedSize = normalized(size)
        defaults.set(normalizedSize.width, forKey: widthKey)
        defaults.set(normalizedSize.height, forKey: heightKey)
    }

    static func normalized(_ size: CGSize) -> CGSize {
        return CGSize(
            width: min(max(size.width, minSize.width), maxSize.width),
            height: min(max(size.height, minSize.height), maxSize.height)
        )
    }

    // Migrate historical default sizes to the new compact default, while preserving real user-resized values.
    private static func isLegacyDefaultSize(_ size: CGSize) -> Bool {
        let legacyDefaults = [
            CGSize(width: 340, height: 408),
            CGSize(width: 320, height: 392),
            CGSize(width: 320, height: 374),
            CGSize(width: 300, height: 356),
            CGSize(width: 176, height: 300),
            CGSize(width: 176, height: 272),
            CGSize(width: 196, height: 252)
        ]

        return legacyDefaults.contains {
            abs($0.width - size.width) < 0.5 && abs($0.height - size.height) < 0.5
        }
    }
}

class FloatingWindowController: NSObject, ObservableObject {
    static let shared = FloatingWindowController()

    private var window: NSPanel?
    private var hostingView: NSHostingView<AnyView>?
    private var onCloseCallback: (() -> Void)?
    private var currentSize: CGSize = FloatingWindowSizePreferences.load()
    private var activeResizeAnchorTopRight: CGPoint?

    @Published var isVisible: Bool = false

    private override init() {
        super.init()
    }

    func show(taskStore: TaskStore, onClose: @escaping () -> Void) {
        self.onCloseCallback = onClose

        if window == nil {
            createWindow(taskStore: taskStore)
        }

        update(taskStore: taskStore)
        positionWindowAtTopRight()
        window?.isMovableByWindowBackground = true
        window?.orderFrontRegardless()
        isVisible = true
    }

    func hide() {
        activeResizeAnchorTopRight = nil
        window?.isMovableByWindowBackground = true
        window?.orderOut(nil)
        isVisible = false
    }

    func update(taskStore: TaskStore) {
        if let hostingView = hostingView {
            hostingView.rootView = AnyView(
                FloatingTimerContentView(
                    taskStore: taskStore,
                    currentWindowSize: { [weak self] in
                        self?.currentSize ?? FloatingWindowSizePreferences.defaultSize
                    },
                    onClose: { [weak self] in
                        self?.hide()
                        self?.onCloseCallback?()
                    },
                    onResize: { [weak self] proposedSize, commit in
                        self?.resizeWindowFromBottomLeft(proposedSize: proposedSize, commit: commit)
                    },
                    onResizeActiveChanged: { [weak self] isResizing in
                        self?.setBackgroundDraggingEnabled(!isResizing)
                    }
                )
            )
        }
    }

    private func createWindow(taskStore: TaskStore) {
        let panel = Self.makePanel(
            contentRect: NSRect(x: 0, y: 0, width: currentSize.width, height: currentSize.height)
        )

        let contentView = FloatingTimerContentView(
            taskStore: taskStore,
            currentWindowSize: { [weak self] in
                self?.currentSize ?? FloatingWindowSizePreferences.defaultSize
            },
            onClose: { [weak self] in
                self?.hide()
                self?.onCloseCallback?()
            },
            onResize: { [weak self] proposedSize, commit in
                self?.resizeWindowFromBottomLeft(proposedSize: proposedSize, commit: commit)
            },
            onResizeActiveChanged: { [weak self] isResizing in
                self?.setBackgroundDraggingEnabled(!isResizing)
            }
        )

        let hosting = NSHostingView(rootView: AnyView(contentView))
        panel.contentView = hosting

        self.hostingView = hosting
        self.window = panel
    }

    static func makePanel(contentRect: NSRect) -> NSPanel {
        let panel = NSPanel(
            contentRect: contentRect,
            styleMask: [.borderless, .nonactivatingPanel],
            backing: .buffered,
            defer: false
        )
        panel.level = .statusBar
        panel.isOpaque = false
        panel.backgroundColor = .clear
        // Avoid NSPanel shadow artifacts after manual resize on transparent borderless windows.
        panel.hasShadow = false
        panel.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary]
        panel.hidesOnDeactivate = false
        panel.isMovable = true
        panel.isMovableByWindowBackground = true
        return panel
    }

    private func positionWindowAtTopRight() {
        guard let window = self.window else { return }
        let mouseLocation = NSEvent.mouseLocation
        let screen = NSScreen.screens.first(where: { NSMouseInRect(mouseLocation, $0.frame, false) }) ?? NSScreen.main
        guard let screen else { return }

        let screenFrame = screen.visibleFrame
        let size = FloatingWindowSizePreferences.normalized(currentSize)
        currentSize = size
        let padding: CGFloat = 20

        let x = screenFrame.maxX - size.width - padding
        let y = screenFrame.maxY - size.height - padding

        window.setFrame(NSRect(x: x, y: y, width: size.width, height: size.height), display: true)
    }

    private func resizeWindowFromBottomLeft(proposedSize: CGSize, commit: Bool) {
        guard let window = self.window else { return }

        let normalizedSize = FloatingWindowSizePreferences.normalized(proposedSize)
        let anchorTopRight = activeResizeAnchorTopRight ?? CGPoint(
            x: window.frame.maxX,
            y: window.frame.maxY
        )
        activeResizeAnchorTopRight = anchorTopRight
        let newFrame = FloatingWindowLayout.frameKeepingTopRight(
            anchorTopRight: anchorTopRight,
            proposedSize: normalizedSize
        )
        let alignedFrame = pixelAligned(frame: newFrame, window: window)

        currentSize = normalizedSize
        window.setFrame(alignedFrame, display: true, animate: false)
        if commit {
            FloatingWindowSizePreferences.save(normalizedSize)
            activeResizeAnchorTopRight = nil
        }
    }

    private func setBackgroundDraggingEnabled(_ enabled: Bool) {
        window?.isMovableByWindowBackground = enabled
    }

    private func pixelAligned(frame: CGRect, window: NSWindow) -> CGRect {
        let scale = backingScaleFactor(for: frame, fallbackWindow: window)
        guard scale > 0 else { return frame.integral }
        func align(_ value: CGFloat) -> CGFloat {
            (value * scale).rounded() / scale
        }
        return CGRect(
            x: align(frame.origin.x),
            y: align(frame.origin.y),
            width: align(frame.size.width),
            height: align(frame.size.height)
        )
    }

    private func backingScaleFactor(for frame: CGRect, fallbackWindow window: NSWindow) -> CGFloat {
        let targetScreen = screenForTargetFrame(frame)
        return targetScreen?.backingScaleFactor
            ?? window.screen?.backingScaleFactor
            ?? NSScreen.main?.backingScaleFactor
            ?? 2
    }

    private func screenForTargetFrame(_ frame: CGRect) -> NSScreen? {
        let screens = NSScreen.screens
        guard !screens.isEmpty else { return nil }

        var bestScreen: NSScreen?
        var bestIntersectionArea: CGFloat = 0
        for screen in screens {
            let area = intersectionArea(between: frame, and: screen.frame)
            if area > bestIntersectionArea {
                bestIntersectionArea = area
                bestScreen = screen
            }
        }

        if let bestScreen, bestIntersectionArea > 0 {
            return bestScreen
        }

        let center = CGPoint(x: frame.midX, y: frame.midY)
        return screens.first(where: { $0.frame.contains(center) })
    }

    private func intersectionArea(between lhs: CGRect, and rhs: CGRect) -> CGFloat {
        let intersection = lhs.intersection(rhs)
        guard !intersection.isNull, !intersection.isEmpty else { return 0 }
        return intersection.width * intersection.height
    }
}

private struct FloatingBottomLeftResizeHandle: NSViewRepresentable {
    var onDragBegan: () -> Void
    var onDragChanged: (CGSize) -> Void
    var onDragEnded: (CGSize) -> Void

    func makeNSView(context: Context) -> FloatingBottomLeftResizeHandleView {
        let view = FloatingBottomLeftResizeHandleView()
        view.onDragBegan = onDragBegan
        view.onDragChanged = onDragChanged
        view.onDragEnded = onDragEnded
        return view
    }

    func updateNSView(_ nsView: FloatingBottomLeftResizeHandleView, context: Context) {
        nsView.onDragBegan = onDragBegan
        nsView.onDragChanged = onDragChanged
        nsView.onDragEnded = onDragEnded
    }
}

private final class FloatingBottomLeftResizeHandleView: NSView {
    var onDragBegan: (() -> Void)?
    var onDragChanged: ((CGSize) -> Void)?
    var onDragEnded: ((CGSize) -> Void)?
    private var dragStartInScreen: CGPoint?
    private var backgroundDraggingBeforeResize: Bool?

    override init(frame frameRect: NSRect) {
        super.init(frame: frameRect)
        wantsLayer = true
        layer?.backgroundColor = NSColor.clear.cgColor
    }

    required init?(coder: NSCoder) {
        nil
    }

    override func acceptsFirstMouse(for event: NSEvent?) -> Bool {
        true
    }

    override var mouseDownCanMoveWindow: Bool {
        false
    }

    override func hitTest(_ point: NSPoint) -> NSView? {
        bounds.contains(point) ? self : nil
    }

    override func mouseDown(with event: NSEvent) {
        if let window {
            backgroundDraggingBeforeResize = window.isMovableByWindowBackground
            window.isMovableByWindowBackground = false
        }
        dragStartInScreen = screenPoint(from: event)
        onDragBegan?()
    }

    override func mouseDragged(with event: NSEvent) {
        guard let translation = dragTranslation(from: event) else { return }
        onDragChanged?(translation)
    }

    override func mouseUp(with event: NSEvent) {
        let translation = dragTranslation(from: event) ?? .zero
        onDragEnded?(translation)
        restoreBackgroundDragging()
        dragStartInScreen = nil
    }

    override func viewDidMoveToWindow() {
        if window == nil {
            dragStartInScreen = nil
            backgroundDraggingBeforeResize = nil
        }
    }

    override func mouseExited(with event: NSEvent) {
        // Keep tracking by start anchor even when cursor leaves the hotspot.
    }

    override func mouseEntered(with event: NSEvent) {
        // No-op; explicit to avoid implicit window-move behavior in this corner.
    }

    private func dragTranslation(from event: NSEvent) -> CGSize? {
        guard let start = dragStartInScreen else { return nil }
        let current = screenPoint(from: event)
        return CGSize(width: current.x - start.x, height: current.y - start.y)
    }

    private func screenPoint(from event: NSEvent) -> CGPoint {
        guard let window else { return NSEvent.mouseLocation }
        return window.convertPoint(toScreen: event.locationInWindow)
    }

    private func restoreBackgroundDragging() {
        guard let window else {
            backgroundDraggingBeforeResize = nil
            return
        }
        if let previous = backgroundDraggingBeforeResize {
            window.isMovableByWindowBackground = previous
        } else {
            window.isMovableByWindowBackground = true
        }
        backgroundDraggingBeforeResize = nil
    }
}

struct FloatingTimerContentView: View {
    @ObservedObject var taskStore: TaskStore
    var currentWindowSize: () -> CGSize
    var onClose: () -> Void
    var onResize: (_ proposedSize: CGSize, _ commit: Bool) -> Void
    var onResizeActiveChanged: (_ isResizing: Bool) -> Void
    @State private var resizeStartSize: CGSize = .zero
    @State private var isResizing: Bool = false

    var body: some View {
        let windowSize = currentWindowSize()
        let ringDiameter = FloatingWindowLayout.timerRingDiameter(
            for: windowSize,
            showsTaskTitle: taskStore.timerDisplayTask != nil
        )
        let ringBackgroundDiameter = ringDiameter + (FloatingWindowLayout.ringBackgroundPadding * 2)
        let ringLineWidth = max(6, min(12, ringDiameter * 0.085))
        let timeFontSize = max(18, min(44, ringDiameter * 0.29))

        GlassCard(mode: mode, padding: 8, showsStroke: false, showsShadow: false) {
            VStack(spacing: 8) {
                HStack {
                    GlassTag(
                        mode: mode,
                        text: taskStore.currentPhase.displayName(language: language),
                        tint: taskStore.currentPhase.themedColor(for: mode)
                    )
                    Spacer(minLength: 0)
                    Button {
                        onClose()
                    } label: {
                        Image(systemName: "arrow.uturn.backward.circle.fill")
                            .font(.system(size: 14, weight: .semibold))
                    }
                    .buttonStyle(SecondaryGlassButtonStyle(mode: mode))
                    .help(AppText.string("help.back_to_main", language: language))
                }

                if let task = taskStore.timerDisplayTask {
                    Text(task.title)
                        .font(.caption.weight(.semibold))
                        .lineLimit(1)
                        .truncationMode(.tail)
                }

                ZStack {
                    Circle()
                        .fill(Color.white.opacity(0.25))
                        .frame(width: ringBackgroundDiameter, height: ringBackgroundDiameter)

                    Circle()
                        .stroke(AppTheme.Colors.ringTrack(for: mode), lineWidth: ringLineWidth)
                        .frame(width: ringDiameter, height: ringDiameter)

                    Circle()
                        .trim(from: timerElapsedProgress, to: 1)
                        .stroke(
                            taskStore.currentPhase.themedColor(for: mode),
                            style: StrokeStyle(lineWidth: ringLineWidth, lineCap: .round)
                        )
                        .frame(width: ringDiameter, height: ringDiameter)
                        .rotationEffect(.degrees(-90))
                        .shadow(color: AppTheme.Colors.ringGlow(for: mode), radius: 4)
                        .animation(.linear(duration: 1), value: taskStore.remainingSeconds)

                    Text(timeString)
                        .font(.system(size: timeFontSize, weight: .light, design: .monospaced))
                        .contentTransition(.numericText())
                        .animation(.linear(duration: 0.5), value: taskStore.remainingSeconds)
                }

                HStack(spacing: 8) {
                    if taskStore.focusControlState == .pause {
                        Button {
                            taskStore.stopTimer()
                        } label: {
                            Image(systemName: "pause.fill")
                        }
                        .buttonStyle(PrimaryGlassButtonStyle(mode: mode))
                    } else {
                        Button {
                            taskStore.startFocusSession()
                        } label: {
                            Image(systemName: "play.fill")
                        }
                        .buttonStyle(PrimaryGlassButtonStyle(mode: mode))
                        .disabled(!taskStore.canStartOrResumeFocus)
                    }

                    Button {
                        taskStore.resetTimer()
                    } label: {
                        Image(systemName: "arrow.counterclockwise")
                    }
                    .buttonStyle(SecondaryGlassButtonStyle(mode: mode))
                }
                .controlSize(.small)
            }
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
        .padding(0)
        .background(Color.clear)
        .overlay(alignment: .bottomLeading) {
            FloatingBottomLeftResizeHandle(
                onDragBegan: {
                    isResizing = true
                    resizeStartSize = currentWindowSize()
                    onResizeActiveChanged(true)
                },
                onDragChanged: { translation in
                    guard isResizing else { return }

                    let proposed = FloatingWindowLayout.proposedSizeFromBottomLeftDrag(
                        startSize: resizeStartSize,
                        translation: translation
                    )
                    onResize(proposed, false)
                },
                onDragEnded: { translation in
                    guard isResizing else { return }

                    let proposed = FloatingWindowLayout.proposedSizeFromBottomLeftDrag(
                        startSize: resizeStartSize,
                        translation: translation
                    )
                    onResize(proposed, true)
                    isResizing = false
                    onResizeActiveChanged(false)
                }
            )
                .frame(width: FloatingWindowLayout.resizeHandleSize, height: FloatingWindowLayout.resizeHandleSize)
        }
        .onDisappear {
            if isResizing {
                isResizing = false
                onResizeActiveChanged(false)
            }
        }
    }

    var timeString: String {
        let minutes = taskStore.remainingSeconds / 60
        let seconds = taskStore.remainingSeconds % 60
        return String(format: "%02d:%02d", minutes, seconds)
    }

    var timerElapsedProgress: CGFloat {
        TimerProgressCalculator.elapsedProgress(
            remaining: taskStore.remainingSeconds,
            total: currentPhaseDuration
        )
    }

    var currentPhaseDuration: Int {
        switch taskStore.currentPhase {
        case .work:
            return taskStore.workDuration
        case .shortBreak:
            return taskStore.shortBreakDuration
        case .longBreak:
            return taskStore.longBreakDuration
        }
    }

    var mode: ThemeMode {
        taskStore.themeMode
    }

    var language: AppLanguage {
        taskStore.appLanguage
    }
}
