import SwiftUI
import AppKit

struct ContentView: View {
    @EnvironmentObject var taskStore: TaskStore
    @State private var newTaskTitle: String = ""
    @State private var taskPendingDeletion: PomodoroTask?
    @State private var showingDeleteConfirmation: Bool = false
    @State private var selectedTaskID: UUID?

    var body: some View {
        ZStack {
            GlassBackground(mode: mode)

            HStack(spacing: AppTheme.Spacing.md) {
                taskListView
                    .frame(width: 300)

                timerView
                    .frame(maxWidth: .infinity)
            }
            .padding(AppTheme.Spacing.md)
        }
        .frame(width: 760, height: 500)
        .sheet(isPresented: $taskStore.showingSettings) {
            SettingsView(taskStore: taskStore)
        }
        .onChange(of: taskStore.showingFloatingWindow) { newValue in
            if newValue {
                FloatingWindowController.shared.show(taskStore: taskStore) {
                    taskStore.showingFloatingWindow = false
                }
                mainAppWindow()?.orderOut(nil)
            } else {
                FloatingWindowController.shared.hide()
                DispatchQueue.main.asyncAfter(deadline: .now() + 0.1) {
                    mainAppWindow()?.makeKeyAndOrderFront(nil)
                    NSApp.activate(ignoringOtherApps: true)
                }
            }
        }
        .onAppear {
            selectedTaskID = taskStore.selectedTask?.id
        }
        .onChange(of: selectedTaskID) { newValue in
            guard taskStore.selectedTask?.id != newValue else { return }
            let selectedTask = taskStore.tasks.first(where: { $0.id == newValue })
            taskStore.selectTask(selectedTask)
        }
        .onChange(of: taskStore.selectedTask?.id) { newValue in
            guard selectedTaskID != newValue else { return }
            selectedTaskID = newValue
        }
        .alert(AppText.string("alert.delete_task.title", language: language), isPresented: $showingDeleteConfirmation, presenting: taskPendingDeletion) { task in
            Button(AppText.string("alert.delete_task.confirm", language: language), role: .destructive) {
                taskStore.deleteTask(id: task.id)
                taskPendingDeletion = nil
            }
            Button(AppText.string("alert.delete_task.cancel", language: language), role: .cancel) {
                taskPendingDeletion = nil
            }
        } message: { task in
            Text(AppText.string("alert.delete_task.message", language: language, task.title))
        }
    }

    private func mainAppWindow() -> NSWindow? {
        NSApp.windows.first(where: { !($0 is NSPanel) })
    }

