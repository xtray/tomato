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
        let panel = NSPanel(
            contentRect: NSRect(x: 0, y: 0, width: 160, height: 180),
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
        
        let contentView = FloatingTimerContentView(taskStore: taskStore) { [weak self] in
            self?.hide()
            self?.onCloseCallback?()
        }
        
        let hosting = NSHostingView(rootView: AnyView(contentView))
        panel.contentView = hosting
        
        self.hostingView = hosting
        self.window = panel
    }
    
    private func positionWindowAtTopRight() {
        guard let window = self.window else { return }
        let mouseLocation = NSEvent.mouseLocation
        let screen = NSScreen.screens.first(where: { NSMouseInRect(mouseLocation, $0.frame, false) }) ?? NSScreen.main
        guard let screen else { return }
        
        let screenFrame = screen.visibleFrame
        let windowWidth: CGFloat = 160
        let windowHeight: CGFloat = 180
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
        VStack(spacing: 8) {
            if let task = taskStore.selectedTask {
                Text(task.title)
                    .font(.caption)
                    .fontWeight(.medium)
                    .lineLimit(1)
                    .truncationMode(.tail)
            }
            
            ZStack {
                Circle()
                    .stroke(Color.gray.opacity(0.2), lineWidth: 8)
                    .frame(width: 100, height: 100)
                
                Circle()
                    .trim(from: 0, to: timerProgress)
                    .stroke(taskStore.currentPhase.color, style: StrokeStyle(lineWidth: 8, lineCap: .round))
                    .frame(width: 100, height: 100)
                    .rotationEffect(.degrees(-90))
                    .animation(.linear(duration: 1), value: taskStore.remainingSeconds)
                
                VStack(spacing: 2) {
                    Text(timeString)
                        .font(.system(size: 20, weight: .light, design: .monospaced))
                        .contentTransition(.numericText())
                        .animation(.linear(duration: 0.5), value: taskStore.remainingSeconds)
                    
                    Text(taskStore.currentPhase.displayName)
                        .font(.caption2)
                        .foregroundColor(.secondary)
                }
            }
            
            HStack(spacing: 12) {
                if taskStore.isTimerRunning {
                    Button {
                        taskStore.stopTimer()
                    } label: {
                        Image(systemName: "pause.fill")
                            .font(.caption)
                    }
                    .buttonStyle(.borderedProminent)
                    .tint(.red)
                    .controlSize(.small)
                } else {
                    Button {
                        taskStore.startFocusSession()
                    } label: {
                        Image(systemName: "play.fill")
                            .font(.caption)
                    }
                    .buttonStyle(.borderedProminent)
                    .controlSize(.small)
                }
                
                Button {
                    taskStore.resetTimer()
                } label: {
                    Image(systemName: "arrow.counterclockwise")
                        .font(.caption)
                }
                .buttonStyle(.bordered)
                .controlSize(.small)
                
                Button {
                    onClose()
                } label: {
                    Label("Back", systemImage: "arrow.uturn.backward.circle.fill")
                        .labelStyle(.iconOnly)
                        .font(.caption)
                }
                .buttonStyle(.bordered)
                .controlSize(.small)
                .help("Back to main window")
            }
        }
        .padding(12)
        .frame(width: 160, height: 180)
        .background(.regularMaterial)
        .clipShape(RoundedRectangle(cornerRadius: 16))
        .shadow(color: .black.opacity(0.15), radius: 8, x: 0, y: 4)
    }
    
    var timeString: String {
        let minutes = taskStore.remainingSeconds / 60
        let seconds = taskStore.remainingSeconds % 60
        return String(format: "%02d:%02d", minutes, seconds)
    }
    
    var timerProgress: CGFloat {
        let total = CGFloat(currentPhaseDuration)
        return CGFloat(taskStore.remainingSeconds) / total
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
}
