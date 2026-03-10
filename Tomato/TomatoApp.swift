import SwiftUI
import AppKit
import ObjectiveC

protocol ApplicationIconSetting: AnyObject {
    var applicationIconImage: NSImage! { get set }
}

extension NSApplication: ApplicationIconSetting {}

private var mainWindowResizeGuardAssociationKey: UInt8 = 0

final class MainWindowResizeGuard: NSObject, NSWindowDelegate {
    func windowWillResize(_ sender: NSWindow, to frameSize: NSSize) -> NSSize {
        MainWindowConfiguration.fixedFrameSize(for: sender)
    }

    func windowShouldZoom(_ window: NSWindow, toFrame newFrame: NSRect) -> Bool {
        false
    }
}

enum MainWindowFrameCursorShieldLayout {
    static let thickness: CGFloat = 16

    static func makeFrameRects(frameBounds: NSRect, contentFrame: NSRect) -> [NSRect] {
        let edgeThickness = min(thickness, min(contentFrame.width, contentFrame.height) / 2)
        let topHeight = max(0, frameBounds.maxY - contentFrame.maxY)

        return [
            NSRect(x: frameBounds.minX, y: contentFrame.minY, width: edgeThickness, height: contentFrame.height),
            NSRect(x: contentFrame.maxX - edgeThickness, y: contentFrame.minY, width: edgeThickness, height: contentFrame.height),
            NSRect(x: contentFrame.minX, y: frameBounds.minY, width: contentFrame.width, height: edgeThickness),
            NSRect(x: frameBounds.minX, y: contentFrame.maxY, width: frameBounds.width, height: topHeight)
        ].filter { $0.width > 0 && $0.height > 0 }
    }
}

final class MainWindowFrameCursorShieldView: NSView {
    override init(frame frameRect: NSRect) {
        super.init(frame: frameRect)
        isHidden = false
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        nil
    }

    override func hitTest(_ point: NSPoint) -> NSView? {
        nil
    }

    override func updateTrackingAreas() {
        super.updateTrackingAreas()
        for area in trackingAreas {
            removeTrackingArea(area)
        }
        addTrackingArea(NSTrackingArea(
            rect: .zero,
            options: [.cursorUpdate, .activeAlways, .inVisibleRect],
            owner: self,
            userInfo: nil
        ))
    }

    override func cursorUpdate(with event: NSEvent) {
        NSCursor.arrow.set()
    }

    override func resetCursorRects() {
        discardCursorRects()
        addCursorRect(bounds, cursor: .arrow)
    }
}

enum MainWindowFrameCursorShieldInstaller {
    static func frameView(in window: NSWindow) -> NSView? {
        window.contentView?.superview
    }

    static func install(on window: NSWindow) {
        guard let contentView = window.contentView, let frameView = frameView(in: window) else {
            return
        }

        frameView.subviews
            .compactMap { $0 as? MainWindowFrameCursorShieldView }
            .forEach { $0.removeFromSuperview() }

        for rect in MainWindowFrameCursorShieldLayout.makeFrameRects(
            frameBounds: frameView.bounds,
            contentFrame: contentView.frame
        ) {
            let shield = MainWindowFrameCursorShieldView(frame: rect)
            shield.autoresizingMask = autoresizingMask(for: rect, in: frameView.bounds, contentFrame: contentView.frame)
            frameView.addSubview(shield, positioned: .above, relativeTo: nil)
            window.invalidateCursorRects(for: shield)
        }
    }

    private static func autoresizingMask(
        for rect: NSRect,
        in frameBounds: NSRect,
        contentFrame: NSRect
    ) -> NSView.AutoresizingMask {
        if rect.minY >= contentFrame.maxY {
            return [.width, .minYMargin]
        }

        if rect.width == contentFrame.width {
            return [.width, .maxYMargin]
        }

        if rect.minX == frameBounds.minX {
            return [.height, .maxXMargin]
        }

        return [.height, .minXMargin]
    }
}

enum MainWindowConfiguration {
    static let fixedContentSize = NSSize(width: 760, height: 500)

    static func fixedFrameSize(for window: NSWindow) -> NSSize {
        window.frameRect(forContentRect: NSRect(origin: .zero, size: fixedContentSize)).size
    }

    static func apply(to window: NSWindow?) {
        guard let window else { return }

        enforceNonResizable(window)
        window.setContentSize(fixedContentSize)

        let resizeGuard = (objc_getAssociatedObject(window, &mainWindowResizeGuardAssociationKey) as? MainWindowResizeGuard)
            ?? MainWindowResizeGuard()
        objc_setAssociatedObject(
            window,
            &mainWindowResizeGuardAssociationKey,
            resizeGuard,
            .OBJC_ASSOCIATION_RETAIN_NONATOMIC
        )
        window.delegate = resizeGuard
        MainWindowFrameCursorShieldInstaller.install(on: window)
    }