    var taskListView: some View {
        GlassCard(mode: mode, padding: AppTheme.Spacing.sm) {
            VStack(alignment: .leading, spacing: AppTheme.Spacing.sm) {
                HStack(alignment: .center) {
                    Text(AppText.string("task.section_title", language: language))
                        .font(AppTheme.Typography.sectionTitle)

                    GlassTag(
                        mode: mode,
                        text: "\(taskStore.tasks.count)",
                        tint: AppTheme.Colors.tomatoPrimary(for: mode)
                    )

                    Spacer()

                    Menu {
                        ForEach(ThemeMode.allCases, id: \.self) { option in
                            Button {
                                taskStore.themeMode = option
                            } label: {
                                if option == mode {
                                    Label(option.displayName(language: language), systemImage: "checkmark")
                                } else {
                                    Text(option.displayName(language: language))
                                }
                            }
                        }
                    } label: {
                        Label {
                            Text(AppText.string("common.theme", language: language))
                                .font(.system(size: 12, weight: .medium))
                                .lineLimit(1)
                                .minimumScaleFactor(0.85)
                        } icon: {
                            Image(systemName: "swatchpalette")
                        }
                    }
                    .frame(minWidth: 94)
                    .buttonStyle(SecondaryGlassButtonStyle(mode: mode))
                    .help(AppText.string("help.quick_theme", language: language))

                    Button {
                        taskStore.showingSettings = true
                    } label: {
                        Image(systemName: "gearshape")
                    }
                    .buttonStyle(SecondaryGlassButtonStyle(mode: mode))
                    .help(AppText.string("help.settings", language: language))
                }
                .padding(.horizontal, AppTheme.Spacing.xs)

                if taskStore.tasks.isEmpty {
                    VStack(spacing: AppTheme.Spacing.sm) {
                        Image(systemName: "list.bullet.clipboard")
                            .font(.system(size: 28, weight: .light))
                            .foregroundStyle(AppTheme.Colors.textSecondary(for: mode))
                        Text(AppText.string("task.empty.title", language: language))
                            .font(.headline)
                        Text(AppText.string("task.empty.subtitle", language: language))
                            .font(.caption)
                            .foregroundStyle(AppTheme.Colors.textSecondary(for: mode))
                            .multilineTextAlignment(.center)
                    }
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
                    .padding(.horizontal, AppTheme.Spacing.md)
                } else {
                    List(selection: $selectedTaskID) {
                        ForEach(taskStore.tasks) { task in
                            TaskRowView(
                                task: task,
                                isSelected: selectedTaskID == task.id,
                                themeMode: mode,
                                onToggleComplete: {
                                    taskStore.toggleTaskCompletion(task)
                                }
                            )
                            .tag(task.id)
                            .contextMenu {
                                Button(
                                    task.isCompleted
                                    ? AppText.string("task.mark.undone", language: language)
                                    : AppText.string("task.mark.done", language: language)
                                ) {
                                    taskStore.selectTask(task)
                                    taskStore.toggleTaskCompletion(task)
                                }

                                Button(AppText.string("task.delete", language: language), role: .destructive) {
                                    taskStore.selectTask(task)
                                    taskPendingDeletion = task
                                    showingDeleteConfirmation = true
                                }
                            }
                        }
                        .onDelete(perform: taskStore.deleteTask)
                        .onMove(perform: taskStore.updateTaskOrder)
                    }
                    .listStyle(.plain)
                    .scrollContentBackground(.hidden)
                }

                HStack(spacing: AppTheme.Spacing.xs) {
                    TextField(AppText.string("task.add.placeholder", language: language), text: $newTaskTitle)
                        .textFieldStyle(.plain)
                        .padding(.horizontal, AppTheme.Spacing.sm)
                        .padding(.vertical, 9)
                        .background(AppTheme.Colors.textFieldFill(for: mode))
                        .overlay(
                            RoundedRectangle(cornerRadius: AppTheme.Radius.small, style: .continuous)
                                .stroke(AppTheme.Colors.textFieldStroke(for: mode), lineWidth: 0.8)
                        )
                        .clipShape(RoundedRectangle(cornerRadius: AppTheme.Radius.small, style: .continuous))
                        .onSubmit {
                            addTaskIfNeeded()
                        }

                    Button {
                        addTaskIfNeeded()
                    } label: {
                        Image(systemName: "plus")
                    }
                    .buttonStyle(PrimaryGlassButtonStyle(mode: mode))
                }
                .padding(.horizontal, AppTheme.Spacing.xs)
            }
        }
    }

    var timerView: some View {
        GlassCard(mode: mode) {
            VStack(spacing: AppTheme.Spacing.lg) {
                if let task = taskStore.timerDisplayTask {
                    VStack(spacing: AppTheme.Spacing.xs) {
                        Text(task.title)
                            .font(.system(size: 26, weight: .semibold, design: .rounded))
                            .lineLimit(2)
                            .multilineTextAlignment(.center)

                        HStack(spacing: AppTheme.Spacing.xs) {
                            Image(systemName: "flame.fill")
                                .foregroundStyle(AppTheme.Colors.tomatoSecondary(for: mode))
                            Text(AppText.string("task.completed.count", language: language, task.completedPomodoros))
                                .foregroundStyle(AppTheme.Colors.textSecondary(for: mode))
                        }
                        .font(.subheadline)
                    }
                } else {
                    VStack(spacing: AppTheme.Spacing.sm) {
                        Image(systemName: "timer")
                            .font(.system(size: 40, weight: .light))
                            .foregroundStyle(AppTheme.Colors.textSecondary(for: mode))
                        Text(AppText.string("task.select.prompt", language: language))
                            .font(.title3)
                            .foregroundStyle(AppTheme.Colors.textSecondary(for: mode))
                    }
                }

                ZStack {
                    Circle()
                        .fill(Color.white.opacity(0.25))
                        .frame(width: 236, height: 236)

                    Circle()
                        .stroke(AppTheme.Colors.ringTrack(for: mode), lineWidth: 12)
                        .frame(width: 206, height: 206)

                    Circle()
                        .trim(from: timerElapsedProgress, to: 1)
                        .stroke(
                            timerColor,
                            style: StrokeStyle(lineWidth: 12, lineCap: .round)
                        )
                        .frame(width: 206, height: 206)
                        .rotationEffect(.degrees(-90))
                        .shadow(color: AppTheme.Colors.ringGlow(for: mode), radius: 6, x: 0, y: 0)
                        .animation(.linear(duration: 1), value: taskStore.remainingSeconds)

                    VStack(spacing: AppTheme.Spacing.xs) {
                        Text(timeString)
                            .font(AppTheme.Typography.heroTimer)
                            .contentTransition(.numericText())
                            .animation(.linear(duration: 0.5), value: taskStore.remainingSeconds)

                        GlassTag(mode: mode, text: phaseText, tint: timerColor)
                    }
                }

                HStack(spacing: AppTheme.Spacing.sm) {
                    if taskStore.isTimerRunning {
                        Button {
                            taskStore.stopTimer()
                        } label: {
                            Label(AppText.string("common.stop", language: language), systemImage: "stop.fill")
                        }
                        .buttonStyle(PrimaryGlassButtonStyle(mode: mode))
                    } else {
                        Button {
                            taskStore.startFocusSession()
                        } label: {
                            Label(AppText.string("common.focus", language: language), systemImage: "play.fill")
                        }
                        .buttonStyle(PrimaryGlassButtonStyle(mode: mode))
                        .disabled(taskStore.selectedTask == nil)
                    }

                    Button {
                        taskStore.resetTimer()
                    } label: {
                        Label(AppText.string("common.reset", language: language), systemImage: "arrow.counterclockwise")
                    }
                    .buttonStyle(SecondaryGlassButtonStyle(mode: mode))

                    Button {
                        taskStore.showingFloatingWindow = true
                    } label: {
                        Label(AppText.string("common.float", language: language), systemImage: "rectangle.portrait.and.arrow.right")
                    }
                    .buttonStyle(SecondaryGlassButtonStyle(mode: mode))
                    .disabled(taskStore.selectedTask == nil)
                }
                .disabled(taskStore.selectedTask == nil && !taskStore.isTimerRunning)
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity)
        }
    }

