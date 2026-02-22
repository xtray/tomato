import SwiftUI
import AppKit

class FloatingWindowController: NSObject, ObservableObject {
    static let shared = FloatingWindowController()

    private var window: NSPanel?
    private var hostingView: NSHostingView<AnyView>?
    private var onCloseCallback: (() -> Void)?

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
        window?.orderFront(nil)
        isVisible = true
    }

    func hide() {
        window?.orderOut(nil)
        isVisible = false
    }

    func update(taskStore: TaskStore) {
        if let hostingView = hostingView {
            hostingView.rootView = AnyView(FloatingTimerContentView(taskStore: taskStore) { [weak self] in
                self?.hide()
                self?.onCloseCallback?()
            })
        }
    }

    private func createWindow(taskStore: TaskStore) {
        let panel = Self.makePanel(
            contentRect: NSRect(x: 0, y: 0, width: 208, height: 232)
        )

        let contentView = FloatingTimerContentView(taskStore: taskStore) { [weak self] in
            self?.hide()
            self?.onCloseCallback?()
        }

        let hosting = NSHostingView(rootView: AnyView(contentView))
        panel.contentView = hosting

        self.hostingView = hosting
        self.window = panel
    }

    static func makePanel(contentRect: NSRect) -> NSPanel {
        let panel = NSPanel(
            contentRect: contentRect,
            styleMask: [.borderless, .nonactivatingPanel, .utilityWindow],
            backing: .buffered,
            defer: false
        )
        panel.level = .floating
        panel.isOpaque = false
        panel.backgroundColor = .clear
        panel.hasShadow = true
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
        let windowWidth: CGFloat = 208
        let windowHeight: CGFloat = 232
        let padding: CGFloat = 20

        let x = screenFrame.maxX - windowWidth - padding
        let y = screenFrame.maxY - windowHeight - padding

        window.setFrame(NSRect(x: x, y: y, width: windowWidth, height: windowHeight), display: true)
    }
}

struct FloatingTimerContentView: View {
    @ObservedObject var taskStore: TaskStore
    var onClose: () -> Void

    var body: some View {
        GlassCard(mode: mode, padding: 10) {
            VStack(spacing: 10) {
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
                        .frame(width: 116, height: 116)

                    Circle()
                        .stroke(AppTheme.Colors.ringTrack(for: mode), lineWidth: 8)
                        .frame(width: 98, height: 98)

                    Circle()
                        .trim(from: timerElapsedProgress, to: 1)
                        .stroke(
                            taskStore.currentPhase.themedColor(for: mode),
                            style: StrokeStyle(lineWidth: 8, lineCap: .round)
                        )
                        .frame(width: 98, height: 98)
                        .rotationEffect(.degrees(-90))
                        .shadow(color: AppTheme.Colors.ringGlow(for: mode), radius: 4)
                        .animation(.linear(duration: 1), value: taskStore.remainingSeconds)

                    Text(timeString)
                        .font(.system(size: 19, weight: .light, design: .monospaced))
                        .contentTransition(.numericText())
                        .animation(.linear(duration: 0.5), value: taskStore.remainingSeconds)
                }

                HStack(spacing: 8) {
                    if taskStore.isTimerRunning {
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
        .frame(width: 208, height: 232)
        .padding(8)
        .background(Color.clear)
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