    /// Lightweight enforcement that is safe to call from every layout pass.
    /// Strips `.resizable` whenever SwiftUI re-adds it and removes any
    /// resize-cursor tracking areas that AppKit attaches to the theme frame.
    static func enforceNonResizable(_ window: NSWindow) {
        if window.styleMask.contains(.resizable) {
            var mask = window.styleMask
            mask.remove(.resizable)
            window.styleMask = mask
        }
        window.minSize = fixedContentSize
        window.maxSize = fixedContentSize
        window.standardWindowButton(.zoomButton)?.isEnabled = false

        if let frameView = window.contentView?.superview {
            for area in frameView.trackingAreas where area.options.contains(.cursorUpdate) {
                frameView.removeTrackingArea(area)
            }
        }
    }
}

struct MainWindowConfigurationContainer<Content: View>: NSViewRepresentable {
    let content: Content

    init(@ViewBuilder content: () -> Content) {
        self.content = content()
    }

    func makeNSView(context: Context) -> MainWindowConfigurationHostingView<Content> {
        MainWindowConfigurationHostingView(rootView: content)
    }

    func updateNSView(_ nsView: MainWindowConfigurationHostingView<Content>, context: Context) {
        nsView.rootView = content
        MainWindowConfiguration.apply(to: nsView.window)
    }
}

final class MainWindowConfigurationHostingView<Content: View>: NSHostingView<Content> {
    override func viewDidMoveToWindow() {
        super.viewDidMoveToWindow()
        MainWindowConfiguration.apply(to: window)
    }

    override func layout() {
        super.layout()
        if let window = window {
            MainWindowConfiguration.enforceNonResizable(window)
        }
    }
}

struct FirstMouseContainer<Content: View>: NSViewRepresentable {
    let content: Content

    init(@ViewBuilder content: () -> Content) {
        self.content = content()
    }

    func makeNSView(context: Context) -> FirstMouseHostingView<Content> {
        FirstMouseHostingView(rootView: content)
    }

    func updateNSView(_ nsView: FirstMouseHostingView<Content>, context: Context) {
        nsView.rootView = content
    }
}

final class FirstMouseHostingView<Content: View>: NSHostingView<Content> {
    override func acceptsFirstMouse(for event: NSEvent?) -> Bool {
        true
    }
}

enum RuntimeAppIconApplier {
    static func applyIfPossible(
        to application: ApplicationIconSetting?,
        loadIcon: () -> NSImage? = { RuntimeAppIconApplier.loadIconFromMainBundle() }
    ) {
        guard let application else { return }
        guard let icon = loadIcon() else { return }
        application.applicationIconImage = icon
    }

    static func loadIconFromMainBundle(bundle: Bundle = .main) -> NSImage? {
        guard let iconPath = bundle.path(forResource: "AppIcon", ofType: "icns") else {
            return nil
        }

        return NSImage(contentsOfFile: iconPath)
    }
}

@main
struct TomatoApp: App {
    @StateObject private var taskStore = TaskStore()
    @State private var commandsLanguage = LanguagePreferences.load()
    @State private var hasAppliedRuntimeIcon = false
    
    var body: some Scene {
        WindowGroup {
            MainWindowConfigurationContainer {
                FirstMouseContainer {
                    ContentView()
                        .environmentObject(taskStore)
                        .onAppear {
                            applyRuntimeIconIfNeeded()
                        }
                }
            }
        }
        .windowStyle(.hiddenTitleBar)
        .commands {
            CommandGroup(replacing: .newItem) {}
            // Keep Commands menu language stable during runtime.
            // Updating CommandMenu titles live can trigger AppKit menu mapping warnings/crashes.
            CommandMenu(AppText.string("menu.timer", language: commandsLanguage)) {
                Button(AppText.string("menu.start_focus", language: commandsLanguage)) {
                    taskStore.startFocusSession()
                }
                .keyboardShortcut("s", modifiers: [.command])
                .disabled(!taskStore.canStartOrResumeFocus)
                
                Button(AppText.string("common.stop", language: commandsLanguage)) {
                    taskStore.stopTimer()
                }
                .keyboardShortcut(".", modifiers: [.command])
                .disabled(!taskStore.isTimerRunning)
                
                Button(AppText.string("common.reset", language: commandsLanguage)) {
                    taskStore.resetTimer()
                }
                .keyboardShortcut("r", modifiers: [.command])
                
                Divider()
                
                Button(AppText.string("menu.settings", language: commandsLanguage)) {
                    taskStore.showingSettings = true
                }
                .keyboardShortcut(",", modifiers: [.command])
            }
        }
    }

    private func applyRuntimeIconIfNeeded() {
        guard !hasAppliedRuntimeIcon else { return }
        RuntimeAppIconApplier.applyIfPossible(to: NSApp)
        hasAppliedRuntimeIcon = true
    }
}