    private func addTaskIfNeeded() {
        let title = newTaskTitle.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !title.isEmpty else { return }
        taskStore.addTask(title: title)
        newTaskTitle = ""
    }

    var timerColor: Color {
        taskStore.currentPhase.themedColor(for: mode)
    }

    var phaseText: String {
        taskStore.currentPhase.displayName(language: language)
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

struct TaskRowView: View {
    let task: PomodoroTask
    let isSelected: Bool
    let themeMode: ThemeMode
    let onToggleComplete: () -> Void

    var body: some View {
        HStack(spacing: AppTheme.Spacing.sm) {
            Button {
                onToggleComplete()
            } label: {
                Image(systemName: task.isCompleted ? "checkmark.circle.fill" : "circle")
                    .foregroundStyle(
                        task.isCompleted
                        ? AppTheme.Colors.accentMint(for: themeMode)
                        : AppTheme.Colors.textSecondary(for: themeMode)
                    )
            }
            .buttonStyle(.plain)

            VStack(alignment: .leading, spacing: 3) {
                Text(task.title)
                    .font(.body)
                    .strikethrough(task.isCompleted)
                    .foregroundStyle(
                        task.isCompleted
                        ? AppTheme.Colors.textSecondary(for: themeMode)
                        : AppTheme.Colors.textPrimary(for: themeMode)
                    )
                    .lineLimit(1)

                if task.completedPomodoros > 0 {
                    HStack(spacing: 2) {
                        Text("\(task.completedPomodoros)x")
                            .font(.caption2)
                            .foregroundStyle(AppTheme.Colors.textSecondary(for: themeMode))
                        ForEach(0..<min(task.completedPomodoros, 5), id: \.self) { _ in
                            TinyTomatoIcon(size: 8, themeMode: themeMode)
                        }
                        if task.completedPomodoros > 5 {
                            Text("+")
                                .font(.caption2)
                                .foregroundStyle(AppTheme.Colors.textSecondary(for: themeMode))
                        }
                    }
                }
            }

            Spacer(minLength: 0)
        }
        .padding(.horizontal, 8)
        .padding(.vertical, 7)
        .background(
            RoundedRectangle(cornerRadius: 10, style: .continuous)
                .fill(isSelected ? AppTheme.Colors.selectionFill(for: themeMode) : Color.clear)
        )
        .contentShape(Rectangle())
    }
}

struct TinyTomatoIcon: View {
    let size: CGFloat
    let themeMode: ThemeMode

    var body: some View {
        ZStack(alignment: .top) {
            Circle()
                .fill(AppTheme.Colors.tomatoPrimary(for: themeMode))
                .frame(width: size, height: size)

            Capsule()
                .fill(AppTheme.Colors.accentMint(for: themeMode))
                .frame(width: size * 0.36, height: size * 0.24)
                .offset(y: -size * 0.35)
        }
        .frame(width: size, height: size)
    }
}
